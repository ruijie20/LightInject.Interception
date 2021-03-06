#load "nuget:Dotnet.Build, 0.3.8"
#load "nuget:github-changelog, 0.1.5"
#load "BuildContext.csx"
using static FileUtils;
using static Internalizer;
using static xUnit;
using static DotNet;
using static ChangeLog;
using static ReleaseManagement;

Build(projectFolder, Git.Default.GetCurrentCommitHash());
Test(testProjectFolder);
AnalyzeCodeCoverage(pathToTestAssembly, $"+[{projectName}]*");
Pack(projectFolder, nuGetArtifactsFolder);

using(var sourceBuildFolder = new DisposableFolder())
{
    string pathToSourceProjectFolder = Path.Combine(sourceBuildFolder.Path,"LightInject.Interception");
    Copy(solutionFolder, sourceBuildFolder.Path, new [] {".vs", "obj"});
    Internalize(pathToSourceProjectFolder, exceptTheseTypes);    
    DotNet.Build(Path.Combine(sourceBuildFolder.Path,"LightInject.Interception"));
    using(var nugetPackFolder = new DisposableFolder())
    {
        var contentFolder = CreateDirectory(nugetPackFolder.Path, "content","netstandard1.1", "LightInject.Interception");
        Copy("LightInject.Interception.Source.nuspec", nugetPackFolder.Path);
        string pathToSourceFileTemplate = Path.Combine(contentFolder, "LightInject.Interception.cs.pp");
        Copy(Path.Combine(pathToSourceProjectFolder, "LightInject.Interception.cs"), pathToSourceFileTemplate);        
        FileUtils.ReplaceInFile(@"namespace \S*", $"namespace $rootnamespace$.{projectName}", pathToSourceFileTemplate);
        NuGet.Pack(nugetPackFolder.Path, nuGetArtifactsFolder, version);
    }
}

if (BuildEnvironment.IsSecure)
    {
        await CreateReleaseNotes();

        if (Git.Default.IsTagCommit())
        {
            Git.Default.RequreCleanWorkingTree();
            await ReleaseManagerFor(owner, projectName,BuildEnvironment.GitHubAccessToken)
            .CreateRelease(Git.Default.GetLatestTag(), pathToReleaseNotes, Array.Empty<ReleaseAsset>());
            NuGet.TryPush(nuGetArtifactsFolder);            
        }
    }

 private async Task CreateReleaseNotes()
{
    Logger.Log("Creating release notes");        
    var generator = ChangeLogFrom(owner, projectName, BuildEnvironment.GitHubAccessToken).SinceLatestTag();
    if (!Git.Default.IsTagCommit())
    {
        generator = generator.IncludeUnreleased();
    }
    await generator.Generate(pathToReleaseNotes);
}   