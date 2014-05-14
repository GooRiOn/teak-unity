/* Teak -- Copyright (C) 2014 GoCarrot Inc.
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

using System;
using GoCarrotInc.MiniJSON;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Net.Security;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

public class TeakSettings : EditorWindow
{
    public static string TeakAppId
    {
        get
        {
            LoadSettings();
            return mTeakAppId;
        }
        private set
        {
            string appId = value.Trim();
            if(appId != mTeakAppId)
            {
                mAppValid = false;
                mAppStatus = "";
                mTeakAppId = appId;
                SaveSettings();
            }
        }
    }

    public static string TeakAppSecret
    {
        get
        {
            LoadSettings();
            return mTeakAppSecret;
        }
        private set
        {
            string appSecret = value.Trim();
            if(appSecret != mTeakAppSecret)
            {
                mAppValid = false;
                mAppStatus = "";
                mTeakAppSecret = appSecret;
                SaveSettings();
            }
        }
    }

    public static bool AppValid
    {
        get
        {
            LoadSettings();
            return mAppValid;
        }
        private set
        {
            if(value != mAppValid)
            {
                mAppValid = value;
                SaveSettings();
            }
        }
    }

    public static string AppStatus
    {
        get
        {
            LoadSettings();
            return mAppStatus;
        }
        private set
        {
            string appStatus = value.Trim();
            if(appStatus != mAppStatus)
            {
                mAppStatus = appStatus;
                SaveSettings();
            }
        }
    }

    [MenuItem("Edit/Teak")]
    public static void ShowWindow()
    {
        LoadSettings();
        TeakSettings settingsWindow = (TeakSettings)GetWindow<TeakSettings>(false, "Teak Settings", false);
        settingsWindow.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        TeakAppId = EditorGUILayout.TextField("Teak App Id", mTeakAppId);
        TeakAppSecret = EditorGUILayout.TextField("Teak App Secret", mTeakAppSecret);

        if(AppValid)
        {
            EditorGUILayout.HelpBox(mAppStatus, MessageType.Info);
        }
        else if(!string.IsNullOrEmpty(mAppStatus))
        {
            EditorGUILayout.HelpBox(mAppStatus, MessageType.Error);
        }

        if(!AppValid)
        {
            if(GUILayout.Button("Validate Settings", GUILayout.Height(25)))
            {
                ValidateSettings();
            }
        }

        if(GUILayout.Button("Get a Teak Account", GUILayout.Height(25)))
        {
            Application.OpenURL("https://app.teak.io/developers/sign_up?referrer=unity");
        }
    }

    static void LoadSettings()
    {
        if(!mSettingsLoaded)
        {
            TextAsset teakJson = Resources.Load("teak") as TextAsset;
            if(teakJson != null)
            {
                Dictionary<string, object> teakConfig = null;
                teakConfig = Json.Deserialize(teakJson.text) as Dictionary<string, object>;
                mTeakAppId = teakConfig["teakAppId"] as string;
                mTeakAppSecret = teakConfig["teakAppSecret"] as string;
                mAppStatus = teakConfig.ContainsKey("appStatus") ? teakConfig["appStatus"] as string : "";
                mAppValid = teakConfig.ContainsKey("appValid") ? (bool)teakConfig["appValid"] : false;
            }
            mSettingsLoaded = true;
        }
    }

    static void SaveSettings()
    {
        Dictionary<string, object> teakConfig = new Dictionary<string, object>();
        teakConfig["teakAppId"] = mTeakAppId;
        teakConfig["teakAppSecret"] = mTeakAppSecret;
        teakConfig["appBundleVersion"] = PlayerSettings.bundleVersion.ToString();
        teakConfig["appValid"] = mAppValid;
        teakConfig["appStatus"] = mAppStatus;

        System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
        File.WriteAllText(Application.dataPath + "/Resources/teak.bytes", Json.Serialize(teakConfig));
        AssetDatabase.Refresh();
    }

    static void ValidateSettings()
    {
        LoadSettings();
        string hostname = "gocarrot.com";
        string endpoint = String.Format("/games/{0}/validate_sig.json", mTeakAppId);
        string versionString = PlayerSettings.bundleVersion.ToString();
        Dictionary<string, object> urlParams  = new Dictionary<string, object> {
            {"app_version", versionString},
            {"id", mTeakAppId}
        };
        string sig = Teak.signParams(hostname, endpoint, mTeakAppSecret, urlParams);

        // Use System.Net.WebRequest due to crossdomain.xml bug in Unity Editor mode
        ServicePointManager.ServerCertificateValidationCallback = TeakCertValidator;
        string postData = String.Format("app_version={0}&id={1}&sig={2}",
            WWW.EscapeURL(versionString),
            WWW.EscapeURL(mTeakAppId),
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
                AppValid = true;
                AppStatus = "Settings valid for: " + reply["name"] as string;
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
                    AppStatus = "Invalid Teak App Secret";
                }
                break;

                case 404:
                {
                    // No such game id
                    AppStatus = "Invalid Teak App Id";
                }
                break;

                default:
                {
                    // Unknown
                    AppStatus = "Unknown error during validation";
                }
                break;
            }
            AppValid = false;
        }
    }

    private static bool TeakCertValidator(object sender, X509Certificate certificate,
                                            X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // This is not ideal
        return true;
    }

    static string mTeakAppId = "";
    static string mTeakAppSecret = "";
    static bool mSettingsLoaded = false;
    static bool mAppValid = false;
    static string mAppStatus = "";
}
