using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public static class PlayerSelectionBuildRunner
{
    const string Trigger=".utmp/player-selection/build.txt";
    static PlayerSelectionBuildRunner()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Trigger))return;
        string command=File.ReadAllText(Trigger).Trim();File.Delete(Trigger);
        if(command=="release"){UiBuildRunner.BuildWindows();return;}
        if(command=="smoke")BuildSmoke();
    }
    [MenuItem("FPSshogi/Build player selection verification")]
    public static void BuildSmoke()
    {
        Directory.CreateDirectory("Builds/PlayerSelectionSmoke");Directory.CreateDirectory("SelectionReview");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{"Assets/App/Scenes/Title.unity","Assets/App/Scenes/Main.unity"},
            locationPathName="Builds/PlayerSelectionSmoke/FPSshogi.exe",target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.None,extraScriptingDefines=new[]{"FPS_SELECTION_SMOKE"}
        });
        File.WriteAllText("SelectionReview/player-smoke-build.txt",$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nTime: {report.summary.totalTime}\n");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Player selection verification build failed");
    }
}
