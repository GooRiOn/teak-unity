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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
#endregion

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TeakLinkAttribute : System.Attribute
{
    // TODO: __FILE__ and __LINE__ can work maybe?
    public TeakLinkAttribute(string url)
    {
        this.Url = url;
    }

    /// @cond hide_from_doxygen
    public readonly string Url;

#if UNITY_EDITOR
    public static Dictionary<string, TeakLinkAttribute> EditorLinks
    {
        get;
        private set;
    }
#endif

    Dictionary<string, ParameterInfo> MethodParams
    {
        get;
        set;
    }

#if UNITY_EDITOR
    public Regex Regex
    {
        get;
        private set;
    }
#else
    Regex Regex
    {
        get;
        set;
    }
#endif

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

    public static void LoadDeepLinks(bool noValidate = true)
    {
        List<MethodInfo> methods = null;
        byte[] bytes = Convert.FromBase64String(TeakSettings.TeakLinkMethods);
        using(var ms = new MemoryStream(bytes, 0, bytes.Length))
        {
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;
            methods = new BinaryFormatter().Deserialize(ms) as List<MethodInfo>;
        }

        TeakLinkAttribute.Links = new Dictionary<Regex, TeakLinkAttribute>();

        foreach(MethodInfo entry in methods)
        {
            ValidateAnnotations(entry, noValidate);
        }
    }

    public static bool ProcessUri(Uri uri)
    {
        if(uri == null) return false;

        // TODO: Check incoming Uri for a query parameter named 'al_applink_data' and
        //       JSON parse that.

        string uriMatchString = String.Format("/{0}{1}", uri.Authority, uri.AbsolutePath);
        foreach(KeyValuePair<Regex, TeakLinkAttribute> entry in TeakLinkAttribute.Links)
        {
            Match matchResult = entry.Key.Match(uriMatchString);
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
                        string err = String.Format("0 objects of type {0} found when trying to resolve TeakLink '{1}'.", entry.Value.DeclaringObjectType, uriMatchString);
                        Debug.LogError(err);
                    }
                    else
                    {
                        target = objects[0];
                        if(objects.Length > 1)
                        {
                            string err = String.Format("{0} possible objects of type {1} found when trying to resolve TeakLink '{2}'. Using the first object found.", objects.Length, entry.Value.DeclaringObjectType, uriMatchString);
                            Debug.LogWarning(err);
                        }
                    }
                }

                if(entry.Value.MethodInfo.IsStatic || target != null)
                {
                    Dictionary<string, object> paramsDict = new Dictionary<string, object>();
                    paramsDict.Add("_uri", uri);
                    if(entry.Value.MethodParams.ContainsKey("parameters"))
                    {
                        foreach(string groupName in entry.Value.Regex.GetGroupNames())
                        {
                            if(groupName == "0") continue;
                            paramsDict.Add(groupName, matchResult.Groups[groupName].Value);
                        }
                    }

                    object[] invokeParams = new object[entry.Value.MethodParams.Count];
                    foreach(KeyValuePair<string, ParameterInfo> param in entry.Value.MethodParams)
                    {
                        if(param.Key == "parameters")
                        {
                            invokeParams[param.Value.Position] = paramsDict;
                        }
                        else
                        {
                            invokeParams[param.Value.Position] = matchResult.Groups[param.Key].Value;
                        }
                    }
                    entry.Value.MethodInfo.Invoke(target, invokeParams);

                    // Stop looking for matches
                    return true;
                }
            }
        }

        return false;
    }

#if UNITY_EDITOR
    public static void ProcessAnnotatedMethods()
    {
        Assembly a = typeof(TeakLinkAttribute).Assembly;
        //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //foreach (var a in assemblies)
        //{
            List<MethodInfo> methods = a.GetTypes()
                      .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                      .Where(m => m.GetCustomAttributes(typeof(TeakLinkAttribute), false).Length > 0)
                      .ToList();

            using(var ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, methods);
                TeakSettings.TeakLinkMethods = Convert.ToBase64String(ms.ToArray());
            }

            // Use in-game functionality for consistency
            LoadDeepLinks(false);
        //}

        TeakLinkAttribute.EditorLinks = TeakLinkAttribute.Links
            .OrderByDescending(l => l.Value.Url)
            .Select(l => l.Value)
            .ToDictionary(l => l.Url.Replace("/", "\u2215")); // For Unity Editor
    }
#endif

    private static void ValidateAnnotations(MethodInfo method, bool noValidate = false)
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


            List<string> dupeCheck = new List<string>();
            Regex compile = new Regex(@"((:\w+)|\*)");
            pattern = compile.Replace(pattern, (Match m) => {
                if(m.Value == "*")
                {
                    // 'splat' behavior could be bad to support from a debugging standpoint
                    throw new NotSupportedException(String.Format("'splat' functionality is not supported by TeakLinks.\nMethod: {0}", DebugStringForMethodInfo(method)));
                    // return "(?<splat>.*?)";
                }
                dupeCheck.Add(m.Value.Substring(1));
                return String.Format("(?<{0}>[^/?#]+)", m.Value.Substring(1));
            });

            if(!noValidate)
            {
                // Check for duplicate capture group names
                List<string> duplicates = dupeCheck
                    .GroupBy(i => i)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                if(duplicates.Count() > 0)
                {
                    throw new ArgumentException(String.Format("Duplicate variable name '{0}'.\nMethod: {1}", duplicates[0], DebugStringForMethodInfo(method)));
                }
            }

            link.Regex = new Regex(pattern);
            link.MethodParams = methodParams;
            link.MethodInfo = method;

            if(!noValidate)
            {
                // Check for special case where method has only one parameter named 'params' of type Dictionary<string, object>
                if(link.MethodParams.ContainsKey("parameters"))
                {
                    if(link.MethodParams["parameters"].ParameterType != typeof(Dictionary<string, object>))
                    {
                        throw new ArgumentException(String.Format("Parameter passing by 'parameters' must use type Dictionary<string, object>.\nMethod: {0}", DebugStringForMethodInfo(method)));
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
            }

            // Check for either static method or method on a MonoBehaviour
            if(method.IsStatic)
            {
                // Nothing for now
            }
            else if(method.DeclaringType.BaseType == typeof(MonoBehaviour)) // TODO: Walk back base types?
            {
                link.DeclaringObjectType = method.DeclaringType;
            }
            else
            {
                Debug.LogError(method.DeclaringType + " is a " + method.DeclaringType.BaseType);
                throw new NotSupportedException(String.Format("Method must be declared 'static' if it is not on a MonoBehaviour.\nMethod: {0}", DebugStringForMethodInfo(method)));
            }

            if(!noValidate)
            {
                // Check for duplicate routes
                string dupeRouteCheck = link.Url;
                foreach(string groupName in link.Regex.GetGroupNames())
                {
                    if(groupName == "0") continue;
                    dupeRouteCheck = dupeRouteCheck.Replace(String.Format(":{0}", groupName), "");
                }

                foreach(KeyValuePair<Regex, TeakLinkAttribute> entry in TeakLinkAttribute.Links)
                {
                    string emptyVarRoute = entry.Value.Url;
                    foreach(string groupName in entry.Key.GetGroupNames())
                    {
                        if(groupName == "0") continue;
                        emptyVarRoute = emptyVarRoute.Replace(String.Format(":{0}", groupName), "");
                    }

                    if(emptyVarRoute == dupeRouteCheck)
                    {
                        throw new ArgumentException(String.Format("TeakLink route for method {0}({1}) conflicts with route for method: {2}({3})",
                            DebugStringForMethodInfo(link.MethodInfo), link.Url,
                            DebugStringForMethodInfo(entry.Value.MethodInfo), entry.Value.Url));
                    }
                }
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
