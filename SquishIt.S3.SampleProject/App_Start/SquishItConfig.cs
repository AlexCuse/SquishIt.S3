using System.Configuration;
using System.Web;
using Amazon;
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
            var baseCdnUrl = ConfigurationManager.AppSettings["aws.baseCdnUrl"];

            var s3client =
                AWSClientFactory.CreateAmazonS3Client(
                    awsAccessKey,
                    awsSecretKey,
                    RegionEndpoint.USEast1);


            var renderer = S3Renderer.Create(s3client)
                .WithBucketName(bucketName)
                .WithDefaultKeyBuilder(HttpContext.Current.Request.PhysicalApplicationPath, HttpContext.Current.Request.ApplicationPath)
                .WithCloudfrontClient(AWSClientFactory.CreateAmazonCloudFrontClient(awsAccessKey, awsSecretKey))
                .WithCannedAcl(S3CannedACL.PublicRead)
                .WithOverwriteBehavior(true) as IRenderer;

            Bundle.ConfigureDefaults()
                .UseReleaseRenderer(renderer)
                .UseDefaultOutputBaseHref(baseCdnUrl);
                //.UseDefaultOutputBaseHref("http://s3.amazonaws.com/" + bucketName);
        }
    }
}