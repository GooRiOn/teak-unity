#region License
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
#endregion

#region References
using System;
using System.Net;
using UnityEngine;
using System.Text;
using System.Security;
using System.Reflection;
using System.Collections;
using System.Net.Security;
using GoCarrotInc.MiniJSON;
using GoCarrotInc.Amazon.Util;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
#endregion

public partial class Teak
{
    /// @cond hide_from_doxygen

    #region Internal
    private enum FacebookSDKType : int
    {
        None = -1,
        OfficialUnitySDK = 0,
        JavaScriptSDK = 1
    }

    protected void getDeepLinkResult(object fbResult)
    {
        mLaunchURL = mFBResultPropertyText.GetValue(fbResult, null) as string;
    }

    Teak()
    {
        mCache = Cache.Create();
        this.InstallDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(mCache.InstallDate);

        // Check to see if the official Facebook SDK is being used
        mFacebookDelegateType = Type.GetType("Facebook.FacebookDelegate,IFacebook");
        if(mFacebookDelegateType != null)
        {
            Type t = Type.GetType("FB");
            mOfficialFBSDKFeedMethod = t.GetMethod("Feed", BindingFlags.Static | BindingFlags.Public);

            t = Type.GetType("FBResult,IFacebook");
            mFBResultPropertyText = t.GetProperty("Text");
            mFBResultPropertyError = t.GetProperty("Error");
            mFacebookDelegateType = Type.GetType("Facebook.FacebookDelegate,IFacebook");

            if(mFacebookDelegateType != null &&
               mOfficialFBSDKFeedMethod != null &&
               mFBResultPropertyText != null &&
               mFBResultPropertyError != null &&
               mFacebookDelegateType != null)
            {
                mFacebookSDKType = FacebookSDKType.OfficialUnitySDK;

                // Get deep link via FB SDK
                MethodInfo getDeepLink = t.GetMethod("GetDeepLink", BindingFlags.Static | BindingFlags.Public);

                if(getDeepLink != null)
                {
                    MethodInfo mi1 = typeof(Teak).GetMethod("getDeepLinkResult", BindingFlags.NonPublic | BindingFlags.Instance);
                    object fbDelegate = Delegate.CreateDelegate(mFacebookDelegateType, this, mi1);
                    getDeepLink.Invoke(null, new object[] { fbDelegate });
                }
            }
        }
#if UNITY_WEBPLAYER
        // If on Unity WebPlayer, some use the JavaScript SDK
        else
        {
            Application.ExternalEval("if(window.__teakUnityInstance == null || window.__teakUnityInstance == undefined) {" +
                                     "    window.__teakUnityInstance = UnityObject2.instances[0];" +
                                     "}" +
                                     "window.__teakUnityInstance.getUnity().SendMessage('TeakGameObject', 'assignUnityObject2Instance', 'window.__teakUnityInstance');" +
                                     "window.__teakUnityInstance.getUnity().SendMessage('TeakGameObject', 'assignLaunchURL', 'document.URL');" +
            );
        }
#endif
    }

#if UNITY_WEBPLAYER
    private void assignUnityObject2Instance(string message)
    {
        mFacebookSDKType = FacebookSDKType.JavaScriptSDK;
        mUnityObject2Instance = message;
    }

    private void assignLaunchURL(string message)
    {
        mLaunchURL = message;
    }
#endif

    private RequestResponse cachedRequestHandler(CachedRequest cachedRequest, RequestResponse callback)
    {
        return (Response ret, string errorText, Dictionary<string, object> reply) => {
                switch(ret)
                {
                    case Response.OK:
                    case Response.NotFound:
                    case Response.ParameterError:
                        cachedRequest.RemoveFromCache();
                        break;

                    default:
                        cachedRequest.AddRetryInCache();
                        break;
                }
                if(callback != null) callback(ret, errorText, reply);
        };
    }

    protected void unitySDKFeedPostCallback(object fbResult)
    {
        string result = mFBResultPropertyText.GetValue(fbResult, null) as string;

        Dictionary<string, object> reply = null;
        if(!string.IsNullOrEmpty(result))
        {
            reply = Json.Deserialize(result) as Dictionary<string, object>;
        }
        else
        {
            result = mFBResultPropertyError.GetValue(fbResult, null) as string;
            if(!string.IsNullOrEmpty(result))
            {
                reply = Json.Deserialize(result) as Dictionary<string, object>;
            }
        }
        feedPostCallbackHandler(reply);
    }

#if UNITY_WEBPLAYER
    protected void javascriptSDKFeedPostCallback(string message)
    {
        Dictionary<string, object> reply = Json.Deserialize(message) as Dictionary<string, object>;
        feedPostCallbackHandler(reply);
    }
#endif

    protected void feedPostCallbackHandler(Dictionary<string, object> reply)
    {
        string postId = null;
        if(reply != null)
        {
            postId = (reply.ContainsKey("post_id") ? reply["post_id"] as string : (reply.ContainsKey("id") ? reply["id"] as string : null));
        }

        if(postId != null)
        {
            StartCoroutine(cachedRequestCoroutine(ServiceType.Metrics, "/feed_dialog_post.json", new Dictionary<string, object>() {
                {"platform_id", postId}
            }));
        }
    }

    private IEnumerator loadMobileAdvertisingIds()
    {
#if false // TEMPORARY: Remove this functionality
        // Get attribution id so we can generate a custom audience id via graph call
        // to app_id/custom_audience_third_party_id
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
        if(string.IsNullOrEmpty(mCache.MobileAdvertisingId))
        {
#   if UNITY_ANDROID
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            // New Android SDK method
            try
            {
                AndroidJavaClass AttributionIdentifiersClass = new AndroidJavaClass("com.facebook.internal.AttributionIdentifiers");
                AndroidJavaObject attributionIdentifiers = AttributionIdentifiersClass.CallStatic<AndroidJavaObject>("getAttributionIdentifiers", new object[] { currentActivity });
                mCache.MobileAdvertisingId = attributionIdentifiers.Call<string>("getAttributionId");
                Debug.Log("Android Advertising Id via com.facebook.internal.AttributionIdentifiers: " + mCache.MobileAdvertisingId);
            }
            catch { Debug.Log("com.facebook.internal.AttributionIdentifiers is not available."); }

            // Try old Android SDK
            if(string.IsNullOrEmpty(mCache.MobileAdvertisingId))
            {
                // Old Android SDK method
                try
                {
                    AndroidJavaClass FacebookSettingsClass = new AndroidJavaClass("com.facebook.Settings");
                    AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
                    mCache.MobileAdvertisingId = FacebookSettingsClass.CallStatic<string>("getAttributionId", new object[] { contentResolver });
                    Debug.Log("Android Advertising Id via com.facebook.Settings: " + mCache.MobileAdvertisingId);
                }
                catch { Debug.Log("com.facebook.Settings is not available."); }
            }

            // Check for Google Advertising Id
            if(string.IsNullOrEmpty(mCache.MobileAdvertisingId))
            {
                try
                {
                    AndroidJavaClass GooglePlayServicesUtilClass = new AndroidJavaClass("com.google.android.gms.common.GooglePlayServicesUtil");
                    if(GooglePlayServicesUtilClass.CallStatic<int>("isGooglePlayServicesAvailable", new object[] { currentActivity }) == 0)
                    {
                        // TODO: Retrieve advertising id
                        Debug.Log("isGooglePlayServicesAvailable == TRUE");
                    }
                }
                catch { Debug.Log("Google Play Services are not available."); }
            }
#   elif UNITY_IPHONE
            // TODO: C-call in to native code
            byte[] buffer = new byte[256];
            GCHandle pinnedArray = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            UIntPtr result = TeakHelper_GetAttributionId(pinnedArray.AddrOfPinnedObject(), new UIntPtr(256));
            if(!result.Equals(UIntPtr.Zero))
            {
                mCache.MobileAdvertisingId = System.Text.Encoding.UTF8.GetString(buffer);
            }
            pinnedArray.Free();
#   endif
        }

        // Resolve to a custom audience id
        if(!string.IsNullOrEmpty(mCache.MobileAdvertisingId) &&
            string.IsNullOrEmpty(mCache.CustomAudienceId))
        {
            string url = string.Format("https://graph.facebook.com/{0}/custom_audience_third_party_id?udid={1}", mFacebookAppId, mCache.MobileAdvertisingId);
            Debug.Log(url);
            UnityEngine.WWW request = new UnityEngine.WWW(url);
            yield return request;
            if(string.IsNullOrEmpty(request.error))
            {
                Dictionary<string, object> reply = Json.Deserialize(request.text) as Dictionary<string, object>;
                if(reply != null && reply.ContainsKey("custom_audience_third_party_id"))
                {
                    mCache.CustomAudienceId = reply["custom_audience_third_party_id"] as string;
                }
            }
        }
#endif

#endif // End temporary remove
        yield return null;
    }
    #endregion

    #region Metrics
    private IEnumerator sendInstallMetricIfNeeded(float delay = 0.0f)
    {
        if(!mCache.InstallMetricSent)
        {
            if(delay > 0.0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Debug.Log("Sending install metric.");
            Dictionary<string, object> payload = new Dictionary<string, object>() {
                {"install_date", mCache.InstallDate}
            };

            Request request = new Request(ServiceType.Metrics, "/install.json", payload);
            yield return StartCoroutine(signedRequestCoroutine(request, (Response response, string errorText, Dictionary<string, object> reply) => {
                Debug.Log("Reply from install metric: " + response);
                if(response == Response.OK)
                {
                    mCache.MarkInstallMetricSent();
                }
                else
                {
                    StartCoroutine(sendInstallMetricIfNeeded(UnityEngine.Random.Range(1.0f, 5.0f)));
                }
            }));
        }
    }

    private IEnumerator sendAppOpenedEvent()
    {
        yield return StartCoroutine(cachedRequestCoroutine(ServiceType.Metrics, "/app_opened.json", new Dictionary<string, object>() {}, null));
    }
    #endregion

    #region Teak request coroutines
    private string buildURLParamsFromDictionary(Dictionary<string, object> urlParams)
    {
        StringBuilder builder = new StringBuilder();
        foreach(KeyValuePair<string, object> entry in urlParams)
        {
            builder.AppendFormat("{0}={1}&", entry.Key,
                UnityEngine.WWW.EscapeURL(entry.Value.ToString()));
        }
        builder.Remove(builder.Length - 1, 1);
        return builder.ToString();
    }

    private void addCommonPayloadFields(Dictionary<string, object> payload)
    {
        // Common
        payload.Add("sdk_version", Teak.SDKVersion);
        payload.Add("sdk_platform", SystemInfo.operatingSystem.Replace(" ", "_").ToLower());
        payload.Add("sdk_type", "unity");
        payload.Add("app_id", mFacebookAppId);
        payload.Add("app_version", mBundleVersion);
        payload.Add("app_build_id", "TODO: USER SPECIFIED BUILD ID");
        payload.Add("user_id", mUserId);
        if(!string.IsNullOrEmpty(this.Tag)) payload.Add("tag", this.Tag);
        if(!string.IsNullOrEmpty(mSessionId)) payload.Add("session_id", mSessionId);
    }

    private IEnumerator servicesDiscoveryCoroutine(float delay = 0.0f)
    {
        if(delay > 0.0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if(string.IsNullOrEmpty(mFacebookAppId))
        {
            StartCoroutine(servicesDiscoveryCoroutine(UnityEngine.Random.Range(0.5f, 2.0f)));
        }
        else
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            if(!string.IsNullOrEmpty(mLaunchURL)) payload.Add("launch_url", mLaunchURL);
            payload.Add("_method", "GET");

            Request servicesRequest = new Request(ServiceType.Discovery, "/services.json", payload);
            StartCoroutine(signedRequestCoroutine(servicesRequest, (Response response, string errorText, Dictionary<string, object> reply) => {
                if(string.IsNullOrEmpty(errorText))
                {
                    mPostHostname = reply["post"] as string;
                    mAuthHostname = reply["auth"] as string;
                    mMetricsHostname = reply["metrics"] as string;

                    if(reply.ContainsKey("session_id"))
                    {
                        mSessionId = reply["session_id"] as string;
                    }

                    foreach(CachedRequest crequest in mCache)
                    {
                        StartCoroutine(signedRequestCoroutine(crequest, cachedRequestHandler(crequest, null)));
                    }
                }
                else
                {
                    StartCoroutine(servicesDiscoveryCoroutine(UnityEngine.Random.Range(5.0f, 15.0f)));
                }
            }));
        }
    }

    private IEnumerator validateUserCoroutine(string accessTokenOrFacebookId, float delay = 0.0f)
    {
        if(delay > 0.0f)
        {
            yield return new WaitForSeconds(delay);
        }

        mAccessTokenOrFacebookId = accessTokenOrFacebookId;

        Dictionary<string, object> payload = new Dictionary<string, object>();
        payload.Add("access_token", mAccessTokenOrFacebookId);

        Request request = new Request(ServiceType.Auth, string.Format("/games/{0}/users.json", mFacebookAppId), payload);
        yield return StartCoroutine(signedRequestCoroutine(request, (Response response, string errorText, Dictionary<string, object> reply) => {
            switch(response)
            {
                // TODO: If we have an access token, load custom audience id
                case Response.NetworkError:
                    StartCoroutine(validateUserCoroutine(accessTokenOrFacebookId, UnityEngine.Random.Range(0.5f, 2.0f)));
                    break;

                case Response.OK:
                    this.Status = AuthStatus.Ready;
                    break;

                case Response.ReadOnly:
                    this.Status = AuthStatus.ReadOnly;
                    break;

                default:
                    this.Status = AuthStatus.NotAuthorized;
                    break;
            }
        }));
    }

    private IEnumerator cachedRequestCoroutine(ServiceType serviceType,
                                               string endpoint,
                                               Dictionary<string, object> parameters,
                                               RequestResponse callback = null)
    {
        CachedRequest cachedRequest = mCache.CacheRequest(serviceType, endpoint, parameters);
        yield return StartCoroutine(signedRequestCoroutine(cachedRequest, cachedRequestHandler(cachedRequest, callback)));
    }

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
                kvList.Add(string.Format("{0}={1}", key, asStr));
            }
            else
            {
                kvList.Add(string.Format("{0}={1}", key,
                    Json.Serialize(urlParams[key])));
            }
        }
        string payload = string.Join("&", kvList.ToArray());
        string signString = string.Format("{0}\n{1}\n{2}\n{3}", "POST", hostname.Split(new char[]{':'})[0], endpoint, payload);
        string sig = AWSSDKUtils.HMACSign(signString, secret, KeyedHashAlgorithm.Create("HMACSHA256"));
        return sig;
    }

    private IEnumerator signedRequestCoroutine(Request teakRequest,
                                               RequestResponse callback = null)
    {
        Response ret = Response.UnknownError;
        string errorText = null;
        string hostname = hostForServiceType(teakRequest.ServiceType);

        if(string.IsNullOrEmpty(hostname))
        {
            if(callback != null) callback(Response.NetworkError, "", null);
            return false;
        }

        if(string.IsNullOrEmpty(mUserId))
        {
            throw new NullReferenceException("UserId is empty. Assign a UserId before using Teak.");
        }

        // Delay if needed
        if(teakRequest.DelayInSeconds > 0.0f)
        {
            yield return new WaitForSeconds(teakRequest.DelayInSeconds);
        }

        Dictionary<string, object> urlParams = new Dictionary<string, object> {
            {"request_date", teakRequest.RequestDate},
            {"request_id", teakRequest.RequestId}
        };
        Dictionary<string, object> parameters = teakRequest.Parameters;

        // If this has an attached image, bytes will be placed here.
        byte[] imageBytes = null;

        if(parameters != null)
        {
            // Check for image on dynamic objects
            if(parameters.ContainsKey("object_properties"))
            {
                IDictionary objectProperties = parameters["object_properties"] as IDictionary;
                object image = objectProperties["image"];
                Texture2D imageTex2D;
                if((imageTex2D = image as Texture2D) != null)
                {
                    imageBytes = imageTex2D.EncodeToPNG();
                    using(SHA256 sha256 = SHA256Managed.Create())
                    {
                        objectProperties["image_sha"] = BitConverter.ToString(sha256.ComputeHash(imageBytes)).Replace("-", string.Empty);
                    }
                }
                else if(image is string)
                {
                    objectProperties["image_url"] = image;
                }
                objectProperties.Remove("image");
            }

            // Merge params
            foreach(KeyValuePair<string, object> entry in parameters)
            {
                urlParams[entry.Key] = entry.Value;
            }
        }

        addCommonPayloadFields(urlParams);

        string sig = signParams(hostname, teakRequest.Endpoint, mTeakAPIKey, urlParams);
        urlParams["sig"] = sig;

        // Copy url params to form payload
        UnityEngine.WWWForm formPayload = new UnityEngine.WWWForm();
        string[] keys = new string[urlParams.Keys.Count];
        urlParams.Keys.CopyTo(keys, 0);
        foreach(string key in keys)
        {
            string asStr;
            if((asStr = urlParams[key] as string) != null)
            {
                formPayload.AddField(key, asStr);
            }
            else
            {
                formPayload.AddField(key,
                    Json.Serialize(urlParams[key]));
            }
        }

        // Attach image
        if(imageBytes != null)
        {
            formPayload.AddBinaryData("image_bytes", imageBytes);
        }

        UnityEngine.WWW request = new UnityEngine.WWW(string.Format("{0}://{1}{2}", mURLScheme, hostname, teakRequest.Endpoint), formPayload);
        yield return request;

        Dictionary<string, object> reply = null;
        int statusCode = 0;
        if(!string.IsNullOrEmpty(request.error))
        {
            errorText = request.error;
            Match match = Regex.Match(errorText, "^([0-9]+)");
            if(match.Success)
            {
                statusCode = int.Parse(match.Value);
            }
            else if(errorText.StartsWith("Resolving host timed out:"))
            {
                ret = Response.NetworkError;
            }
            else
            {
                Debug.Log(errorText);
                Debug.Log(string.Format("{0}://{1}{2}", mURLScheme, hostname, teakRequest.Endpoint));
                Debug.Log(Json.Serialize(urlParams));

            }
        }
        else
        {
            Match match = Regex.Match(request.responseHeaders["STATUS"], "^([0-9]+)");
            if(match.Success)
            {
                statusCode = int.Parse(match.Value);
            }

            if(!string.IsNullOrEmpty(request.text))
            {
                reply = Json.Deserialize(request.text) as Dictionary<string, object>;
            }
        }

        switch(statusCode)
        {
            case 201:
            case 200: // Successful
                ret = Response.OK;
                if(teakRequest.ServiceType < 0) this.Status = AuthStatus.Ready;
                break;

            case 401: // User has not authorized 'publish_actions', read only
                ret = Response.ReadOnly;
                if(teakRequest.ServiceType < 0) this.Status = AuthStatus.ReadOnly;
                break;

            case 402: // Service tier exceeded, not posted
                ret = Response.UserLimitHit;
                if(teakRequest.ServiceType < 0) this.Status = AuthStatus.Ready;
                break;

            case 403: // Authentication error, app secret incorrect
                ret = Response.BadApiKey;
                if(teakRequest.ServiceType < 0) this.Status = AuthStatus.Ready;
                break;

            case 404: // Resource not found
                ret = Response.NotFound;
                if(teakRequest.ServiceType < 0) this.Status = AuthStatus.Ready;
                break;

            case 405: // User is not authorized for Facebook App
                ret = Response.NotAuthorized;
                if(teakRequest.ServiceType < 0) this.Status = AuthStatus.NotAuthorized;
                break;

            case 424: // Dynamic OG object not created due to parameter error
                ret = Response.ParameterError;
                if(teakRequest.ServiceType < 0) this.Status = AuthStatus.Ready;
                break;
        }
        if(callback != null) callback(ret, errorText, reply);
    }
    #endregion

    #region Service Type
    private enum ServiceType : int
    {
        Discovery   = -2,
        Metrics     = -1,
        Auth        =  1,
        Post        =  2
    }

    private string hostForServiceType(ServiceType type)
    {
        switch(type)
        {
            case ServiceType.Discovery:     return mServicesDiscoveryHost;
            case ServiceType.Auth:          return mAuthHostname;
            case ServiceType.Metrics:       return mMetricsHostname;
            case ServiceType.Post:          return mPostHostname;
        }
        return null;
    }
    #endregion

    #region iOS Imports
#if UNITY_IPHONE
    [DllImport("__Internal")]
    extern static UIntPtr TeakHelper_GetAttributionId(IntPtr buffer, UIntPtr bufferSize);
#endif
    #endregion

    #region Member Variables
    private static Teak mInstance = null;
    private AuthStatus mAuthStatus;
    private string mUserId;
    private string mServicesDiscoveryHost = "services.gocarrot.com";
    private string mURLScheme = "https";
    private string mPostHostname;
    private string mAuthHostname;
    private string mMetricsHostname;
    private string mFacebookAppId;
    private string mTeakAPIKey;
    private string mBundleVersion;
    private string mAccessTokenOrFacebookId;
    private string mLaunchURL;
    private string mSessionId;
    private Cache mCache;
    private long mSessionStartTime;
    private FacebookSDKType mFacebookSDKType = FacebookSDKType.None;
    private string mUnityObject2Instance = null;
    private MethodInfo mOfficialFBSDKFeedMethod;
    private PropertyInfo mFBResultPropertyText;
    private PropertyInfo mFBResultPropertyError;
    private Type mFacebookDelegateType;
    #endregion
    /// @endcond
}