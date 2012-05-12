using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using NUnit.Framework;

namespace SquishIt.S3.Tests
{
    [TestFixture]
    public class S3RendererTest
    {
        [Test]
        public void Render_Uploads_If_File_Doesnt_Exist()
        {
            var s3client = new Mock<AmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);

            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
                Throws(new AmazonS3Exception("", HttpStatusCode.NotFound));

            using(var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithKeyBuilder(keyBuilder.Object))
            {
                renderer.Render(content, path);
            }

            s3client.Verify(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.ContentBody == content &&
                                                                                por.CannedACL == S3CannedACL.NoACL)));
        }

        [Test]
        public void Render_Skips_Upload_If_File_Exists()
        {
            var s3client = new Mock<AmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);
            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
                Returns(new GetObjectMetadataResponse());

            using(var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithKeyBuilder(keyBuilder.Object))
            {
                renderer.Render(content, path);
            }

            s3client.VerifyAll();
        }

        [Test]
        public void Render_Uploads_If_File_Exists_And_ForceUpload()
        {
            var s3client = new Mock<AmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);
            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
                Returns(new GetObjectMetadataResponse());

            using(var renderer = S3Renderer.Create(s3client.Object)
                                    .WithBucketName(bucket)
                                    .WithOverwriteBehavior(true)
                                    .WithKeyBuilder(keyBuilder.Object))
            {
                renderer.Render(content, path);
            }

            s3client.Verify(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.ContentBody == content &&
                                                                                por.CannedACL == S3CannedACL.NoACL)));
        }

        [Test]
        public void WithCannedACL()
        {
            var s3client = new Mock<AmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            var cannedAcl = S3CannedACL.BucketOwnerFullControl;

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);
            s3client.Setup(c => c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key))).
               Throws(new AmazonS3Exception("", HttpStatusCode.NotFound));

            using(var renderer = S3Renderer.Create(s3client.Object)
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
            var s3client = new Mock<AmazonS3>();
            var keyBuilder = new Mock<IKeyBuilder>();

            var key = "key";
            var bucket = "bucket";
            var path = "path";
            var content = "content";
            var headerValue = "test value";
            var headerName = "cache-control";
            var headers = new NameValueCollection {{headerName, headerValue}};

            keyBuilder.Setup(kb => kb.GetKeyFor(path)).Returns(key);
            s3client.Setup(
                c =>
                c.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(gomr => gomr.BucketName == bucket && gomr.Key == key)))
                .
                Throws(new AmazonS3Exception("", HttpStatusCode.NotFound));

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
            var s3client = new Mock<AmazonS3>();
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
                Throws(new AmazonS3Exception("", HttpStatusCode.NotFound));

            using(var renderer = S3Renderer.Create(s3client.Object)
                .WithBucketName(bucket)
                .WithOverwriteBehavior(false)
                .WithKeyBuilder(keyBuilder.Object)
                .WithInvalidator(invalidator.Object))
            {
                renderer.Render(content, path);
            }

            invalidator.Verify(i => i.InvalidateObject(bucket, key));
        }

        NameValueCollection GetHeaders(S3Request request)
        {
            var propertyInfo = typeof(S3Request).GetProperty("Headers", BindingFlags.NonPublic | BindingFlags.Instance);
            return (NameValueCollection)propertyInfo.GetValue(request, null);
        }
    }
}
