#region License
/* Teak -- Copyright (C) 2016 GoCarrot Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

#region References
using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Reflection.Emit;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#endregion

[InitializeOnLoad]
public class TeakPackageBuilder : Editor
{
    public static string TeakVersionFile
    {
        get
        {
            return Application.dataPath + "/Teak/TeakVersion.cs";
        }
    }

    static TeakPackageBuilder()
    {
        if(!File.Exists(TeakPackageBuilder.TeakVersionFile))
        {
            GenerateVersionFile();
        }
    }

    public static void BuildUnityPackage()
    {
        GenerateVersionFile();
        string[] assetPaths = new string[] {
            "Assets/Teak",
            "Assets/Plugins/Android/teak.jar",
            "Assets/Plugins/Android/res/layout/teak_big_notif_image_text.xml",
            "Assets/Plugins/Android/res/layout/teak_notif_no_title.xml",
            "Assets/Plugins/Android/res/values/teak_styles.xml",
            "Assets/Plugins/Android/res/values-v21/teak_styles.xml"
        };
        AssetDatabase.ExportPackage(assetPaths, "Teak.unitypackage", ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
    }

    public static void GenerateVersionFile()
    {
        Process proc = new Process();
        proc.StartInfo.FileName = "git";
        proc.StartInfo.Arguments = "describe --tags";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();
        string version = proc.StandardOutput.ReadToEnd().Trim();
        string errors = proc.StandardError.ReadToEnd().Trim();
        if(!String.IsNullOrEmpty(errors))
            UnityEngine.Debug.LogError(errors);
        proc.WaitForExit();

        if(String.IsNullOrEmpty(errors))
        {
            string fileTemplate =
@"/* THIS FILE IS AUTOMATICALLY GENERATED, DO NOT MODIFY IT. */
public class TeakVersion
{{
    public static string Version
    {{
        get
        {{
            return ""{0}"";
        }}
    }}
}}
";
            File.WriteAllText(TeakPackageBuilder.TeakVersionFile,
                string.Format(fileTemplate, version));
        }
    }
}
