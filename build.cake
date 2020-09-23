#load "nuget:?package=PleOps.Cake&prerelease"

Task("Define-Project")
    .Description("Fill specific project information")
    .Does<BuildInfo>(info =>
{
    info.AddLibraryProjects("SceneGate.PC");
    info.AddTestProjects("SceneGate.PC.Tests");
});

Task("Default")
    .IsDependentOn("Stage-Artifacts");

string target = Argument("target", "Default");
RunTarget(target);
