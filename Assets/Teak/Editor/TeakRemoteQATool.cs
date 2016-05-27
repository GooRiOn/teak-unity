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
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using TeakEditor.MiniJSON;
#endregion

[InitializeOnLoad]
public class TeakRemoteQATool : EditorWindow
{
    static TeakRemoteQATool()
    {
        EditorApplication.update += EditorRunOnceOnLoad;
    }

    static void EditorRunOnceOnLoad()
    {
        EditorApplication.update -= EditorRunOnceOnLoad;
    }

    [MenuItem ("Window/Teak QA Tool")]
    static void Init()
    {
        TeakRemoteQATool window = EditorWindow.GetWindow(typeof(TeakRemoteQATool)) as TeakRemoteQATool;
        window.Show();
    }

    class TeakGame
    {
        public string Name
        {
            get;
            private set;
        }

        public string OsInfo
        {
            get;
            private set;
        }

        public EndPoint EndPoint
        {
            get;
            private set;
        }

        public Dictionary<string, object> Created
        {
            get;
            private set;
        }

        public bool SettingsValid
        {
            get;
            private set;
        }

        public string SettingsErrorOrName
        {
            get;
            private set;
        }

        public TeakGame(Dictionary<string, object> identifyPacket, EndPoint ep)
        {
            this.Name = identifyPacket["id"] as string;
            this.EndPoint = ep;

            if(identifyPacket["android"] != null)
            {
                Dictionary<string, object> android = identifyPacket["android"] as Dictionary<string, object>;
                this.OsInfo = string.Format("Android {0} (API level {1})", android["version"], android["api_level"]);
            }
        }

        public void Update(Dictionary<string, object> packet)
        {
            Debug.Log(Json.Serialize(packet));

            string packetType = packet["type"] as string;
            string eventName = packet["name"] as string;
            Dictionary<string, object> extras = packet["extras"] as Dictionary<string, object>;
            if(packetType == "lifecycle")
            {
                if(eventName == "created")
                {
                    this.Created = extras;
                }
            }
            else if(packetType == "settings")
            {
                if(eventName == "valid")
                {
                    this.SettingsValid = true;
                    this.SettingsErrorOrName = extras["name"] as string;
                }
                else
                {

                    this.SettingsValid = false;
                    this.SettingsErrorOrName = extras["error"] as string;
                }
            }
        }
    }
    Dictionary<string, TeakGame> _games = new Dictionary<string, TeakGame>();

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        foreach(TeakGame game in _games.Values)
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(game.Name, EditorStyles.largeLabel);
            if(game.OsInfo != null)
            {
                GUILayout.Label(game.OsInfo, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            if(game.Created != null)
            {
                EditorGUILayout.HelpBox(string.Format("Teak initialized\n"),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Waiting for Teak initilization...",  MessageType.None);
            }

            if(game.SettingsValid)
            {
                EditorGUILayout.HelpBox("Teak settings valid for: " + game.SettingsErrorOrName, MessageType.Info);
            }
            else
            {
                if(string.IsNullOrEmpty(game.SettingsErrorOrName))
                {
                    EditorGUILayout.HelpBox("Waiting for Teak settings...", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("Teak settings invalid:\n" + game.SettingsErrorOrName, MessageType.Error);
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    Socket _socket;
    const int TIMEOUT_MILLISECONDS = 50;
    const int MAX_BUFFER_SIZE = 2048;

    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();  
    }

    void OnEnable()
    {
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);
        EndPoint ep = ipep as EndPoint;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(ipep);

        Debug.Log("Listening for Teak QA client...");

        StateObject state = new StateObject();
        state.workSocket = _socket;
        _socket.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, 0, ref ep, new AsyncCallback(ReceiveCallback), state);
    }

    void OnDisable()
    {
        Debug.Log("Closing Teak QA Socket.");
        _socket.Close();
    }

    void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint LocalIPEndPoint = new IPEndPoint(IPAddress.Any, 9050);
        EndPoint LocalEndPoint = (EndPoint)LocalIPEndPoint;
        StateObject state = (StateObject)ar.AsyncState;
        Socket client = state.workSocket;
        int bytesRead = client.EndReceiveFrom(ar, ref LocalEndPoint);
        string packetString = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);

        Dictionary<string, object> packet = Json.Deserialize(packetString) as Dictionary<string, object>;
        string packetId = packet["id"] as string;
        if(packet["type"].Equals("identify"))
        {
            if(!_games.ContainsKey(packetId))
            {
                _games[packet["id"] as string] = new TeakGame(packet, null /* TODO: Need where this packet came from */ );
            }
        }
        else if(_games.ContainsKey(packetId))
        {
            _games[packetId].Update(packet);
        }


        client.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, 0, ref LocalEndPoint, new AsyncCallback(ReceiveCallback), state);
    }
}
