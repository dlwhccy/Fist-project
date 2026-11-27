using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// WebGL「体积优化」构建（2026-11-28）
/// 用法：Unity.exe -batchmode -quit -projectPath "&lt;项目&gt;" -executeMethod BuildWebGLSmall.BuildSmall -logFile "&lt;日志&gt;"
/// 说明：IL2CPP 相关的枚举在 Unity 版本间改过名，这里全部用反射调用，避免编译期报错。
/// </summary>
public static class BuildWebGLSmall
{
    static readonly string[] Scenes = { "Assets/Scenes/SampleScene.unity" };

    [MenuItem("Build/WebGL (Small)")]
    public static void BuildSmall()
    {
        PlayerSettings.productName = "Breakout";
        PlayerSettings.companyName = "DongLiwei";

        Try("ManagedStrippingLevel=High", () =>
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High));

        Try("stripEngineCode=true", () => PlayerSettings.stripEngineCode = true);

        // IL2CPP：优先「为体积优化」，枚举名各版本不同，逐个试
        InvokeEnumMethod("SetIl2CppCodeGeneration", "UnityEditor.Il2CppCodeGeneration",
            new[] { "OptimizeForSize", "Size", "Minimal" });

        InvokeEnumMethod("SetIl2CppCompilerConfiguration", "UnityEditor.Il2CppCompilerConfiguration",
            new[] { "Release", "Master" });

        Try("exceptionSupport=ExplicitlyThrownExceptionsOnly", () =>
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly);

        Try("dataCaching=true", () => PlayerSettings.WebGL.dataCaching = true);

        Try("compressionFormat=Disabled", () =>
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled);

        Debug.Log("[BUILD] 优化设置: stripping=" + PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL) +
                  " stripEngineCode=" + PlayerSettings.stripEngineCode +
                  " dataCaching=" + PlayerSettings.WebGL.dataCaching +
                  " exceptionSupport=" + PlayerSettings.WebGL.exceptionSupport);

        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        Report(BuildPipeline.BuildPlayer(opts));
    }

    /// <summary>反射调用 PlayerSettings 上「(NamedBuildTarget, 枚举)」形式的方法</summary>
    static void InvokeEnumMethod(string methodName, string enumTypeName, string[] valueNames)
    {
        try
        {
            var m = typeof(PlayerSettings).GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(NamedBuildTarget), typeof(int) }, null);

            var enumType = Type.GetType(enumTypeName + ", UnityEditor");
            if (enumType == null && m != null)
                enumType = m.GetParameters()[1].ParameterType;
            if (enumType == null) { Debug.LogWarning("[BUILD] 找不到枚举 " + enumTypeName); return; }

            foreach (var vn in valueNames)
            {
                var val = Enum.Parse(enumType, vn, false);
                if (val == null) continue;
                if (m != null)
                {
                    m.Invoke(null, new object[] { NamedBuildTarget.WebGL, Convert.ToInt32(val) });
                    Debug.Log("[BUILD] " + methodName + " = " + vn);
                    return;
                }
            }
            Debug.LogWarning("[BUILD] 方法或枚举值不可用: " + methodName);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BUILD] 设置失败 " + methodName + " : " + (e.InnerException != null ? e.InnerException.Message : e.Message));
        }
    }

    static void Try(string name, Action act)
    {
        try { act(); }
        catch (Exception e) { Debug.LogWarning("[BUILD] 跳过设置 " + name + " : " + e.Message); }
    }

    static void Report(BuildReport report)
    {
        if (report == null) { Debug.LogError("[BUILD] report == null"); EditorApplication.Exit(1); return; }
        var s = report.summary;
        Debug.Log(string.Format("[BUILD] result={0} errors={1} warnings={2} bytes={3} time={4}",
            s.result, s.totalErrors, s.totalWarnings, s.totalSize, s.totalTime));
        if (s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
