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
using System.Net;
using System.Text;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using TeakEditor.MiniJSON;
#endregion

[InitializeOnLoad]
[CustomEditor(typeof(TeakSettings))]
public class TeakSettingsEditor : Editor
{
    static TeakSettingsEditor()
    {
        EditorApplication.update += EditorRunOnceOnLoad;
    }
    static void EditorRunOnceOnLoad()
    {
        EditorApplication.update -= EditorRunOnceOnLoad;

        try
        {
            TeakLinkAttribute.ProcessAnnotatedMethods();
        }
        catch(Exception e)
        {
            Debug.LogError(e.ToString());
        }

        OnScriptsReloaded();
        mAndroidFoldout = !String.IsNullOrEmpty(TeakSettings.GCMSenderId);
    }

    static bool mAndroidFoldout;
    static int mSelectedEditorLink = 0;
    static string[] mPopupArrayEditorLinks;

    [DidReloadScripts]
    public static void OnScriptsReloaded()
    {
        // Rebuild string-array
        if(TeakLinkAttribute.EditorLinks != null)
        {
            if(mPopupArrayEditorLinks == null ||
                TeakLinkAttribute.EditorLinks.Keys.Count != mPopupArrayEditorLinks.Length)
            {
                mPopupArrayEditorLinks = new string[TeakLinkAttribute.EditorLinks.Keys.Count];
            }
            TeakLinkAttribute.EditorLinks.Keys.CopyTo(mPopupArrayEditorLinks, 0);

            // Update index
            mSelectedEditorLink = mPopupArrayEditorLinks == null ? 0 : Array.IndexOf(mPopupArrayEditorLinks, TeakSettings.SimulateDeepLinkEditorKey);
            mSelectedEditorLink = (mSelectedEditorLink < 0 ? 0 : mSelectedEditorLink);
        }
    }

    public override void OnInspectorGUI()
    {
        if(TeakLinkAttribute.EditorLinks != null && mPopupArrayEditorLinks == null)
        {
            // This prevents exceptions/crashes when the Teak settings are being inspected
            // and the editor enters play-mode.
            OnScriptsReloaded();
        }

        GUILayout.Label("Settings", EditorStyles.boldLabel);
        TeakSettings.AppId = EditorGUILayout.TextField("Teak App Id", TeakSettings.AppId);
        TeakSettings.APIKey = EditorGUILayout.TextField("Teak API Key", TeakSettings.APIKey);

        if(TeakSettings.AppValid)
        {
            EditorGUILayout.HelpBox("Settings valid for: " + TeakSettings.AppStatus, MessageType.Info);
        }
        else if(!string.IsNullOrEmpty(TeakSettings.AppStatus))
        {
            EditorGUILayout.HelpBox(TeakSettings.AppStatus, MessageType.Error);
        }

        if(!TeakSettings.AppValid)
        {
            if(GUILayout.Button("Validate Settings", GUILayout.Height(25)))
            {
                ValidateSettings();
            }
        }

        EditorGUILayout.Space();
        GUILayout.Label("Additional Settings", EditorStyles.boldLabel);
        mAndroidFoldout = EditorGUILayout.Foldout(mAndroidFoldout, "Android");
        if(mAndroidFoldout)
        {
            GUIContent gcmSenderIdContent = new GUIContent("GCM Sender Id [?]",  "Put in your GCM Sender Id to have Teak auto-register for GCM notifications.");
            TeakSettings.GCMSenderId = EditorGUILayout.TextField(gcmSenderIdContent, TeakSettings.GCMSenderId);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Deep Linking Tools", EditorStyles.boldLabel);

        GUIContent simulateDeepLinkContent = new GUIContent("Simulate Deep Link [?]",  "When running the game in the Unity Editor, Teak will simulate that the app has been opened by deep linking.");
        TeakSettings.SimulateDeepLink = EditorGUILayout.ToggleLeft(simulateDeepLinkContent, TeakSettings.SimulateDeepLink, GUILayout.ExpandWidth(true));
        if(TeakSettings.SimulateDeepLink)
        {
            if(TeakLinkAttribute.EditorLinks == null)
            {
                EditorGUILayout.HelpBox("Error in TeakLinks, see the Console tab for more details.", MessageType.Error);
            }
            else
            {
                mSelectedEditorLink = EditorGUILayout.Popup(mSelectedEditorLink, mPopupArrayEditorLinks);
                TeakSettings.SimulateDeepLinkEditorKey = mPopupArrayEditorLinks[mSelectedEditorLink];
                TeakLinkAttribute selected = TeakLinkAttribute.EditorLinks[TeakSettings.SimulateDeepLinkEditorKey];
                int shouldBeLength = (selected.Regex.GetGroupNames().Length - 1);
                if(TeakSettings.DeepLinkParams == null || TeakSettings.DeepLinkParams.Length != shouldBeLength)
                {
                    TeakSettings.DeepLinkParams = new TeakSettings.StringStringPair[shouldBeLength];
                }

                int j = 0;
                foreach(string groupName in selected.Regex.GetGroupNames())
                {
                    if(groupName == "0") continue;
                    TeakSettings.DeepLinkParams[j].Key = groupName;
                    TeakSettings.DeepLinkParams[j].Value = EditorGUILayout.TextField(groupName, TeakSettings.DeepLinkParams[j].Value);
                    j++;
                }

                TeakSettings.SimulatedDeepLink = selected.Url;
                j = 0;
                foreach(string groupName in selected.Regex.GetGroupNames())
                {
                    if(groupName == "0") continue;
                    TeakSettings.SimulatedDeepLink = TeakSettings.SimulatedDeepLink.Replace(String.Format(":{0}", groupName), TeakSettings.DeepLinkParams[j].Value);
                    j++;
                }
                EditorGUILayout.HelpBox(String.Format("Deep Link Preview:\n{0}", TeakSettings.SimulatedDeepLink), MessageType.Info);
            }
            EditorGUILayout.Space();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Notification Tools", EditorStyles.boldLabel);

        GUIContent simulateOpenedWithNotificationContent = new GUIContent("Simulate Opening App via Notification [?]",  "When running the game in the Unity Editor, Teak will simulate that the app has been opened by a notification.");
        TeakSettings.SimulateOpenedWithNotification = EditorGUILayout.ToggleLeft(simulateOpenedWithNotificationContent, TeakSettings.SimulateOpenedWithNotification, GUILayout.ExpandWidth(true));
        if(TeakSettings.SimulateOpenedWithNotification)
        {
            GUIContent simulateRewardContent = new GUIContent("Simulate Teak Reward [?]",  "Simulate the Teak reward instead of querying the Teak service.");
            TeakSettings.SimulateRewardReply = EditorGUILayout.ToggleLeft(simulateRewardContent, TeakSettings.SimulateRewardReply, GUILayout.ExpandWidth(true));
            if(TeakSettings.SimulateRewardReply)
            {
                GUIContent simulateRewardStatusContent = new GUIContent("Reward Status [?]",  "The Teak Reward Status that will be simulated.");
                TeakSettings.SimulatedTeakRewardStatus = (TeakNotification.Reward.RewardStatus)EditorGUILayout.EnumPopup(simulateRewardStatusContent, TeakSettings.SimulatedTeakRewardStatus);

                if(TeakSettings.SimulatedTeakRewardStatus == TeakNotification.Reward.RewardStatus.GrantReward)
                {
                    GUIContent simulateRewardJsonContent = new GUIContent("Reward Payload [?]",  "The contents of the Teak Reward JSON that will be simulated.");
                    SerializedProperty rewardEntries = serializedObject.FindProperty("mRewardEntries");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(rewardEntries, simulateRewardJsonContent, true);
                    if(EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        if(TeakSettings.RewardEntries != null)
                        {
                            Dictionary<string, object> json = new Dictionary<string, object>();
                            foreach(TeakSettings.RewardEntry entry in TeakSettings.RewardEntries)
                            {
                                json[entry.key] = entry.count;
                            }
                            TeakSettings.SimulatedTeakRewardJson = Json.Serialize(json);
                        }
                    }
                }
            }
            else
            {
                GUIContent teakRewardIdContent = new GUIContent("teakRewardId [?]",  "teakRewardId parameter of push notification payload.");
                TeakSettings.SimulatedTeakRewardId = EditorGUILayout.TextField(teakRewardIdContent, TeakSettings.SimulatedTeakRewardId);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(" ");
                EditorGUILayout.HelpBox("Leave this field blank for no reward", MessageType.None);
                EditorGUILayout.EndHorizontal();
                if(TeakSettings.AppValid)
                {
                    string bundleConfigUrl = "https://app.teak.io/apps#/dashboard/" + TeakSettings.AppId + "/bundles?tab=Bundles";
                    if(GUILayout.Button("Reward Configuration for " + TeakSettings.AppStatus, GUILayout.Height(25)))
                    {
                        Application.OpenURL(bundleConfigUrl);
                    }
                    EditorGUILayout.HelpBox("This will open up the Teak Reward Bundle Configuration in your browser.", MessageType.Info);
                }
            }
        }
    }

    void ValidateSettings()
    {
        string hostname = "gocarrot.com";
        string endpoint = String.Format("/games/{0}/validate_sig.json", TeakSettings.AppId);
        string versionString = PlayerSettings.bundleVersion.ToString();
        Dictionary<string, object> urlParams  = new Dictionary<string, object> {
            {"app_version", versionString},
            {"id", TeakSettings.AppId}
        };

        string sig = Teak.signParams(hostname, endpoint, TeakSettings.APIKey, urlParams);

        // Use System.Net.WebRequest due to crossdomain.xml bug in Unity Editor mode
        string postData = String.Format("app_version={0}&id={1}&sig={2}",
            WWW.EscapeURL(versionString),
            WWW.EscapeURL(TeakSettings.AppId),
            WWW.EscapeURL(sig));
        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        WebRequest request = WebRequest.Create(String.Format("https://{0}{1}", hostname, endpoint));
        request.Method = "POST";
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = byteArray.Length;

        Stream dataStream = request.GetRequestStream();
        dataStream.Write(byteArray, 0, byteArray.Length);
        dataStream.Close();

        try
        {
            using(WebResponse response = request.GetResponse())
            {
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();

                Dictionary<string, object> reply = null;
                reply = Json.Deserialize(responseFromServer) as Dictionary<string, object>;
                TeakSettings.AppValid = true;
                TeakSettings.AppStatus = reply["name"] as string;
            }
        }
        catch(WebException e)
        {
            HttpWebResponse response = (HttpWebResponse)e.Response;
            switch((int)response.StatusCode)
            {
                case 403:
                {
                    // Invalid signature
                    TeakSettings.AppStatus = "Invalid Teak API Key";
                }
                break;

                case 404:
                {
                    // No such game id
                    TeakSettings.AppStatus = "Invalid Teak App Id";
                }
                break;

                default:
                {
                    // Unknown
                    TeakSettings.AppStatus = "Unknown error during validation";
                }
                break;
            }
            TeakSettings.AppValid = false;
        }
    }
}
