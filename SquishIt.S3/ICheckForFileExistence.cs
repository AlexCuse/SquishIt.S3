using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.S3;
using Amazon.S3.IO;

namespace SquishIt.S3
{
    public interface ICheckForFileExistence
    {
        bool Exists(string bucket, string key);
    }

    public class CheckForFileExistence : ICheckForFileExistence
    {
        private readonly IAmazonS3 s3Client;

        public CheckForFileExistence(IAmazonS3 s3client)
        {
            s3Client = s3client;
        }

        public bool Exists(string bucket, string key)
        {
            try
            {
                return new S3FileInfo(s3Client, bucket, key).Exists;
            }
            //If key doesn't exist then AmazonS3Exception exception will be 
            //thrown with message 'Forbidden 403'. Pretty silly stuff... But if we are sure that we have access to the bucket we can treat it as 'false'.
            //comment from: http://stackoverflow.com/a/16102595/794
            catch 
            {
                return false;
            }
        }
    }
}
