using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using SquishIt.Framework.Files;

namespace SquishIt.S3.Internal
{
    internal class S3FileWriter : IFileWriter
    {
        //build file in memory instead of on disk
        private StringBuilder combinedContents;

        private string file;
        private readonly AmazonS3 s3client;
        private string bucket;

        //how to get s3 key from this file path?
        //maybe something like IFileWriterFactory.GetFileWriter(string filePath, string localPath) would be helpful (writers only need to use the one they want)
        //any way to force using hash in file name?  (cloudfront doesn't support querystring invalidation)
        public S3FileWriter(string file, string bucket, AmazonS3 s3client)
        {
            this.bucket = bucket;
            this.file = file;
            this.s3client = s3client;
            combinedContents = new StringBuilder();
        }

        public void Write(string value)
        {
            combinedContents.Append(value);
        }

        public void WriteLine(string value)
        {
            combinedContents.AppendLine(value);
        }

        public void Dispose()
        {
            //before disposing, upload the file to s3
            var request = new PutObjectRequest()
                .WithKey(file)
                .WithBucketName(bucket)
                .WithCannedACL(S3CannedACL.PublicRead) //this permission ok?
                //add cache control headers
                .WithContentBody(combinedContents.ToString());

            s3client.PutObject(request);

            combinedContents = null;
        }
    }
}
