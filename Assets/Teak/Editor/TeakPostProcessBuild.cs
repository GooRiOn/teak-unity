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
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using System;
using System.Diagnostics;
#endregion

public class TeakPostProcessBuild
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject)
    {
#if UNITY_IPHONE
        string objCPath = Application.dataPath + "/Teak/Plugins/iOS";
        Process proc = new Process();
        proc.StartInfo.FileName = "python";
        proc.StartInfo.Arguments = string.Format("Assets/Teak/Editor/ios_post_process.py \"{0}\" \"{1}\" \"{2}\" \"{3}\"", pathToBuildProject, objCPath, TeakSettings.AppId, TeakSettings.APIKey);
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();
        UnityEngine.Debug.Log(proc.StandardOutput.ReadToEnd());
        string errors = proc.StandardError.ReadToEnd();
        if(!String.IsNullOrEmpty(errors))
            UnityEngine.Debug.LogError(errors);
        proc.WaitForExit();
#endif
    }
}
