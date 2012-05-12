using System;
using System.Linq;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Moq;
using NUnit.Framework;

namespace SquishIt.S3.Tests
{
    [TestFixture]
    public class CloudFrontInvalidatorTest
    {
        [Test]
        public void Invalidate()
        {
            var cloudfrontClient = new Mock<AmazonCloudFront>();

            var distributionId = Guid.NewGuid().ToString();
            var bucket = Guid.NewGuid().ToString();
            var distribution = bucket + ".s3.amazonaws.com";
            var key = Guid.NewGuid().ToString();

            var listDistributionsResponse = new ListDistributionsResponse();
            listDistributionsResponse.Distribution.Add(new CloudFrontDistribution
                {
                    Id = distributionId,
                    DistributionConfig = new CloudFrontDistributionConfig
                    {
                        S3Origin = new S3Origin(distribution, null)
                    }
                });

            cloudfrontClient.Setup(cfc => cfc.ListDistributions())
                .Returns(listDistributionsResponse);

            var invalidator = new CloudFrontInvalidator(cloudfrontClient.Object);
            invalidator.InvalidateObject(bucket, key);

            cloudfrontClient.Verify(cfc => cfc.PostInvalidation(It.Is<PostInvalidationRequest>(pir => pir.DistributionId == distributionId 
                && pir.InvalidationBatch.Paths.Count == 1 
                && pir.InvalidationBatch.Paths.First() == key)));
        }
    }
}
