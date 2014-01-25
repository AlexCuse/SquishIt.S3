using System;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using Amazon.CloudFront;
using Amazon.S3;
using Amazon.S3.Model;
using SquishIt.Framework.Renderers;

namespace SquishIt.S3
{
    public class S3Renderer : IRenderer, IDisposable
    {
        string bucket;
        IAmazonS3 s3client;
        IKeyBuilder keyBuilder;
        IInvalidator invalidator;
        bool overwrite;
        S3CannedACL cannedACL = S3CannedACL.NoACL;
        NameValueCollection headers;
        bool compress;
        ICompressor compressor;

        public void Render(string content, string outputPath)
        {
            if(string.IsNullOrEmpty(outputPath) || string.IsNullOrEmpty(content)) throw new InvalidOperationException("Can't render to S3 with missing key/content.");

            var key = keyBuilder.GetKeyFor(outputPath);
            if(overwrite || !FileExists(key))
            {
                using(var stream = ContentForUpload(content))
                {
                    UploadContent(key, stream); 
                }
            }
        }

        Stream ContentForUpload(string content)
        {
            if(compress)
            {
                return compressor.Compress(content);
            }
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        void UploadContent(string key, Stream content)
        {
            var request = new PutObjectRequest()
                {
                    BucketName = bucket,
                    Key = key,
                    CannedACL = cannedACL,
                    InputStream = content,
                };

            if(headers != null)
            {
                foreach (var headerName in headers.AllKeys)
                {
                    request.Headers[headerName] = headers[headerName];
                }
            }

            //TODO: handle exceptions properly
            s3client.PutObject(request);

            if(invalidator != null) invalidator.InvalidateObject(bucket, key);
        }

        //TODO: extract this to another object that can be mocked (can't instantiate the exception we need)
        bool FileExists(string key)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                    {
                        BucketName = bucket,
                        Key = key
                    };

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

        #region setup
        public static S3Renderer Create(IAmazonS3 s3client)
        {
            return new S3Renderer(s3client);
        }

        public S3Renderer WithBucketName(string bucketName)
        {
            this.bucket = bucketName;
            return this;
        }

        internal S3Renderer WithCompressor(ICompressor textCompressor)
        {
            this.compressor = textCompressor;
            this.compress = true;

            if (headers == null)
            {
                headers = new NameValueCollection();
            }
            
            headers.Add(textCompressor.Headers);
            return this;
        }

        public S3Renderer WithGZipCompressionEnabled()
        {
            return WithCompressor(new GZipCompressor());
        }

        public S3Renderer WithKeyBuilder(IKeyBuilder builder)
        {
            this.keyBuilder = builder;
            return this;
        }

        public S3Renderer WithDefaultKeyBuilder(string physicalApplicationPath, string virtualDirectory)
        {
            return WithKeyBuilder(new KeyBuilder(physicalApplicationPath, virtualDirectory));
        }

        public S3Renderer WithCloudfrontClient(IAmazonCloudFront client)
        {
            return WithInvalidator(new CloudFrontInvalidator(client));
        }

        public S3Renderer WithInvalidator(IInvalidator instance)
        {
            this.invalidator = instance;
            return this;
        }

        public S3Renderer WithOverwriteBehavior(bool overwrite)
        {
            this.overwrite = overwrite;
            return this;
        }

        public S3Renderer WithCannedAcl(S3CannedACL acl)
        {
            this.cannedACL = acl;
            return this;
        }

        public S3Renderer WithHeaders(NameValueCollection headers)
        {
            if (this.headers == null)
            {
                this.headers = headers;
            }
            else
            {
                foreach(var key in headers.AllKeys)
                {
                    this.headers.Remove(key);
                }
                this.headers.Add(headers);
            }
            return this;
        }

        private S3Renderer(IAmazonS3 s3client)
        {
            this.s3client = s3client;
        }
        #endregion
    }
}
