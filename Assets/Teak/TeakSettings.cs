#if UNITY_EDITOR
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
using UnityEditor;
#endregion

[InitializeOnLoad]
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
        get { return Instance.mAppId; }
#if UNITY_EDITOR
        set
        {
            string appId = value.Trim();
            if(appId != Instance.mAppId)
            {
                Instance.mAppValid = false;
                Instance.mAppStatus = "";
                Instance.mAppId = appId;
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
            string apiKey = value.Trim();
            if(apiKey != Instance.mAPIKey)
            {
                Instance.mAppValid = false;
                Instance.mAppStatus = "";
                Instance.mAPIKey = apiKey;
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
            string gcmSenderId = value.Trim();
            if(gcmSenderId != Instance.mGCMSenderId)
            {
                Instance.mAppValid = false;
                Instance.mAppStatus = "";
                Instance.mGCMSenderId = gcmSenderId;
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

    public static bool SimulateOpenedWithPush
    {
        get { return Instance.mSimulateOpenedWithPush; }
#if UNITY_EDITOR
        set
        {
            if(value != Instance.mSimulateOpenedWithPush)
            {
                Instance.mSimulateOpenedWithPush = value;
                DirtyEditor();
            }
        }
#endif
    }

    public static string SimulatedTeakRewardId
    {
        get { return Instance.mSimulateTeakRewardId; }
#if UNITY_EDITOR
        set
        {
            string teakNotifId = value.Trim();
            if(teakNotifId != Instance.mSimulateTeakRewardId)
            {
                Instance.mSimulateTeakRewardId = teakNotifId;
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
    [SerializeField]
    private bool mAppValid = false;
    [SerializeField]
    private string mAppStatus = "";
    [SerializeField]
    private bool mSimulateOpenedWithPush = false;
    [SerializeField]
    private string mSimulateTeakRewardId = "";

    private static TeakSettings mInstance;
}
#endif
