using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;

namespace SquishIt.S3
{
    public interface IInvalidator
    {
        void InvalidateObject(string bucket, string key);
    }

    class CloudFrontInvalidator : IDisposable, IInvalidator
    {
        const string amazonBucketUriSuffix = ".s3.amazonaws.com";
        const string dateFormatWithMilliseconds = "yyyy-MM-dd hh:mm:ss.ff";
        readonly AmazonCloudFront _cloudFrontClient;

        public CloudFrontInvalidator(AmazonCloudFront cloudFrontClient)
        {
            _cloudFrontClient = cloudFrontClient;
        }

        public void InvalidateObject(string bucket, string key)
        {
            var distId = GetDistributionIdFor(bucket);
            if(!string.IsNullOrWhiteSpace(distId))
            {
                var invalidationRequest = new PostInvalidationRequest()
                    .WithDistribtionId(distId)
                    .WithInvalidationBatch(new InvalidationBatch(DateTime.Now.ToString(dateFormatWithMilliseconds), new List<string> { key }));

                _cloudFrontClient.PostInvalidation(invalidationRequest);
            }
        }

        Dictionary<string, string> distributionNameAndIds;

        string GetDistributionIdFor(string bucketName)
        {
            distributionNameAndIds = distributionNameAndIds ??
                _cloudFrontClient.ListDistributions()
                .Distribution
                .ToDictionary(cfd =>
                    cfd.DistributionConfig.S3Origin.DNSName.Replace(amazonBucketUriSuffix, ""),
                    cfd => cfd.Id);

            string id = null;
            distributionNameAndIds.TryGetValue(bucketName, out id);
            return id;
        }

        public void Dispose()
        {
            _cloudFrontClient.Dispose();
        }
    }
}
