using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 命令行打包入口（也加了菜单，编辑器里可以点）。
/// 用法（PowerShell）：
///   &amp; "C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe" -batchmode -quit `
///     -projectPath "D:\Gameproject\Fist project" -executeMethod BuildCLI.BuildWindows `
///     -logFile "D:\dsh工作区\_game\build_win.log"
/// </summary>
public static class BuildCLI
{
    static readonly string[] Scenes = { "Assets/Scenes/SampleScene.unity" };

    /// 环境自检：只打一行日志然后退出，用来确认批处理模式能正常跑起来
    public static void Ping()
    {
        Debug.Log("[PING] batchmode ok, version=" + Application.unityVersion);
    }

    [MenuItem("Build/Windows x64")]
    public static void BuildWindows()
    {
        PlayerSettings.productName = "Breakout";
        PlayerSettings.companyName = "DongLiwei";

        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Builds/Windows/Breakout.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        Report(BuildPipeline.BuildPlayer(opts));
    }

    [MenuItem("Build/WebGL")]
    public static void BuildWebGL()
    {
        PlayerSettings.productName = "Breakout";
        PlayerSettings.companyName = "DongLiwei";
        // 关掉压缩：普通静态服务器（python -m http.server / GitHub Pages 之外的简易服务器）也能直接跑
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        Report(BuildPipeline.BuildPlayer(opts));
    }

    static void Report(BuildReport report)
    {
        if (report == null)
        {
            Debug.LogError("[BUILD] report == null");
            EditorApplication.Exit(1);
            return;
        }

        var s = report.summary;
        Debug.Log(string.Format("[BUILD] result={0} errors={1} warnings={2} bytes={3} time={4}",
            s.result, s.totalErrors, s.totalWarnings, s.totalSize, s.totalTime));

        if (s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
