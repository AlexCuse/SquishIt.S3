using System.Net;
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

            using(var renderer = new S3Renderer(bucket, s3client.Object, false, keyBuilder.Object))
            {
                renderer.Render(content, path);
            }

            s3client.Verify(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.ContentBody == content &&
                                                                                por.CannedACL == S3CannedACL.PublicRead)));
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

            using(var renderer = new S3Renderer(bucket, s3client.Object, false, keyBuilder.Object))
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

            using(var renderer = new S3Renderer(bucket, s3client.Object, true, keyBuilder.Object))
            {
                renderer.Render(content, path);
            }

            s3client.Verify(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.ContentBody == content &&
                                                                                por.CannedACL == S3CannedACL.PublicRead)));
        }
    }
}
