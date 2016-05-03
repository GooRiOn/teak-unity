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
    static TeakPackageBuilder()
    {
        if(!System.IO.File.Exists(Application.dataPath + "/Teak/TeakVersion.dll"))
        {
            GenerateAssembly();
        }
    }

    public static void BuildUnityPackage()
    {
        GenerateAssembly();
        string[] assetPaths = new string[] {
            "Assets/Teak",
            "Assets/Plugins/Android/teak.jar",
        };
        AssetDatabase.ExportPackage(assetPaths, "Teak.unitypackage", ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
    }

    public static void GenerateAssembly()
    {
        Process proc = new Process();
        proc.StartInfo.FileName = "git";
        proc.StartInfo.Arguments = "describe --tags";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();
        string version = proc.StandardOutput.ReadToEnd();
        string errors = proc.StandardError.ReadToEnd();
        if(!String.IsNullOrEmpty(errors))
            UnityEngine.Debug.LogError(errors);
        proc.WaitForExit();

        if(String.IsNullOrEmpty(errors))
        {
            AppDomain myDomain = Thread.GetDomain();
            AssemblyName myAsmName = new AssemblyName();
            myAsmName.Name = "TeakVersion";

            AssemblyBuilder myAsmBuilder = myDomain.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder myModBuilder = myAsmBuilder.DefineDynamicModule(myAsmName.Name, myAsmName.Name + ".dll");
            TypeBuilder myTypeBuilder = myModBuilder.DefineType("TeakVersion", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

            var getMethodBuilder = myTypeBuilder.DefineMethod("get_Version", MethodAttributes.Public | MethodAttributes.Static, typeof(string), Type.EmptyTypes);

            var il = getMethodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldstr, version);
            il.Emit(OpCodes.Ret);

            var propertyBuilder = myTypeBuilder.DefineProperty("Version", PropertyAttributes.None, typeof(string), Type.EmptyTypes);
            propertyBuilder.SetGetMethod(getMethodBuilder);

            myTypeBuilder.CreateType();
            myAsmBuilder.Save(myAsmName.Name + ".dll");
            FileUtil.ReplaceFile(myAsmName.Name + ".dll", Application.dataPath + "/Teak/" + myAsmName.Name + ".dll");
            FileUtil.DeleteFileOrDirectory(myAsmName.Name + ".dll");
        }
    }
}
