using System;
using System.Net;
using Amazon.S3;
using SquishIt.Framework.Files;
using Amazon.S3.Model;

namespace SquishIt.S3
{
    //typically I think it would be better to do file *reading* locally and then write bundled assets to amazon
    public class S3FileReaderFactory : IFileReaderFactory
    {
        private AmazonS3 s3client;

        public S3FileReaderFactory(AmazonS3 client)
        {
            s3client = client;
        }

        public IFileReader GetFileReader(string file)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string file)
        {
            try
            {
                var request = new GetObjectMetadataRequest()
                    .WithBucketName("bucket")
                    .WithKey("key");

                var response = s3client.GetObjectMetadata(request);

                return true;
            }
            catch(AmazonS3Exception ex)
            {
                if(ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }
                throw;
            }
        }
    }
}
