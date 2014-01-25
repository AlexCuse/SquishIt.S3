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
            return new S3FileInfo(s3Client, bucket, key).Exists;
        }
    }
}
