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
using System.Xml;
using System.Xml.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#endregion

public class TeakPostProcessScene
{
    [PostProcessScene]
    public static void OnPostprocessScene()
    {
        if(!mRanThisBuild)
        {
            mRanThisBuild = true;

            if(string.IsNullOrEmpty(TeakSettings.AppId))
            {
                Debug.LogError("Teak App Id needs to be assigned in the Edit/Teak menu.");
            }

            if(string.IsNullOrEmpty(TeakSettings.APIKey))
            {
                Debug.LogError("Teak API Key needs to be assigned in the Edit/Teak menu.");
            }

            if(!TeakSettings.AppValid)
            {
                Debug.LogWarning("Your Teak settings have not been validated. Click 'Validate Settings' in the Edit/Teak menu.");
            }

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Plugins/Android/res/values"));
            XDocument doc = new XDocument(
                new XElement("resources", 
                    new XElement("string", TeakSettings.AppId, new XAttribute("name", "io_teak_app_id")),
                    new XElement("string", TeakSettings.APIKey, new XAttribute("name", "io_teak_api_key")),
                    String.IsNullOrEmpty(TeakSettings.GCMSenderId) ? null : new XElement("string", TeakSettings.GCMSenderId, new XAttribute("name", "io_teak_gcm_sender_id"))
                )
            );
            doc.Save(Path.Combine(Application.dataPath, "Plugins/Android/res/values/teak.xml"));
        }
    }

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuildProject)
    {
        mRanThisBuild = false;
    }

    private static bool mRanThisBuild = false;
}
