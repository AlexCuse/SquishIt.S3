using System;
using System.Collections.Generic;
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
            var cloudfrontClient = new Mock<IAmazonCloudFront>();

            var distributionId = Guid.NewGuid().ToString();
            var bucket = Guid.NewGuid().ToString();
            var distribution = bucket + ".s3.amazonaws.com";
            var key = Guid.NewGuid().ToString();
            var key2 = Guid.NewGuid().ToString();

            var listDistributionsResponse = new ListDistributionsResponse()
                {
                    DistributionList = new DistributionList()
                        {
                            Items = new List<DistributionSummary>
                                {
                                    new DistributionSummary
                                    {
                                        Id = distributionId,
                                        DomainName = distribution,
                                        Origins = new Origins
                                        {
                                            Quantity = 1,
                                            Items = new List<Origin>
                                            {
                                                new Origin
                                                {
                                                    DomainName = distribution
                                                }
                                            }
                                        }
                                    }
                                }
                        }
                };
            
            cloudfrontClient.Setup(cfc => cfc.ListDistributions())
                .Returns(listDistributionsResponse);

            var invalidator = new CloudFrontInvalidator(cloudfrontClient.Object);
            invalidator.InvalidateObject(bucket, key);
            
            cloudfrontClient.Verify(cfc => cfc.CreateInvalidation(It.IsAny<CreateInvalidationRequest>()), Times.Never());
            
            invalidator.InvalidateObject(bucket, key2);

            cloudfrontClient.Verify(cfc => cfc.CreateInvalidation(It.IsAny<CreateInvalidationRequest>()), Times.Never());

            invalidator.Flush();

            cloudfrontClient.Verify(cfc => cfc.CreateInvalidation(It.Is<CreateInvalidationRequest>(pir => pir.DistributionId == distributionId
                && pir.InvalidationBatch.Paths.Quantity == 1
                && pir.InvalidationBatch.Paths.Items.First() == "/" + key
                && pir.InvalidationBatch.Paths.Items.Skip(1).First() == "/" + key2)));
        }

        [Test]
        public void Only_Retrieve_Distributions_Once()
        {
            var cloudfrontClient = new Mock<IAmazonCloudFront>();

            var distributionId = Guid.NewGuid().ToString();
            var bucket = Guid.NewGuid().ToString();
            var distribution = bucket + ".s3.amazonaws.com";
            var key = "/" + Guid.NewGuid().ToString();

            var listDistributionsResponse = new ListDistributionsResponse()
            {
                DistributionList = new DistributionList()
                {
                    Items = new List<DistributionSummary>
                                {
                                    new DistributionSummary
                                    {
                                        Id = distributionId,
                                        DomainName = distribution,
                                        Origins = new Origins
                                        {
                                            Quantity = 1,
                                            Items = new List<Origin>
                                            {
                                                new Origin
                                                {
                                                    DomainName = distribution
                                                }
                                            }
                                        }
                                    }
                                }
                }
            };
            
            cloudfrontClient.Setup(cfc => cfc.ListDistributions())
                .Returns(listDistributionsResponse);

            var invalidator = new CloudFrontInvalidator(cloudfrontClient.Object);
            invalidator.InvalidateObject(bucket, key);
            invalidator.InvalidateObject(bucket, key);

            cloudfrontClient.Verify(cfc => cfc.ListDistributions(), Times.Once());
        }

        [Test]
        public void Flush()
        {
            var cloudfrontClient = new Mock<IAmazonCloudFront>();

            var distributionId = Guid.NewGuid().ToString();
            var bucket = Guid.NewGuid().ToString();
            var distribution = bucket + ".s3.amazonaws.com";
            var key = "/" + Guid.NewGuid();
            var key2 = "/" + Guid.NewGuid();

            var listDistributionsResponse = new ListDistributionsResponse()
            {
                DistributionList = new DistributionList()
                {
                    Items = new List<DistributionSummary>
                                {
                                    new DistributionSummary
                                    {
                                        Id = distributionId,
                                        DomainName = distribution,
                                        Origins = new Origins
                                        {
                                            Quantity = 1,
                                            Items = new List<Origin>
                                            {
                                                new Origin
                                                {
                                                    DomainName = distribution
                                                }
                                            }
                                        }
                                    }
                                }
                }
            };

            cloudfrontClient.Setup(cfc => cfc.ListDistributions())
                .Returns(listDistributionsResponse);

            var invalidator = new CloudFrontInvalidator(cloudfrontClient.Object);
            invalidator.InvalidateObject(bucket, key);
            invalidator.InvalidateObject(bucket, key2);

            invalidator.Flush();

            cloudfrontClient.Verify(cfc => cfc.ListDistributions(), Times.Once());   
            
            cloudfrontClient.Verify(cfc => cfc.CreateInvalidation(It.Is<CreateInvalidationRequest>(cir =>
                cir.InvalidationBatch.Paths.Items.Count == 2    
                && cir.InvalidationBatch.Paths.Items.Any(i => i == key)
                && cir.InvalidationBatch.Paths.Items.Any(i => i == key2))));
        }
    }
}
