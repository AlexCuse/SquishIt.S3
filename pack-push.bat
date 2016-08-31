msbuild SquishIt.S3.sln /p:Configuration=Release /p:Platform="Any CPU"
nuget pack squishit.s3.nuspec
for %%p in (*.nupkg) do (
	nuget push %%p -Source https://www.nuget.org/api/v2/package
	del %%p
)
