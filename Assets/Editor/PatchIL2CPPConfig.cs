#region References
using UnityEditor;
using UnityEditor.Callbacks;

using System;
using System.IO;
using System.Diagnostics;
#endregion

public class PatchIL2CPPConfig
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject)
    {
        if(target != BuildTarget.iOS) return;

        Process proc = new Process();
        proc.StartInfo.FileName = "patch";
        proc.StartInfo.Arguments = "-p0";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardInput = true;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.StartInfo.WorkingDirectory = pathToBuildProject;
        proc.Start();
        proc.StandardInput.Write(
@"
--- Libraries/libil2cpp/include/il2cpp-config.h 2016-05-31 15:51:28.000000000 -0700
+++ Libraries/libil2cpp/include/il2cpp-config copy.h    2016-05-31 15:52:08.000000000 -0700
@@ -178,7 +178,11 @@
 #endif
 
 #if IL2CPP_COMPILER_MSVC || IL2CPP_TARGET_DARWIN || defined(__ARMCC_VERSION)
+#if IL2CPP_TARGET_DARWIN && __has_attribute(noreturn)
+#define NORETURN __attribute__((noreturn))
+#else
 #define NORETURN __declspec(noreturn)
+#endif
 #else
 #define NORETURN
 #endif
");
        proc.StandardInput.Close();

        UnityEngine.Debug.Log(proc.StandardOutput.ReadToEnd());
        string errors = proc.StandardError.ReadToEnd();
        if(!String.IsNullOrEmpty(errors))
            UnityEngine.Debug.LogError(errors);
        proc.WaitForExit();
    }
}
