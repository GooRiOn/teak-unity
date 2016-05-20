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

using TeakEditor;
#endregion

[InitializeOnLoad]
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
                Instance.mAppValid = false;
                Instance.mAppStatus = "";
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
                Instance.mAppValid = false;
                Instance.mAppStatus = "";
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
                Instance.mAppValid = false;
                Instance.mAppStatus = "";
                Instance.mGCMSenderId = valueTrim;
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
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mAppStatus)
            {
                Instance.mAppStatus = valueTrim;
                DirtyEditor();
            }
        }
#endif
    }

    public static bool SimulateDeepLink
    {
        get { return PlayerPrefs.GetInt("simulate_deep_link") == 1; }
#if UNITY_EDITOR
        set
        {
            PlayerPrefs.SetInt("simulate_deep_link", value ? 1 : 0);
        }
#endif
    }

#if UNITY_EDITOR
    public static string SimulateDeepLinkEditorKey
    {
        get { return Instance.mSimulateDeepLinkEditorKey; }
        set
        {
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mSimulateDeepLinkEditorKey)
            {
                Instance.mSimulateDeepLinkEditorKey = valueTrim;
                DirtyEditor();
            }
        }
    }
#endif

    public static bool SimulateOpenedWithNotification
    {
        get { return PlayerPrefs.GetInt("simulate_opened_with_notification") == 1; }
#if UNITY_EDITOR
        set
        {
            PlayerPrefs.SetInt("simulate_opened_with_notification", value ? 1 : 0);
        }
#endif
    }

    public static bool SimulateRewardReply
    {
        get { return Instance.mSimulateRewardReply; }
#if UNITY_EDITOR
        set
        {
            if(value != Instance.mSimulateRewardReply)
            {
                Instance.mSimulateRewardReply = value;
                DirtyEditor();
            }
        }
#endif
    }

    public static bool SimulateNotificationPayload
    {
        get { return Instance.mSimulateNotificationPayload; }
#if UNITY_EDITOR
        set
        {
            if(value != Instance.mSimulateNotificationPayload)
            {
                Instance.mSimulateNotificationPayload = value;
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
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mSimulateTeakRewardId)
            {
                Instance.mSimulateTeakRewardId = valueTrim;
                DirtyEditor();
            }
        }
#endif
    }

    public static TeakNotification.Reward.RewardStatus SimulatedTeakRewardStatus
    {
        get { return Instance.mSimulateTeakRewardStatus; }
#if UNITY_EDITOR
        set
        {
            if(value != Instance.mSimulateTeakRewardStatus)
            {
                Instance.mSimulateTeakRewardStatus = value;
                DirtyEditor();
            }
        }
#endif
    }

    public static string SimulatedTeakRewardJson
    {
        get { return Instance.mSimulatedTeakRewardJson; }
#if UNITY_EDITOR
        set
        {
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mSimulatedTeakRewardJson)
            {
                Instance.mSimulatedTeakRewardJson = valueTrim;
                DirtyEditor();
            }
        }
#endif
    }

    public static string SimulatedNotificationPayloadJson
    {
        get { return Instance.mSimulatedNotificationPayloadJson; }
#if UNITY_EDITOR
        set
        {
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mSimulatedNotificationPayloadJson)
            {
                Instance.mSimulatedNotificationPayloadJson = valueTrim;
                DirtyEditor();
            }
        }
#endif
    }

    public static RewardEntry[] RewardEntries
    {
        get { return Instance.mRewardEntries; }
    }

    public static StringStringPair[] NotificationPayloadEntries
    {
        get { return Instance.mNotificationPayloadEntries; }
    }

    public static StringStringPair[] DeepLinkParams
    {
        get { return Instance.mDeepLinkParams; }
#if UNITY_EDITOR
        set
        {
            Instance.mDeepLinkParams = value;
            DirtyEditor();
        }
#endif
    }

    public static string SimulatedDeepLink
    {
        get { return Instance.mSimulatedDeepLink; }
#if UNITY_EDITOR
        set
        {
            string valueTrim = value.Trim();
            if(valueTrim != Instance.mSimulatedDeepLink)
            {
                Instance.mSimulatedDeepLink = valueTrim;
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

    [Serializable]
    public struct RewardEntry {
        public string key;
        public int count;
    }
    public RewardEntry[] mRewardEntries;

    [Serializable]
    public struct StringStringPair {
        public string Key;
        public string Value;
    }
    public StringStringPair[] mDeepLinkParams;
    public StringStringPair[] mNotificationPayloadEntries;

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
    private bool mSimulateRewardReply = false;
    [SerializeField]
    private bool mSimulateNotificationPayload = false;
    [SerializeField]
    private string mSimulateTeakRewardId = "";
    [SerializeField]
    private TeakNotification.Reward.RewardStatus mSimulateTeakRewardStatus = TeakNotification.Reward.RewardStatus.GrantReward;
    [SerializeField]
    private string mSimulatedTeakRewardJson = "";
    [SerializeField]
    private string mSimulatedDeepLink = "";
    [SerializeField]
    private string mSimulatedNotificationPayloadJson = "";

#if UNITY_EDITOR
    [SerializeField]
    private string mSimulateDeepLinkEditorKey = "";
#endif

    private static TeakSettings mInstance;
}
#endif
