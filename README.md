# MongoDB-backup-Lambda-s3

quick port. we use this for our internal projects at [gressquel](https://www.gressquel.com)

it uses mongodump (which is really fast) to take backup of your db. then it uploads to S3 bucket.

It also has archieve functionality as in it creates 1 folder in s3 for every day so you don't overwrite previous backups.

# Usage

in S3ProxyController.cs change the variable:
```
string mongoarguments = "-h mongodb.server.com:27017 -d local -u MyUsername -p pw1234 --authenticationDatabase admin";
```

# Lambda settings (optional)

remember to turn up timeout time for big databases

![](https://raw.githubusercontent.com/San-Jeevan/MongoDB-backup-Lambda-s3/master/lambdasettings.png)
