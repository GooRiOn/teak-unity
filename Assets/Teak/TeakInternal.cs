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
using UnityEngine;

using System;
using System.Net;
using System.Text;
using System.Reflection;

#if UNITY_EDITOR
using TeakEditor.MiniJSON;
using TeakEditor.Amazon.Util;

using System.Net.Security;
using System.Collections.Generic;
using System.Security.Cryptography;
#endif
#endregion

public partial class Teak : MonoBehaviour
{
    /// @cond hide_from_doxygen
    private static Teak mInstance;

    Type _socketType;
    Type _addressFamilyType;
    Type _socketTypeType;
    Type _protocolTypeType;
    Type _socketOptionLevelType;
    Type _socketOptionNameType;
    MethodInfo _socket_SetSocketOption;
    MethodInfo _socket_SendTo;
    MethodInfo _socket_Close;

    void SetupForRemoteQA()
    {
        _socketType = Type.GetType("System.Net.Sockets.Socket, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        _addressFamilyType = Type.GetType("System.Net.Sockets.AddressFamily, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        _socketTypeType = Type.GetType("System.Net.Sockets.SocketType, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        _protocolTypeType = Type.GetType("System.Net.Sockets.ProtocolType, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        _socketOptionLevelType = Type.GetType("System.Net.Sockets.SocketOptionLevel, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        _socketOptionNameType = Type.GetType("System.Net.Sockets.SocketOptionName, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

        _socket_SetSocketOption = _socketType.GetMethod("SetSocketOption", new Type [] { _socketOptionLevelType, _socketOptionNameType, typeof(int) });
        _socket_SendTo = _socketType.GetMethod("SendTo", new Type [] { typeof(byte[]), typeof(EndPoint) });
        _socket_Close = _socketType.GetMethod("Close", new Type [] {});
    }

#if UNITY_EDITOR
    public static string signParams(string hostname, string endpoint, string secret, Dictionary<string, object> urlParams)
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
#endif
    /// @endcond
}
