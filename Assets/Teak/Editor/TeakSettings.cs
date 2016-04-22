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
using UnityEngine;
#endregion

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
#endif
public class TeakSettings : ScriptableObject
{
    const string teakSettingsAssetName = "TeakSettings";
    const string teakSettingsPath = "Teak/Resources";
    const string teakSettingsAssetExtension = ".asset";

    static TeakSettings Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = Resources.Load(teakSettingsAssetName) as TeakSettings;
                if (mInstance == null)
                {
                    // If not found, autocreate the asset object.
                    mInstance = CreateInstance<TeakSettings>();
#if UNITY_EDITOR
                    string properPath = Path.Combine(Application.dataPath, teakSettingsPath);
                    if (!Directory.Exists(properPath))
                    {
                        AssetDatabase.CreateFolder("Assets/Teak", "Resources");
                    }

                    string fullPath = Path.Combine(Path.Combine("Assets", teakSettingsPath),
                                                   teakSettingsAssetName + teakSettingsAssetExtension
                                                  );
                    AssetDatabase.CreateAsset(mInstance, fullPath);
#endif
                }
            }
            return mInstance;
        }
    }

    public static string AppId
    {
        get { return Instance.appId; }
#if UNITY_EDITOR
        set
        {
            string appId = value.Trim();
            if(appId != Instance.appId)
            {
                Instance.mAppValid = false;
                Instance.mAppStatus = "";
                Instance.appId = appId;
                DirtyEditor();
            }
        }
#endif
    }

    public static string APIKey
    {
        get { return Instance.apiKey; }
#if UNITY_EDITOR
        set
        {
            string apiKey = value.Trim();
            if(apiKey != Instance.apiKey)
            {
                Instance.mAppValid = false;
                Instance.mAppStatus = "";
                Instance.apiKey = apiKey;
                DirtyEditor();
            }
        }
#endif
    }

    public static bool AppValid
    {
        get { return Instance.mAppValid; }
#if UNITY_EDITOR
        set
        {
            if(value != Instance.mAppValid)
            {
                Instance.mAppValid = value;
                DirtyEditor();
            }
        }
#endif
    }

    public static string AppStatus
    {
        get { return Instance.mAppStatus; }
#if UNITY_EDITOR
        set
        {
            string appStatus = value.Trim();
            if(appStatus != Instance.mAppStatus)
            {
                Instance.mAppStatus = appStatus;
                DirtyEditor();
            }
        }
#endif
    }

#if UNITY_EDITOR
    [MenuItem("Edit/Teak")]
    public static void Edit()
    {
        Selection.activeObject = Instance;
    }

    private static void DirtyEditor()
    {
        EditorUtility.SetDirty(Instance);
    }
#endif

    [SerializeField]
    private string appId = "";
    [SerializeField]
    private string apiKey = "";
    [SerializeField]
    private bool mAppValid = false;
    [SerializeField]
    private string mAppStatus = "";

    private static TeakSettings mInstance;
}
