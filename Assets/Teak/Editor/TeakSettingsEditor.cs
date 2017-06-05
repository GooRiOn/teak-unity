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

        mAndroidFoldout = !String.IsNullOrEmpty(TeakSettings.GCMSenderId);
    }

    static bool mAndroidFoldout;

    public override void OnInspectorGUI()
    {

        GUILayout.Label("Settings", EditorStyles.boldLabel);
        TeakSettings.AppId = EditorGUILayout.TextField("Teak App Id", TeakSettings.AppId);
        TeakSettings.APIKey = EditorGUILayout.TextField("Teak API Key", TeakSettings.APIKey);

        EditorGUILayout.Space();
        GUILayout.Label("Additional Settings", EditorStyles.boldLabel);
        mAndroidFoldout = EditorGUILayout.Foldout(mAndroidFoldout, "Android");
        if(mAndroidFoldout)
        {
            GUIContent gcmSenderIdContent = new GUIContent("GCM Sender Id [?]",  "Put in your GCM Sender Id to have Teak auto-register for GCM notifications.");
            TeakSettings.GCMSenderId = EditorGUILayout.TextField(gcmSenderIdContent, TeakSettings.GCMSenderId);
        }
    }
}
