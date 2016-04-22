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
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Collections.Generic;

using TeakEditor.MiniJSON;
#endregion

// Disable warning for the Teak.signParams partial class
#pragma warning disable 0436

[CustomEditor(typeof(TeakSettings))]
public class TeakSettingsEditor : Editor
{
    private bool mAndroidFoldout;

    public override void OnInspectorGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        TeakSettings.AppId = EditorGUILayout.TextField("Teak App Id", TeakSettings.AppId);
        TeakSettings.APIKey = EditorGUILayout.TextField("Teak API Key", TeakSettings.APIKey);

        if(TeakSettings.AppValid)
        {
            EditorGUILayout.HelpBox(TeakSettings.AppStatus, MessageType.Info);
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
            GUIContent content = new GUIContent("GCM Sender Id [?]",  "Put in your GCM Sender Id to have Teak auto-register for GCM notifications.");
            TeakSettings.GCMSenderId = EditorGUILayout.TextField(content, TeakSettings.GCMSenderId);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Development Tools", EditorStyles.boldLabel);
        TeakSettings.SimulateOpenedWithPush = EditorGUILayout.ToggleLeft("Simulate Opening App via Push Notification", TeakSettings.SimulateOpenedWithPush, GUILayout.ExpandWidth(true));
        if(TeakSettings.SimulateOpenedWithPush)
        {
            TeakSettings.SimulatedTeakRewardId = EditorGUILayout.TextField("teakRewardId", TeakSettings.SimulatedTeakRewardId);
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
                TeakSettings.AppStatus = "Settings valid for: " + reply["name"] as string;
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
