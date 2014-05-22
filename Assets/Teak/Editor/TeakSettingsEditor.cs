using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Net.Security;
using GoCarrotInc.MiniJSON;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

[CustomEditor(typeof(TeakSettings))]
public class TeakSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        TeakSettings.AppId = EditorGUILayout.TextField("Teak App Id", TeakSettings.AppId);
        TeakSettings.AppSecret = EditorGUILayout.TextField("Teak App Secret", TeakSettings.AppSecret);

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

        if(GUILayout.Button("Get a Teak Account", GUILayout.Height(25)))
        {
            Application.OpenURL("https://app.teak.io/developers/sign_up?referrer=unity");
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
        string sig = Teak.signParams(hostname, endpoint, TeakSettings.AppSecret, urlParams);

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
                    TeakSettings.AppStatus = "Invalid Teak App Secret";
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
