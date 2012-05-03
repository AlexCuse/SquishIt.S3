using System;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using NUnit.Framework;
using SquishIt.S3.Internal;

namespace SquishIt.S3.Tests
{
    [TestFixture]
    public class S3FileWriterTest
    {
        [Test]
        public void Uploads_On_Dispose()
        {
            var s3client = new Mock<AmazonS3>();

            var key = "key";
            var bucket = "bucket";
            var text1 = "start text";
            var text2 = "some text";
            var text3 = "some more text";

            using(var writer = new S3FileWriter(key, bucket, s3client.Object))
            {
                writer.WriteLine(text1);
                writer.Write(text2);
                writer.Write(text3);
            }

            var expectedContent = text1 + Environment.NewLine + text2 + text3;

            s3client.Verify(c => c.PutObject(It.Is<PutObjectRequest>(por => por.Key == key &&
                                                                                por.BucketName == bucket &&
                                                                                por.ContentBody == expectedContent &&
                                                                                por.CannedACL == S3CannedACL.PublicRead)));
        }
    }
}
