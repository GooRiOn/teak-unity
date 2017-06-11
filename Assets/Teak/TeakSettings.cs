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

#if UNITY_EDITOR
using UnityEditor;
#endif
#endregion

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class TeakSettings : ScriptableObject
{
    const string teakSettingsAssetName = "TeakSettings";
    const string teakSettingsPath = "Resources";
    const string teakSettingsAssetExtension = ".asset";

    static TeakSettings Instance
    {
        get
        {
            if(mInstance == null)
            {
                mInstance = Resources.Load(teakSettingsAssetName) as TeakSettings;
                if(mInstance == null)
                {
                    // If not found, autocreate the asset object.
                    mInstance = CreateInstance<TeakSettings>();
#if UNITY_EDITOR
                    System.IO.Directory.CreateDirectory(Path.Combine(Application.dataPath, teakSettingsPath));

                    AssetDatabase.CreateAsset(mInstance, Path.Combine(
                        Path.Combine("Assets", teakSettingsPath),
                            teakSettingsAssetName + teakSettingsAssetExtension));

                    AssetDatabase.Refresh();
#endif
                }
            }
            return mInstance;
        }
    }

    public static string AppId
    {
        get { return Instance.mAppId; }
#if UNITY_EDITOR
        set
        {
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mAppId)
            {
                Instance.mAppId = valueTrim;
                DirtyEditor();
            }
        }
#endif
    }

    public static string APIKey
    {
        get { return Instance.mAPIKey; }
#if UNITY_EDITOR
        set
        {
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mAPIKey)
            {
                Instance.mAPIKey = valueTrim;
                DirtyEditor();
            }
        }
#endif
    }

    public static string GCMSenderId
    {
        get { return Instance.mGCMSenderId; }
#if UNITY_EDITOR
        set
        {
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mGCMSenderId)
            {
                Instance.mGCMSenderId = valueTrim;
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
    private string mAppId = "";
    [SerializeField]
    private string mAPIKey = "";
    [SerializeField]
    private string mGCMSenderId = "";

    private static TeakSettings mInstance;
}
