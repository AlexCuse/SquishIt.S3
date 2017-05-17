using System.Configuration;
using System.Web;
using Amazon;
using Amazon.CloudFront;
using Amazon.Runtime;
using Amazon.S3;
using SquishIt.Framework;
using SquishIt.Framework.Renderers;

namespace SquishIt.S3.SampleProject.App_Start
{
    public class SquishItConfig
    {
        public static void Initialize()
        {
            var awsAccessKey = ConfigurationManager.AppSettings["aws.accessKey"];
            var awsSecretKey = ConfigurationManager.AppSettings["aws.secretKey"];
            var bucketName = ConfigurationManager.AppSettings["aws.bucketName"];
            //var baseCdnUrl = ConfigurationManager.AppSettings["aws.baseCdnUrl"];

            var awsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

            using (var s3client = new AmazonS3Client(awsCredentials, RegionEndpoint.SAEast1))
            using(var cfclient = new AmazonCloudFrontClient(awsCredentials))
            using (var invalidator = new CloudFrontInvalidator(cfclient))
            {
                var renderer = S3Renderer.Create(s3client)
                    .WithBucketName(bucketName)
                    .WithDefaultKeyBuilder(HttpContext.Current.Request.PhysicalApplicationPath,
                        HttpContext.Current.Request.ApplicationPath)
                    .WithInvalidator(invalidator)
                    .WithCannedAcl(S3CannedACL.PublicRead)
                    .WithOverwriteBehavior(true) as IRenderer;

                //dont recommend this usage pattern if cloudfront invalidation is needed, because of the need to tightly control renderer lifecycle for request pooling

                //Bundle.ConfigureDefaults()
                //    .UseReleaseRenderer(renderer)
                //    .UseDefaultOutputBaseHref(baseCdnUrl);

                Bundle.ConfigureDefaults().UseDefaultOutputBaseHref("//s3.amazonaws.com/" + bucketName);

                Bundle.JavaScript()
                    .Add("~/Scripts/jquery-1.7.1.js")
                    .WithReleaseFileRenderer(renderer)
                    //.WithOutputBaseHref(baseCdnUrl)
                    .ForceRelease()
                    .AsNamed("combined-jq.js", "/combined-jq.js");
            }//when dispose is called, the invalidator will be flushed, sending an invalidation request for *all* processed bundles to cloudfront
        }
    }
}