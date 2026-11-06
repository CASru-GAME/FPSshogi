using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public static class GameplayBuildRunner
{
    const string Trigger = ".utmp/gameplay-fix/build-player.txt";
    static GameplayBuildRunner() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
        string command = File.ReadAllText(Trigger).Trim(); File.Delete(Trigger);
        if (command == "release") { UiBuildRunner.BuildWindows(); return; }
        if (command == "smoke") BuildSmoke();
    }
    [MenuItem("FPSshogi/Build gameplay verification player")]
    public static void BuildSmoke()
    {
        Directory.CreateDirectory("Builds/GameplaySmoke"); Directory.CreateDirectory("GameplayReview");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/App/Scenes/Title.unity", "Assets/App/Scenes/Main.unity" },
            locationPathName = "Builds/GameplaySmoke/FPSshogi.exe", target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None, extraScriptingDefines = new[] { "FPS_GAMEPLAY_SMOKE" }
        });
        File.WriteAllText("GameplayReview/player-smoke-build.txt", $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nTime: {report.summary.totalTime}\n");
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Gameplay verification build failed");
    }
}
