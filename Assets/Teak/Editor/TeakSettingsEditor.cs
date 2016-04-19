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
using System.Net.Security;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Teak.MiniJSON;
using Teak.Amazon.Util;
#endregion

[CustomEditor(typeof(TeakSettings))]
public class TeakSettingsEditor : Editor
{
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
        string sig = signParams(hostname, endpoint, TeakSettings.APIKey, urlParams);

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

    private static string signParams(string hostname, string endpoint, string secret, Dictionary<string, object> urlParams)
    {
        // Build sorted list of key-value pairs
        string[] keys = new string[urlParams.Keys.Count];
        urlParams.Keys.CopyTo(keys, 0);
        Array.Sort(keys);
        List<string> kvList = new List<string>();
        foreach(string key in keys)
        {
            string asStr;
            if((asStr = urlParams[key] as string) != null)
            {
                kvList.Add(String.Format("{0}={1}", key, asStr));
            }
            else
            {
                kvList.Add(String.Format("{0}={1}", key,
                    Json.Serialize(urlParams[key])));
            }
        }
        string payload = String.Join("&", kvList.ToArray());
        string signString = String.Format("{0}\n{1}\n{2}\n{3}", "POST", hostname.Split(new char[]{':'})[0], endpoint, payload);
        string sig = AWSSDKUtils.HMACSign(signString, secret, KeyedHashAlgorithm.Create("HMACSHA256"));
        return sig;
    }
}
