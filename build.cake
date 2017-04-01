#addin Cake.Coveralls
#addin Cake.Docker

#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=coveralls.io"

var testProj = "./ValidationPipeline.LogStorage.Tests/ValidationPipeline.LogStorage.Tests.csproj";
var testSettings = new DotNetCoreTestSettings 
{
	Framework = "netcoreapp1.1",
	Configuration = "Release"
};

var coverageDir = "./coverageOutput/";
var coverageOutput = coverageDir + "coverage.xml";

Task("StartStorageEmulator")
	.WithCriteria(() => !BuildSystem.IsRunningOnTravisCI)
	.Does(() => 
	{
		StartProcess(@"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe",
			new ProcessSettings{ Arguments = "start" });
	});

Task("Clean")
	.Does(() => 
	{
		if (DirectoryExists(coverageDir))
			DeleteDirectory(coverageDir, recursive:true);

		CreateDirectory(coverageDir);

		DockerComposeRm(new DockerComposeRmSettings { Force = true });
	});

Task("Restore")
	.Does(() => DotNetCoreRestore("ValidationPipeline.LogStorage.sln"));

Task("TestWithCoverage")
	.WithCriteria(() => !BuildSystem.IsRunningOnTravisCI)
	.IsDependentOn("StartStorageEmulator")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does(() => 
	{
		Action<ICakeContext> testAction = tool => tool.DotNetCoreTest(testProj, testSettings);

		OpenCover(testAction, coverageOutput, new OpenCoverSettings 
		{
			OldStyle = true, // Needed for .NET Core
			Register = "user",
			ArgumentCustomization = args => args.Append("-hideskipped:all")
		}.WithFilter("+[ValidationPipeline.LogStorage*]*"));

		ReportGenerator(coverageOutput, coverageDir);
	});

Task("CoverallsUpload")
	.WithCriteria(() => FileExists(coverageOutput))
	.WithCriteria(() => BuildSystem.IsRunningOnAppVeyor)
	.IsDependentOn("TestWithCoverage")	
	.Does(() => 
	{
		CoverallsIo(coverageOutput, new CoverallsIoSettings
		{
			RepoToken = EnvironmentVariable("coveralls_repo_token")
		});
	});

Task("Build")
	.WithCriteria(() => !BuildSystem.IsRunningOnAppVeyor)
	.IsDependentOn("TestWithCoverage")
	.IsDependentOn("TestWithoutCoverage")
	.Does(() => 
	{
		DockerComposeUp(new DockerComposeUpSettings { Files = new [] { "docker-compose.ci.build.yml" } });
		DockerComposeBuild();
	});

Task("DockerPush")
	.WithCriteria(() => BuildSystem.IsRunningOnTravisCI)
	.IsDependentOn("Build")
	.Does(() => 
	{
		
	});

Task("Default")
	.IsDependentOn("DockerPush")
	.IsDependentOn("CoverallsUpload");

RunTarget("Default");