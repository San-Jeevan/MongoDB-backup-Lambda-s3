//Jeevan Sivagnanasuntharam
//04.11.2018


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.S3;
using Amazon.S3.Model;
using System.Diagnostics;

namespace BackupMongo.Controllers
{
    /// <summary>
    /// ASP.NET Core controller acting as a S3 Proxy.
    /// </summary>
  
    public class S3ProxyController : Controller
    {
        string mongoarguments = "-h mongodb.server.com:27017 -d local -u MyUsername -p pw1234 --authenticationDatabase admin";
        IAmazonS3 S3Client { get; set; }
        ILogger Logger { get; set; }

        string BucketName { get; set; }

        public static void Exec(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };

            process.Start();
            process.WaitForExit();
        }


        public S3ProxyController(IConfiguration configuration, ILogger<S3ProxyController> logger, IAmazonS3 s3Client)
        {
            this.Logger = logger;
            this.S3Client = s3Client;

            this.BucketName = configuration[Startup.AppS3BucketKey];
            if(string.IsNullOrEmpty(this.BucketName))
            {
                logger.LogCritical("Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
                throw new Exception("Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
            }

            logger.LogInformation($"Configured to use bucket {this.BucketName}");
        }


        [Route("api/ocrdb")]
        [HttpGet]
        public async Task<string> Get()
        {
            //var folder = System.AppDomain.CurrentDomain.BaseDirectory;
            var folder = "/tmp/";
            System.IO.File.Copy("/var/task/mongodump", folder + "mongodump", true);
            Exec("chmod +x /tmp/mongodump");

            System.IO.File.Copy("/var/task/libcrypto.so.1.0.0", folder + "libcrypto.so.1.0.0", true);
            Exec("chmod +x /tmp/libcrypto.so.1.0.0");

            System.IO.File.Copy("/var/task/libgo.so.9", folder + "libgo.so.9", true);
            Exec("chmod +x /tmp/libgo.so.9");

            System.IO.File.Copy("/var/task/libsasl2.so.3", folder + "libsasl2.so.3", true);
            Exec("chmod +x /tmp/libsasl2.so.3");

            System.IO.File.Copy("/var/task/libssl.so.1.0.0", folder + "libssl.so.1.0.0", true);
            Exec("chmod +x /tmp/libssl.so.1.0.0");

            Environment.SetEnvironmentVariable("PATH", $"{Environment.GetEnvironmentVariable("PATH")}:{Environment.GetEnvironmentVariable("LAMBDA_TASK_ROOT")}");
            var process = new Process
            {
                StartInfo =
                    {
                        FileName = folder +"mongodump",
                        Arguments = mongoarguments + " -o " + folder + "raw/",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                EnableRaisingEvents = true
            };


            process.Exited += (sender, e) => {
                //create folder + upload
                System.IO.DirectoryInfo di = new DirectoryInfo(folder + "raw/");
                foreach (FileInfo fil in di.GetFiles("*", SearchOption.AllDirectories))
                {
                    var seekableStream = new MemoryStream();
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = this.BucketName,
                        Key = + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "/" + fil.Name,
                        InputStream = fil.OpenRead()
                    };

                    try
                    {
                        var response = this.S3Client.PutObjectAsync(putRequest).Result;
                        fil.Delete();
                        Logger.LogInformation($"Uploaded object to bucket {this.BucketName}. Request Id: {response.ResponseMetadata.RequestId}");
                    }
                    catch (AmazonS3Exception exe)
                    {
                        continue;
                    }
                }


                };
            process.OutputDataReceived += (obj, args) => Console.WriteLine(args.Data);
            process.ErrorDataReceived += (obj, args) => Console.WriteLine(args.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            
            return "ok";
        }

    }
}
