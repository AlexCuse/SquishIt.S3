﻿[assembly: WebActivator.PreApplicationStartMethod(typeof($rootnamespace$.App_Start.SquishItS3), "Start")]

namespace $rootnamespace$.App_Start
{
	using SquishIt.Framework;
	using SquishIt.S3;

	public class SquishItS3
	{
		public static void Start()
		{
			var s3client = new AmazonS3Client(awsAccessKey, awsSecretKey);
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
