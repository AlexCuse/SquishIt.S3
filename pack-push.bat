msbuild SquishIt.S3.sln /p:Configuration=Release
nuget pack squishit.s3.nuspec
for %%p in (*.nupkg) do (
	nuget push %%p
	del %%p
)
