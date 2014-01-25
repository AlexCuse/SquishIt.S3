using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using NUnit.Framework;

namespace SquishIt.S3.Tests
{
    [TestFixture]
    public class S3RendererTest
    {
        AmazonS3Exception CreateNotFoundException()
        {
            var constructorInfo = typeof (AmazonS3Exception).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[]
                {
                    typeof (string), typeof (ErrorType), typeof (string), typeof (string), typeof (HttpStatusCode)
                }, null);

            return (AmazonS3Exception) constructorInfo.Invoke(new object[]
                                                   {"", ErrorType.Receiver, "", "", HttpStatusCode.NotFound});
        }

        [Test]
        public void Render_Uploads_If_File_Doesnt_Exist()
        {
            var s3client = new Mock<IAmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            string capturedContentAsString = null;

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);

            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
                Throws(CreateNotFoundException());

            s3client.Setup(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.CannedACL == S3CannedACL.NoACL)))
                    .Callback<PutObjectRequest>(por =>
                                                    {
                                                        var reader = new StreamReader(por.InputStream);
                                                        capturedContentAsString = reader.ReadToEnd();
                                                    });

            using (var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithKeyBuilder(keyBuilder.Object))
            {
                renderer.Render(content, path);
            }

            Assert.AreEqual(content, capturedContentAsString);
        }

        [Test]
        public void Render_Skips_Upload_If_File_Exists()
        {
            var s3client = new Mock<IAmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);
            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
                Returns(new GetObjectMetadataResponse());

            using (var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithKeyBuilder(keyBuilder.Object))
            {
                renderer.Render(content, path);
            }

            s3client.VerifyAll();
        }

        [Test]
        public void Render_Uploads_If_ForceUpload()
        {
            var s3client = new Mock<IAmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            string capturedContentAsString = null;

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);

            s3client.Setup(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.CannedACL == S3CannedACL.NoACL)))
                    .Callback<PutObjectRequest>(por =>
                    {
                        var reader = new StreamReader(por.InputStream);
                        capturedContentAsString = reader.ReadToEnd();
                    });

            using (var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithOverwriteBehavior(true)
                                    .WithKeyBuilder(keyBuilder.Object))
            {
                renderer.Render(content, path);
            }

            Assert.AreEqual(content, capturedContentAsString);
        }

        [Test]
        public void WithCannedACL()
        {
            var s3client = new Mock<IAmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            var cannedAcl = S3CannedACL.BucketOwnerFullControl;

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);
            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
               Throws(CreateNotFoundException());

            using (var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithOverwriteBehavior(false)
                                    .WithKeyBuilder(keyBuilder.Object)
                                    .WithCannedAcl(cannedAcl))
            {
                renderer.Render(content, path);
            }

            s3client.Verify(c => c.PutObject(It.Is<PutObjectRequest>(por => por.CannedACL == cannedAcl)));
        }

        [Test]
        public void WithHeaders()
        {
            var s3client = new Mock<IAmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            var headerValue = "test value";
            var headerName = "cache-control";
            var headers = new NameValueCollection { { headerName, headerValue } };

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);
            s3client.Setup(
                c =>
                c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key)))
                .
                Throws(CreateNotFoundException());

            using (var renderer = S3Renderer.Create(s3client.Object)
                .WithBucketName(bucket)
                .WithOverwriteBehavior(false)
                .WithKeyBuilder(keyBuilder.Object)
                .WithHeaders(headers))
            {
                renderer.Render(content, path);
            }

            s3client.Verify(c => c.PutObject(It.Is<PutObjectRequest>(por => GetHeaders(por)[headerName] == headerValue)));
        }

        [Test]
        public void WithInvalidation()
        {
            var s3client = new Mock<IAmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();
            var invalidator = new Mock<IInvalidator>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            var headerValue = "test value";
            var headerName = "cache-control";
            var headers = new NameValueCollection { { headerName, headerValue } };

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);
            s3client.Setup(
                c =>
                c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key)))
                .
                Throws(CreateNotFoundException());

            using (var renderer = S3Renderer.Create(s3client.Object)
                .WithBucketName(bucket)
                .WithOverwriteBehavior(false)
                .WithKeyBuilder(keyBuilder.Object)
                .WithInvalidator(invalidator.Object))
            {
                renderer.Render(content, path);
            }

            invalidator.Verify(i => i.InvalidateObject(bucket, key));
        }

        [Test]
        public void Render_With_Compression()
        {
            var s3client = new Mock<IAmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();
            var compressor = new Mock<ICompressor>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            var compressedContent = "compressedContent";
            string capturedContentAsString = null;

            var headers = new NameValueCollection { { "test", "header" } };
            HeadersCollection capturedHeaders = null;

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);

            compressor.Setup(c => c.Compress(content))
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes(compressedContent)));

            compressor.Setup(c => c.Headers).Returns(headers);

            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
                Throws(CreateNotFoundException());

            s3client.Setup(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.CannedACL == S3CannedACL.NoACL)))
                    .Callback<PutObjectRequest>(por =>
                    {
                        var reader = new StreamReader(por.InputStream);
                        capturedContentAsString = reader.ReadToEnd();
                        capturedHeaders = GetHeaders(por);
                    });

            using (var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithKeyBuilder(keyBuilder.Object)
                                    .WithCompressor(compressor.Object))
            {
                renderer.Render(content, path);
            }

            Assert.AreEqual(compressedContent, capturedContentAsString);
            Assert.AreEqual(1, capturedHeaders.Count);
            Assert.AreEqual(headers["test"], capturedHeaders["test"]);
        }

        [Test]
        public void Render_With_Compression_And_Additional_Headers()
        {
            var s3client = new Mock<IAmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();
            var compressor = new Mock<ICompressor>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            var compressedContent = "compressedContent";
            string capturedContentAsString = null;

            var headers = new NameValueCollection { { "test", "header" }, { "another", "headerToOverwrite" } };
            var moreHeaders = new NameValueCollection { { "another", "header" } };

            HeadersCollection capturedHeaders = null;

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);

            compressor.Setup(c => c.Compress(content))
                .Returns(new MemoryStream(Encoding.UTF8.GetBytes(compressedContent)));

            compressor.Setup(c => c.Headers).Returns(headers);

            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
                Throws(CreateNotFoundException());

            s3client.Setup(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.CannedACL == S3CannedACL.NoACL)))
                    .Callback<PutObjectRequest>(por =>
                    {
                        var reader = new StreamReader(por.InputStream);
                        capturedContentAsString = reader.ReadToEnd();
                        capturedHeaders = GetHeaders(por);
                    });

            using (var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithKeyBuilder(keyBuilder.Object)
                                    .WithCompressor(compressor.Object)
                                    .WithHeaders(moreHeaders))
            {
                renderer.Render(content, path);
            }

            Assert.AreEqual(compressedContent, capturedContentAsString);
            Assert.AreEqual(2, capturedHeaders.Count);
            Assert.AreEqual(headers["test"], capturedHeaders["test"]);
            Assert.AreEqual(moreHeaders["another"], capturedHeaders["another"]);
        }

        HeadersCollection GetHeaders(PutObjectRequest request)
        {
            var propertyInfo = typeof(PutObjectRequest).GetProperty("Headers", BindingFlags.Public | BindingFlags.Instance);
            return (HeadersCollection)propertyInfo.GetValue(request, null);
        }

    }
}
