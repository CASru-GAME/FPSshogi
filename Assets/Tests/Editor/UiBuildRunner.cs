using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class UiBuildRunner
{
    [MenuItem("FPSshogi/Build Windows")]
    public static void BuildWindows()
    {
        Directory.CreateDirectory("Builds/FPSshogi");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{"Assets/App/Scenes/Title.unity","Assets/App/Scenes/Main.unity"},
            locationPathName="Builds/FPSshogi/FPSshogi.exe",
            target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.None
        });
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/ui-build-results.txt",$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\nTime: {report.summary.totalTime}\nPath: {report.summary.outputPath}\n");
        if(report.summary.result!=BuildResult.Succeeded) throw new System.Exception("Windows build failed; see Logs/ui-build-results.txt");
        File.Copy("Assets/Resources/UI/OFL.txt","Builds/FPSshogi/Noto-OFL.txt",true);
    }
}
