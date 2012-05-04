using System;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using SquishIt.Framework.Renderers;

namespace SquishIt.S3
{
    public class S3Renderer : IRenderer, IDisposable
    {
        readonly string bucket;
        readonly AmazonS3 s3client;
        readonly IKeyBuilder keyBuilder;
        readonly bool overwrite;

        public S3Renderer(string bucket, AmazonS3 s3client)
            : this(bucket, s3client, false)
        {
        }

        public S3Renderer(string bucket, AmazonS3 s3client, bool overwrite)
            : this(bucket, s3client, overwrite, new KeyBuilder())
        {
        }

        public S3Renderer(string bucket, AmazonS3 s3client, bool overwrite, IKeyBuilder keyBuilder)
        {
            this.bucket = bucket;
            this.s3client = s3client;
            this.keyBuilder = keyBuilder;
            this.overwrite = overwrite;
        }

        public void Render(string content, string outputPath)
        {
            if(string.IsNullOrEmpty(outputPath) || string.IsNullOrEmpty(content)) throw new InvalidOperationException("Can't render to S3 with missing key/content.");

            var key = keyBuilder.GetKeyFor(outputPath);
            if(overwrite || !FileExists(key))
            {
                UploadContent(key, content);
            }
        }

        void UploadContent(string key, string content)
        {
            var request = new PutObjectRequest()
                .WithBucketName(bucket)
                .WithKey(key)
                .WithCannedACL(S3CannedACL.PublicRead)
                .WithContentBody(content);

            //TODO: handle exceptions properly
            s3client.PutObject(request);
        }

        bool FileExists(string key)
        {
            try
            {
                var request = new GetObjectMetadataRequest()
                    .WithBucketName(bucket)
                    .WithKey(key);

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
        public void Dispose()
        {
            s3client.Dispose();
        }
    }
}
