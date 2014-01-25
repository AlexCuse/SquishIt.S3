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
            var s3client =
                AWSClientFactory.CreateAmazonS3Client(
                    ConfigurationManager.AppSettings["aws.accessKey"],
                    ConfigurationManager.AppSettings["aws.secretKey"],
                    RegionEndpoint.USEast1);

            var bucketName = ConfigurationManager.AppSettings["aws.bucketName"];

            var renderer = S3Renderer.Create(s3client)
                .WithBucketName(bucketName)
                .WithDefaultKeyBuilder(HttpContext.Current.Request.PhysicalApplicationPath,
                                                HttpContext.Current.Request.ApplicationPath)
                .WithCannedAcl(S3CannedACL.PublicRead) as IRenderer;

            Bundle.ConfigureDefaults()
                .UseReleaseRenderer(renderer)
                .UseDefaultOutputBaseHref("http://s3.amazonaws.com/" + bucketName);
        }
    }
}