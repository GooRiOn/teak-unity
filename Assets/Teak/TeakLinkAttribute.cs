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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#endregion

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TeakLinkAttribute : System.Attribute
{
    public TeakLinkAttribute(string url)
    {
        this.Url = url;
    }

    /// @cond hide_from_doxygen
    readonly string Url;

    Dictionary<string, ParameterInfo> MethodParams
    {
        get;
        set;
    }

    Regex Regex
    {
        get;
        set;
    }

    MethodInfo MethodInfo
    {
        get;
        set;
    }

    Type DeclaringObjectType
    {
        get;
        set;
    }

    static Dictionary<Regex, TeakLinkAttribute> Links
    {
        get;
        set;
    }

    public static bool ProcessUrl(string url)
    {
        Debug.Log("Trying to match: " + url);
        foreach(KeyValuePair<Regex, TeakLinkAttribute> entry in TeakLinkAttribute.Links)
        {
            Debug.Log(entry.Key);
            Match matchResult = entry.Key.Match(url);
            if(matchResult.Success)
            {
                object target = null;

                // TODO: If there is a specific Unity Scene that this needs to be executed on
                //       change to that scene before proceeding.

                // If this has a DeclaringObjectType then search the Unity scene for it.
                if(entry.Value.DeclaringObjectType != null)
                {
                    UnityEngine.Object[] objects = UnityEngine.Object.FindObjectsOfType(entry.Value.DeclaringObjectType);
                    if(objects.Length == 0)
                    {
                        string err = String.Format("0 objects of type {0} found when trying to resolve TeakLink '{1}'.", entry.Value.DeclaringObjectType, url);
                        Debug.LogError(err);
                    }
                    else
                    {
                        target = objects[0];
                        if(objects.Length > 1)
                        {
                            string err = String.Format("{0} possible objects of type {1} found when trying to resolve TeakLink '{2}'. Using the first object found.", objects.Length, entry.Value.DeclaringObjectType, url);
                            Debug.LogWarning(err);
                        }
                    }
                }

                if(entry.Value.MethodInfo.IsStatic || target != null)
                {
                    object[] invokeParams = new object[entry.Value.MethodParams.Count];
                    foreach(KeyValuePair<string, ParameterInfo> param in entry.Value.MethodParams)
                    {
                        invokeParams[param.Value.Position] = matchResult.Groups[param.Key].Value;
                    }
                    entry.Value.MethodInfo.Invoke(target, invokeParams);

                    // Stop looking for matches
                    return true;
                }
            }
        }

        return false;
    }

    public static void ProcessAnnotatedMethods()
    {
        TeakLinkAttribute.Links = new Dictionary<Regex, TeakLinkAttribute>();

        Assembly a = typeof(TeakLinkAttribute).Assembly;
        //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //foreach (var a in assemblies)
        //{
            Debug.Log(a.ToString());
            MethodInfo[] methods = a.GetTypes()
                      .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                      .Where(m => m.GetCustomAttributes(typeof(TeakLinkAttribute), false).Length > 0)
                      .ToArray();
            foreach(MethodInfo entry in methods)
            {
                ValidateAnnotations(entry);
            }
        //}
    }

    private static void ValidateAnnotations(MethodInfo method)
    {
        Dictionary<string, ParameterInfo> methodParams = method.GetParameters()
            .ToDictionary(m => m.Name);

        TeakLinkAttribute[] teakLinks = method.GetCustomAttributes(typeof(TeakLinkAttribute), false) as TeakLinkAttribute[];
        foreach(TeakLinkAttribute link in teakLinks)
        {
            // https://github.com/rkh/mustermann/blob/master/mustermann-simple/lib/mustermann/simple.rb
            Regex escape = new Regex(@"[^\?\%\\\/\:\*\w]");
            string pattern = escape.Replace(link.Url, (Match m) => {
                return Regex.Escape(m.Value);
            });

            Regex compile = new Regex(@"((:\w+)|\*)");
            pattern = compile.Replace(pattern, (Match m) => {
                if(m.Value == "*")
                {
                    // 'splat' behavior could be bad to support from a debugging standpoint
                    throw new NotSupportedException(String.Format("'splat' functionality is not supported by TeakLinks.\nMethod: {0}", DebugStringForMethodInfo(method)));
                    // return "(?<splat>.*?)";
                }
                return String.Format("(?<{0}>[^/?#]+)", m.Value.Substring(1));
            });

            link.Regex = new Regex(pattern);
            link.MethodParams = methodParams;
            link.MethodInfo = method;

            // Check for special case where method has only one parameter named 'params' of type Dictionary<string, object>
            if(link.MethodParams.ContainsKey("params"))
            {
                if(link.MethodParams["params"].ParameterType != typeof(Dictionary<string, object>))
                {
                    throw new ArgumentException(String.Format("Parameter passing by 'params' must use type Dictionary<string, object>.\nMethod: {0}", DebugStringForMethodInfo(method)));
                }
            }
            else
            {
                foreach(string groupName in link.Regex.GetGroupNames())
                {
                    if(groupName == "0") continue;
                    if(!link.MethodParams.ContainsKey(groupName))
                    {
                        throw new ArgumentException(String.Format("TeakLink missing parameter name '{0}'\nMethod: {1}", groupName, DebugStringForMethodInfo(method)));
                    }
                }
            }

            if(method.IsStatic)
            {
                // Nothing for now
            }
            else if(method.DeclaringType.BaseType == typeof(MonoBehaviour))
            {
                link.DeclaringObjectType = method.DeclaringType;
            }
            else
            {
                Debug.LogError(method.DeclaringType + " is a " + method.DeclaringType.BaseType);
                throw new NotSupportedException(String.Format("Method must be declared 'static' if it is not on a MonoBehaviour.\nMethod: {0}", DebugStringForMethodInfo(method)));
            }

            TeakLinkAttribute.Links.Add(link.Regex, link);
        }
    }

    private static string DebugStringForMethodInfo(MethodInfo method)
    {
        return String.Format("{0}:{1}", method.DeclaringType, method.Name);
    }
    /// @endcond
}
