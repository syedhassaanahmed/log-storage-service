#addin Cake.Coveralls

#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=coveralls.io"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var framework = Argument("framework", "netcoreapp1.1");

var outputDir = "./buildOutput/";
var artifactName = outputDir + "artifact.zip";

var coverageDir = "./coverageOutput/";
var coverageOutput = coverageDir + "coverage.xml";

var projectPath = "./ValidationPipeline.LogStorage";
var projectJsonPath = projectPath + "/project.json";

Task("StartStorageEmulator")
	.Does(() => 
	{
		StartProcess(@"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe",
			new ProcessSettings{ Arguments = "start" });
	});

Task("Clean")
	.Does(() => 
	{
		if (DirectoryExists(outputDir))
			DeleteDirectory(outputDir, recursive:true);

		CreateDirectory(outputDir);

		if (DirectoryExists(coverageDir))
			DeleteDirectory(coverageDir, recursive:true);

		CreateDirectory(coverageDir);
	});

Task("Restore")
	.Does(() => DotNetCoreRestore());

Task("Version")
	.Does(() => 
	{
		GitVersion(new GitVersionSettings
		{
			UpdateAssemblyInfo = true,
			OutputType = GitVersionOutput.BuildServer
		});

		var versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });
		var updatedProjectJson = System.IO.File.ReadAllText(projectJsonPath)
			.Replace("1.0.0-*", versionInfo.NuGetVersion);
			
		System.IO.File.WriteAllText(projectJsonPath, updatedProjectJson);
	});

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("Version")
	.Does(() => 
	{
		var projects = GetFiles("./**/*.xproj");

		var settings = new DotNetCoreBuildSettings
		{
			Framework = framework,
			Configuration = configuration,
		};

		foreach (var project in projects)
		{
			DotNetCoreBuild(project.GetDirectory().FullPath, settings);
		}
	});

Task("TestWithCoverage")
	.IsDependentOn("Build")
	.IsDependentOn("StartStorageEmulator")
	.Does(() => 
	{
		Action<ICakeContext> testAction = tool => 
		{
			tool.DotNetCoreTest("./ValidationPipeline.LogStorage.Tests", new DotNetCoreTestSettings 
			{
				Framework = framework,
				Configuration = configuration
			});
		};

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
		CoverallsIo(coverageOutput, new CoverallsIoSettings()
		{
			RepoToken = "OnGGqF3H1jOotiu3p6430fXY6rWtBfDyN"
		});
	});

Task("Publish")
	.IsDependentOn("TestWithCoverage")
	.Does(() => 
	{
		var settings = new DotNetCorePublishSettings
		{
			Configuration = configuration,
			OutputDirectory = outputDir
		};
					
		DotNetCorePublish(projectPath, settings);
		Zip(outputDir, artifactName);
	});

Task("AppVeyorUpload")
	.WithCriteria(() => BuildSystem.IsRunningOnAppVeyor)
	.IsDependentOn("Publish")
	.Does(() => 
	{
		var files = GetFiles(artifactName);
		foreach (var file in files)
			AppVeyor.UploadArtifact(file.FullPath);
	});

Task("Default")
	.IsDependentOn("AppVeyorUpload")
	.IsDependentOn("CoverallsUpload");

RunTarget(target);