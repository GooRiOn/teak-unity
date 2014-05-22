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
using System.Security.Cryptography.X509Certificates;
#endregion

/// <summary>
/// A MonoBehaviour which can be attached to a Unity GameObject to
/// provide access to Teak functionality.
/// </summary>
public partial class Teak : MonoBehaviour
{
    /// <summary>
    /// Gets the <see cref="Teak"/> singleton.
    /// </summary>
    /// <value> The <see cref="Teak"/> singleton.</value>
    public static Teak Instance
    {
        get
        {
            if(mInstance == null)
            {
                mInstance = FindObjectOfType(typeof(Teak)) as Teak;

                if(mInstance == null)
                {
                    GameObject teakGameObject = GameObject.Find("TeakGameObject");
                    if(teakGameObject == null)
                    {
                        teakGameObject = new GameObject("TeakGameObject");
                        teakGameObject.AddComponent("Teak");
                    }
                    mInstance = teakGameObject.GetComponent<Teak>();
                }

                mInstance.mFacebookAppId = TeakSettings.AppId;
                mInstance.mTeakAppSecret = TeakSettings.AppSecret;
                mInstance.mBundleVersion = TeakSettings.BundleVersion;

                if(string.IsNullOrEmpty(mInstance.mFacebookAppId))
                {
                    throw new ArgumentException("Teak App Id has not been configured. Use the configuration tool in the 'Edit/Teak' menu to assign your Teak App Id and Secret.");
                }
            }
            return mInstance;
        }
    }

    /// <summary>
    /// Represents a Teak authentication status for a user.
    /// </summary>
    public enum AuthStatus : int
    {
        /// <summary>The current user has not yet authorized the app, or has deauthorized the app.</summary>
        NotAuthorized = -1,

        /// <summary>The current authentication status has not been determined.</summary>
        Undetermined = 0,

        /// <summary>The current user has not granted the 'publish_actions' permission, or has removed the permission.</summary>
        ReadOnly = 1,

        /// <summary>The current user has granted all needed permissions and Teak will send events to the Teak server.</summary>
        Ready = 2
    }

    /// <summary>
    /// Responses to Teak requests.
    /// </summary>
    public enum Response
    {
        /// <summary>Successful.</summary>
        OK,

        /// <summary>User has not authorized 'publish_actions', read only.</summary>
        ReadOnly,

        /// <summary>Service tier exceeded, not posted.</summary>
        UserLimitHit,

        /// <summary>Authentication error, app secret incorrect.</summary>
        BadAppSecret,

        /// <summary>Resource not found.</summary>
        NotFound,

        /// <summary>User is not authorized for Facebook App.</summary>
        NotAuthorized,

        /// <summary>Dynamic OG object not created due to parameter error.</summary>
        ParameterError,

        /// <summary>Network error.</summary>
        NetworkError,

        /// <summary>Undetermined error.</summary>
        UnknownError,
    }

    /// <summary>Teak SDK version.</summary>
    public static readonly string SDKVersion = "2.0.0";

    /// <summary>
    /// Teak debug users which can be assigned to UserId in order to simulate
    /// different cases for use.
    /// </summary>
    public class DebugUser
    {
        /// <summary>A user which never exists.</summary>
        public static readonly string NoSuchUser = "nosuchuser";

        /// <summary>A user which has not authorized the 'publish_actions' permission.</summary>
        public static readonly string ReadOnlyUser = "nopublishactions";

        /// <summary>A user which deauthorized the Facebook application.</summary>
        public static readonly string DeauthorizedUser = "deauthorized";
    }

    /// <summary>
    /// Return the string value of an <see cref="AuthStatus"/> value.
    /// </summary>
    /// <returns>The string description of an <see cref="AuthStatus"/>.</returns>
    public static string authStatusString(AuthStatus authStatus)
    {
        switch(authStatus)
        {
            case Teak.AuthStatus.NotAuthorized: return "Teak user has not authorized the application.";
            case Teak.AuthStatus.Undetermined: return "Teak user status is undetermined.";
            case Teak.AuthStatus.ReadOnly: return "Teak user has not allowed the 'publish_actions' permission.";
            case Teak.AuthStatus.Ready: return "Teak user is authorized.";
            default: return "Invalid Teak AuthStatus.";
        }
    }

    /// <summary>
    /// The delegate type for the <see cref="AuthenticationStatusChanged"/> event.
    /// </summary>
    /// <param name="sender">The object which dispatched the <see cref="AuthenticationStatusChanged"/> event.</param>
    /// <param name="status">The new authentication status.</param>
    public delegate void AuthenticationStatusChangedHandler(object sender, AuthStatus status);

    /// <summary>
    /// An event which will notify listeners when the authentication status for the Teak user has changed.
    /// </summary>
    public static event AuthenticationStatusChangedHandler AuthenticationStatusChanged;

    /// <summary>
    /// The callback delegate type for Teak requests.
    /// </summary>
    public delegate void RequestResponse(Response response, string errorText, Dictionary<string, object> reply);

    /// <summary>
    /// The callback delegate for canMakeFeedPost.
    /// </summary>
    public delegate void CanMakeFeedPostResponse(bool canMakeFeedPost);

    /// <summary>
    /// Check the authentication status of the current Teak user.
    /// </summary>
    /// <value>The <see cref="Teak.AuthStatus"/> of the current Teak user.</value>
    public AuthStatus Status
    {
        get
        {
            return mAuthStatus;
        }
        private set
        {
            if(mAuthStatus != value)
            {
                mAuthStatus = value;
                if(AuthenticationStatusChanged != null)
                {
                    AuthenticationStatusChanged(this, mAuthStatus);
                }

                foreach(TeakCache.CachedRequest request in mTeakCache.RequestsInCache())
                {
                    StartCoroutine(signedRequestCoroutine(request, cachedRequestHandler(request, null)));
                }
            }
        }
    }

    /// <summary>
    /// The user id for the current Teak user.
    /// </summary>
    /// <value>The user id of the current Teak user.</value>
    public string UserId
    {
        get
        {
            return mUserId;
        }
        set
        {
            if(mUserId != value)
            {
                mUserId = value;
                this.Status = AuthStatus.Undetermined;
            }
        }
    }

    /// <summary>
    /// An app-specified tag for associating metrics with A/B testing groups or other purposes.
    /// </summary>
    /// <value>The assigned tag.</value>
    public string Tag
    {
        get;
        set;
    }

    public DateTime InstallDate
    {
        get;
        private set;
    }

    /// <summary>
    /// Validate a Facebook user to allow posting of Teak events.
    /// </summary>
    /// <remarks>
    /// This method will trigger notification of authentication status using the <see cref="AuthenticationStatusChanged"/> event.
    /// </remarks>
    /// <param name="accessTokenOrFacebookId">Facebook user access token or Facebook User Id.</param>
    public void validateUser(string accessTokenOrFacebookId)
    {
        StartCoroutine(validateUserCoroutine(accessTokenOrFacebookId));
    }

    /// <summary>
    /// Post an achievement to Teak.
    /// </summary>
    /// <param name="achievementId">Teak achievement id.</param>
    /// <param name="callback">Optional <see cref="RequestResponse"/> which will be used to deliver the reply.</param>
    public void postAchievement(string achievementId, RequestResponse callback = null)
    {
        if(string.IsNullOrEmpty(achievementId))
        {
            throw new ArgumentNullException("achievementId must not be null or empty string.", "achievementId");
        }

        StartCoroutine(cachedRequestCoroutine(ServiceType.Post, "/me/achievements.json", new Dictionary<string, object>() {
                {"achievement_id", achievementId}
        }, callback));
    }

    /// <summary>
    /// Post a high score to Teak.
    /// </summary>
    /// <param name="score">Score.</param>
    /// <param name="callback">Optional <see cref="RequestResponse"/> which will be used to deliver the reply.</param>
    public void postHighScore(uint score, RequestResponse callback = null)
    {
        StartCoroutine(cachedRequestCoroutine(ServiceType.Post, "/me/scores.json", new Dictionary<string, object>() {
                {"value", score}
        }, callback));
    }

    /// <summary>
    /// Sends an Open Graph action which will use an existing object.
    /// </summary>
    /// <param name="actionId">Teak action id.</param>
    /// <param name="objectInstanceId">Teak object instance id.</param>
    /// <param name="callback">Optional <see cref="RequestResponse"/> which will be used to deliver the reply.</param>
    public void postAction(string actionId, string objectInstanceId, RequestResponse callback = null)
    {
        postAction(actionId, null, objectInstanceId, callback);
    }

    /// <summary>
    /// Sends an Open Graph action which will use an existing object.
    /// </summary>
    /// <param name="actionId">Teak action id.</param>
    /// <param name="actionProperties">Parameters to be submitted with the action.</param>
    /// <param name="objectInstanceId">Teak object instance id.</param>
    /// <param name="callback">Optional <see cref="RequestResponse"/> which will be used to deliver the reply.</param>
    public void postAction(string actionId, IDictionary actionProperties, string objectInstanceId,
                           RequestResponse callback = null)
    {
        if(string.IsNullOrEmpty(objectInstanceId))
        {
            throw new ArgumentNullException("objectInstanceId must not be null or empty string.", "objectInstanceId");
        }

        if(string.IsNullOrEmpty(actionId))
        {
            throw new ArgumentNullException("actionId must not be null or empty string.", "actionId");
        }

        Dictionary<string, object> parameters = new Dictionary<string, object>() {
            {"action_id", actionId},
            {"action_properties", actionProperties == null ? new Dictionary<string, object>() : actionProperties},
            {"object_properties", new Dictionary<string, object>()}
        };
        parameters["object_instance_id"] = objectInstanceId;

        StartCoroutine(cachedRequestCoroutine(ServiceType.Post, "/me/actions.json", parameters, callback));
    }

    /// <summary>
    /// Sends an Open Graph action which will create a new object.
    /// </summary>
    /// <param name="actionId">Teak action id.</param>
    /// <param name="templateId">Teak template instance id.</param>
    /// <param name="objectProperties">Properties used to fill in the object template.</param>
    /// <param name="callback">Optional <see cref="RequestResponse"/> which will be used to deliver the reply.</param>
    public void postAction(string actionId, string templateId,
                           IDictionary objectProperties,
                           RequestResponse callback = null)
    {
        postAction(actionId, templateId, null, objectProperties, callback);
    }

    /// <summary>
    /// Sends an Open Graph action which will create a new object.
    /// </summary>
    /// <param name="actionId">Teak action id.</param>
    /// <param name="templateId">Teak template instance id.</param>
    /// <param name="actionProperties">Parameters to be submitted with the action.</param>
    /// <param name="objectProperties">Properties used to fill in the object template.</param>
    /// <param name="callback">Optional <see cref="RequestResponse"/> which will be used to deliver the reply.</param>
    public void postAction(string actionId, string templateId,
                           IDictionary actionProperties,
                           IDictionary objectProperties,
                           RequestResponse callback = null)
    {
        if(string.IsNullOrEmpty(actionId))
        {
            throw new ArgumentNullException("actionId must not be null or empty string.", "actionId");
        }

        if(string.IsNullOrEmpty(templateId))
        {
            throw new ArgumentNullException("templateId must not be null or empty string.", "templateId");
        }

        if(objectProperties == null)
        {
            throw new ArgumentNullException("objectProperties must not be null.", "objectProperties");
        }

        Dictionary<string, object> parameters = new Dictionary<string, object>() {
            {"action_id", actionId},
            {"action_properties", actionProperties == null ? new Dictionary<string, object>() : actionProperties},
            {"object_properties", objectProperties}
        };
        parameters["object_instance_id"] = templateId;

        StartCoroutine(cachedRequestCoroutine(ServiceType.Post, "/me/actions.json", parameters, callback));
    }

    /// <summary>
    /// Inform Teak about a purchase of premium currency for metrics tracking.
    /// </summary>
    /// <param name="amount">The amount of real money spent.</param>
    /// <param name="currency">The type of real money spent (eg. USD).</param>
    /// <param name="callback">Optional <see cref="RequestResponse"/> which will be used to deliver the reply.</param>
    public void postPremiumCurrencyPurchase(float amount, string currency, RequestResponse callback = null)
    {
        StartCoroutine(cachedRequestCoroutine(ServiceType.Metrics, "/purchase.json", new Dictionary<string, object>() {
            {"amount", amount},
            {"currency", currency}
        }, callback));
    }

    /// <summary>
    /// Pop-up a feed post dialog.
    /// </summary>
    /// <param name="objectInstanceId">The instance id of the feed post.</param>
    /// <param name="objectProperties">The properties required to fill in data templating in the feed post.</param>
    /// <param name="callback">Optional <see cref="RequestResponse"/> which will be used to deliver the reply.</param>
    public void popupFeedPost(string objectInstanceId, Dictionary<string, object> objectProperties = null,
                              RequestResponse callback = null)
    {
        if(string.IsNullOrEmpty(objectInstanceId))
        {
            throw new ArgumentNullException("objectInstanceId must not be null or empty string.", "objectInstanceId");
        }

        Dictionary<string, object> parameters = new Dictionary<string, object>() {
            {"object_instance_id", objectInstanceId},
            {"object_properties", objectProperties == null ? new Dictionary<string, object>() : objectProperties}
        };

        Request request = new Request(ServiceType.Post, "/me/feed_post.json", parameters);
        StartCoroutine(signedRequestCoroutine(request, (Response response, string errorText, Dictionary<string, object> reply) => {
            if(response == Response.OK)
            {
                Dictionary<string, object> fb_data = reply["fb_data"] as Dictionary<string, object>;

                if(fb_data["method"] as string == "feed")
                {
                    if(mFacebookSDKType == FacebookSDKType.OfficialUnitySDK)
                    {
                        string actionName = "";
                        string actionLink = "";
                        if(fb_data.ContainsKey("actions"))
                        {
                            object[] actions = fb_data["actions"] as object[];
                            if(actions != null && actions.Length > 0)
                            {
                                // Will only ever have 1 element
                                Dictionary<string, object> action = actions[0] as Dictionary<string, object>;
                                actionName = action["name"] as string;
                                actionLink = action["link"] as string;
                            }
                        }

                        MethodInfo mi1 = typeof(Teak).GetMethod("unitySDKFeedPostCallback", BindingFlags.NonPublic | BindingFlags.Instance);
                        object fbDelegate = Delegate.CreateDelegate(mFacebookDelegateType, this, mi1);
                        mOfficialFBSDKFeedMethod.Invoke(null, new object[] {
                            "", // FBID of timeline this should be posted to (default: current)
                            fb_data["link"] as string,
                            "", // Name of the link (default: App Name)
                            fb_data["caption"] as string,
                            fb_data["description"] as string,
                            fb_data["picture"] as string,
                            "", // URL of audio/video content
                            actionName, // Action name
                            actionLink, // Action link
                            fb_data["ref"] as string,
                            new Dictionary<string, string[]>{},
                            fbDelegate
                        });
                    }
                    else if(mFacebookSDKType == FacebookSDKType.JavaScriptSDK)
                    {
                        Application.ExternalEval("FB.ui(" + Json.Serialize(fb_data) + ", function(response) {" +
                                                 "    if(response == null || response == undefined) { response = {canceled: true}; }" +
                                                 "    " + mUnityObject2Instance + ".getUnity().SendMessage('TeakGameObject', 'javascriptSDKFeedPostCallback', JSON.stringify(response));" +
                                                 "});"
                        );
                    }
                }
            }
            else
            {
                // Something-something danger zone
            }
        }));
    }

    /// <summary>
    /// Query the Teak server to see if a user should be offered the option of making a feed post.
    /// </summary>
    /// <param name="objectInstanceId">The instance id of the feed post.</param>
    /// <param name="canMakeFeedPostResponse">The response from the server saying if the user should be given the option to share this post.</param>
    public void canMakeFeedPost(string objectInstanceId, CanMakeFeedPostResponse canMakeFeedPostResponse)
    {
        if(string.IsNullOrEmpty(objectInstanceId))
        {
            throw new ArgumentNullException("objectInstanceId must not be null or empty string.", "objectInstanceId");
        }

        Dictionary<string, object> parameters = new Dictionary<string, object>() {
            {"object_instance_id", objectInstanceId}
        };

        Request request = new Request(ServiceType.Post, "/me/can_post.json", parameters);
        StartCoroutine(signedRequestCoroutine(request, (Response response, string errorText, Dictionary<string, object> reply) => {
            canMakeFeedPostResponse(response == Response.OK);
        }));
    }

    #region Internal
    /// @cond hide_from_doxygen
    public enum FacebookSDKType : int
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
        mAttributionId = null;
        mTeakCache = new TeakCache();
        this.InstallDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(mTeakCache.InstallDate);

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

    private RequestResponse cachedRequestHandler(TeakCache.CachedRequest cachedRequest,
                                                       RequestResponse callback)
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
    /// @endcond
    #endregion

    #region Metrics
    /// @cond hide_from_doxygen
    private IEnumerator sendInstallMetricIfNeeded()
    {
        if(!mTeakCache.InstallMetricSent)
        {
            yield return StartCoroutine(cachedRequestCoroutine(ServiceType.Metrics, "/install.json", new Dictionary<string, object>() {
                    {"install_date", mTeakCache.InstallDate}
            }, (Response response, string errorText, Dictionary<string, object> reply) => {
                if(response == Response.OK)
                {
                    mTeakCache.markInstallMetricSent();
                }
            }));
        }
        yield return null;
    }

    private IEnumerator sendAppOpenedEvent()
    {
        yield return StartCoroutine(cachedRequestCoroutine(ServiceType.Metrics, "/app_opened.json", new Dictionary<string, object>() {}, null));
    }
    /// @endcond
    #endregion

    #region MonoBehaviour
    /// @cond hide_from_doxygen
    void Start()
    {
        DontDestroyOnLoad(this);

        // Get attribution id so we can generate a custom audience id via graph call
        // to app_id/custom_audience_third_party_id
#if !UNITY_EDITOR
#   if UNITY_ANDROID
        AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

        // New Android SDK method
        try
        {
            AndroidJavaClass AttributionIdentifiersClass = new AndroidJavaClass("com.facebook.internal.AttributionIdentifiers");
            AndroidJavaObject attributionIdentifiers = AttributionIdentifiersClass.CallStatic<AndroidJavaObject>("getAttributionIdentifiers", new object[] { currentActivity });
            mAttributionId = attributionIdentifiers.Call<string>("getAttributionId");
        }
        catch { Debug.Log("com.facebook.internal.AttributionIdentifiers is not available."); }

        // Try old Android SDK
        if(string.IsNullOrEmpty(mAttributionId))
        {
            // Old Android SDK method
            try
            {
                AndroidJavaClass FacebookSettingsClass = new AndroidJavaClass("com.facebook.Settings");
                AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
                mAttributionId = FacebookSettingsClass.CallStatic<string>("getAttributionId", new object[] { contentResolver });
            }
            catch { Debug.Log("com.facebook.Settings is not available."); }
        }

        // Check for Google Advertising Id
        if(string.IsNullOrEmpty(mAttributionId))
        {
            try
            {
                AndroidJavaClass GooglePlayServicesUtilClass = new AndroidJavaClass("com.google.android.gms.common.GooglePlayServicesUtil");
                if(GooglePlayServicesUtilClass.CallStatic<int>("isGooglePlayServicesAvailable", new object[] { currentActivity }) == 0)
                {
                    Debug.Log("isGooglePlayServicesAvailable == TRUE");
                }
            }
            catch { Debug.Log("Google Play Services are not available."); }
        }
#   elif UNITY_IPHONE
        // TODO: C-call in to native code
#   endif
        if(string.IsNullOrEmpty(mAttributionId))
        {
            Debug.Log("Attribution Id for Advertising not found.");
        }
        else
        {
            Debug.Log("Advertising identifier: " + mAttributionId);
        }
#endif

        // Do services discovery
        StartCoroutine(servicesDiscoveryCoroutine());

#if UNITY_IPHONE || UNITY_ANDROID
        StartCoroutine(sendInstallMetricIfNeeded());
        mSessionStartTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
#else
        StartCoroutine(sendAppOpenedEvent());
#endif
    }

#if UNITY_IPHONE || UNITY_ANDROID
    void OnApplicationPause(bool isPaused)
    {
        if(isPaused)
        {
            long sessionEndTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            StartCoroutine(cachedRequestCoroutine(ServiceType.Metrics, "/session.json", new Dictionary<string, object>() {
                    {"start_time", mSessionStartTime},
                    {"end_time", sessionEndTime}
            }, null));
        }
        else
        {
            mSessionStartTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }
#endif

    void OnApplicationQuit()
    {
        mTeakCache.Dispose();
        Destroy(this);
    }
    /// @endcond
    #endregion

    #region Service Type
    /// @cond hide_from_doxygen
    public enum ServiceType : int
    {
        Auth    = -2,
        Metrics = -1,
        Post    = 2
    }

    private string hostForServiceType(ServiceType type)
    {
        switch(type)
        {
            case ServiceType.Auth: return mAuthHostname;
            case ServiceType.Metrics: return mMetricsHostname;
            case ServiceType.Post: return mPostHostname;
        }
        return null;
    }
    /// @endcond
    #endregion

    #region Teak request coroutines
    /// @cond hide_from_doxygen
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

    delegate void PayloadUrlParamsHelperDelegate(string key, string value);
    private void addCommonPayloadFields(UnityEngine.WWWForm payload, Dictionary<string, object> urlParams)
    {
        // Helper
        PayloadUrlParamsHelperDelegate addKeyValue = (string key, string value) => {
            if(payload != null)   payload.AddField(key, value);
            if(urlParams != null) urlParams.Add(key, value);
        };

        // Common
        addKeyValue("sdk_version", Teak.SDKVersion);
        addKeyValue("sdk_platform", SystemInfo.operatingSystem.Replace(" ", "_").ToLower());
        addKeyValue("sdk_type", "unity");
        addKeyValue("app_id", mFacebookAppId);
        addKeyValue("app_version", mBundleVersion);
        addKeyValue("app_build_id", "TODO: USER SPECIFIED BUILD ID");
        if(!string.IsNullOrEmpty(mUserId)) addKeyValue("api_key", mUserId);
        if(!string.IsNullOrEmpty(this.Tag)) addKeyValue("tag", this.Tag);
        if(!string.IsNullOrEmpty(mSessionId)) addKeyValue("session_id", mSessionId);
    }

    private IEnumerator servicesDiscoveryCoroutine()
    {
        if(string.IsNullOrEmpty(mFacebookAppId))
        {
            yield return new WaitForSeconds(1);
            StartCoroutine(servicesDiscoveryCoroutine());
        }
        else
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            addCommonPayloadFields(null, payload);
            if(!string.IsNullOrEmpty(mLaunchURL)) payload.Add("launch_url", mLaunchURL);

            string urlString = String.Format("https://{0}/services.json?{1}", mServicesDiscoveryHost,
                buildURLParamsFromDictionary(payload));

            UnityEngine.WWW request = new UnityEngine.WWW(urlString);
            yield return request;

            if(request.error == null)
            {
                Dictionary<string, object> reply = Json.Deserialize(request.text) as Dictionary<string, object>;
                mPostHostname = reply["post"] as string;
                mAuthHostname = reply["auth"] as string;
                mMetricsHostname = reply["metrics"] as string;

                if(reply.ContainsKey("session_id"))
                {
                    mSessionId = reply["session_id"] as string;
                }

                if(!string.IsNullOrEmpty(mAccessTokenOrFacebookId))
                {
                    validateUser(mAccessTokenOrFacebookId);
                }
                else
                {
                    foreach(TeakCache.CachedRequest crequest in mTeakCache.RequestsInCache())
                    {
                        StartCoroutine(signedRequestCoroutine(crequest, cachedRequestHandler(crequest, null)));
                    }
                }
            }
            else
            {
                Debug.Log(request.error);
                yield return new WaitForSeconds(UnityEngine.Random.Range(5.0f, 15.0f));
                StartCoroutine(servicesDiscoveryCoroutine());
            }
        }
    }

    private IEnumerator validateUserCoroutine(string accessTokenOrFacebookId)
    {
        AuthStatus ret = AuthStatus.Undetermined;
        string hostname = hostForServiceType(ServiceType.Auth);
        mAccessTokenOrFacebookId = accessTokenOrFacebookId;

        if(string.IsNullOrEmpty(hostname))
        {
            return false;
        }

        if(string.IsNullOrEmpty(mUserId))
        {
            throw new NullReferenceException("UserId is empty. Assign a UserId before calling validateUser");
        }

        ServicePointManager.ServerCertificateValidationCallback = TeakCertValidator;

        UnityEngine.WWWForm payload = new UnityEngine.WWWForm();
        addCommonPayloadFields(payload, null);
        payload.AddField("access_token", mAccessTokenOrFacebookId);
        if(!string.IsNullOrEmpty(mAttributionId)) payload.AddField("attribution_id", mAttributionId);

        UnityEngine.WWW request = new UnityEngine.WWW(String.Format("https://{0}/games/{1}/users.json", hostname, mFacebookAppId), payload);
        yield return request;

        int statusCode = 0;
        if(request.error != null)
        {
            Match match = Regex.Match(request.error, "^([0-9]+)");
            if(match.Success)
            {
                statusCode = int.Parse(match.Value);
            }
            else
            {
                Debug.Log(request.error);
            }
        }
        else
        {
            // TODO: Change if JSON updates to include code
            // Dictionary<string, object> reply = Json.Deserialize(request.text) as Dictionary<string, object>;
            // statusCode = (int)((long)reply["code"]);
            statusCode = 200;
        }

        switch(statusCode)
        {
            case 201:
            case 200: // Successful
                ret = AuthStatus.Ready;
                break;

            case 401: // User has not authorized 'publish_actions', read only
                ret = AuthStatus.ReadOnly;
                break;

            case 404:
            case 405: // User is not authorized for Facebook App
            case 422: // User was not created
                ret = AuthStatus.NotAuthorized;
                break;
        }
        this.Status = ret;

        yield return ret;
    }

    private IEnumerator cachedRequestCoroutine(ServiceType serviceType,
                                               string endpoint,
                                               Dictionary<string, object> parameters,
                                               RequestResponse callback = null)
    {
        TeakCache.CachedRequest cachedRequest = mTeakCache.CacheRequest(serviceType, endpoint, parameters);
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

    private IEnumerator signedRequestCoroutine(Request teakRequest,
                                               RequestResponse callback = null)
    {
        Response ret = Response.UnknownError;
        string errorText = null;
        string hostname = hostForServiceType(teakRequest.ServiceType);

        if(string.IsNullOrEmpty(hostname))
        {
            if(callback != null) callback(Response.OK, "", null);
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

        ServicePointManager.ServerCertificateValidationCallback = TeakCertValidator;

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
                        objectProperties["image_sha"] = System.Text.Encoding.UTF8.GetString(sha256.ComputeHash(imageBytes));
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

        UnityEngine.WWWForm formPayload = new UnityEngine.WWWForm();
        addCommonPayloadFields(formPayload, urlParams);

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

        string sig = signParams(hostname, teakRequest.Endpoint, mTeakAppSecret, urlParams);
        formPayload.AddField("sig", sig);

        // Attach image
        if(imageBytes != null)
        {
            formPayload.AddBinaryData("image_bytes", imageBytes);
        }

        UnityEngine.WWW request = new UnityEngine.WWW(String.Format("https://{0}{1}", hostname, teakRequest.Endpoint), formPayload);
        yield return request;

        Dictionary<string, object> reply = null;
        int statusCode = 0;
        if(request.error != null)
        {
            Match match = Regex.Match(request.error, "^([0-9]+)");
            if(match.Success)
            {
                statusCode = int.Parse(match.Value);
            }
            else
            {
                errorText = request.error;
                Debug.Log(request.error);
            }
        }
        else
        {
            if(!string.IsNullOrEmpty(request.text))
            {
                reply = Json.Deserialize(request.text) as Dictionary<string, object>;
                if(reply.ContainsKey("code")) statusCode = (int)((long)reply["code"]);
            }
        }

        switch(statusCode)
        {
            case 201:
            case 200: // Successful
                ret = Response.OK;
                if(teakRequest.ServiceType != ServiceType.Metrics) this.Status = AuthStatus.Ready;
                break;

            case 401: // User has not authorized 'publish_actions', read only
                ret = Response.ReadOnly;
                if(teakRequest.ServiceType != ServiceType.Metrics) this.Status = AuthStatus.ReadOnly;
                break;

            case 402: // Service tier exceeded, not posted
                ret = Response.UserLimitHit;
                if(teakRequest.ServiceType != ServiceType.Metrics) this.Status = AuthStatus.Ready;
                break;

            case 403: // Authentication error, app secret incorrect
                ret = Response.BadAppSecret;
                if(teakRequest.ServiceType != ServiceType.Metrics) this.Status = AuthStatus.Ready;
                break;

            case 404: // Resource not found
                ret = Response.NotFound;
                if(teakRequest.ServiceType != ServiceType.Metrics) this.Status = AuthStatus.Ready;
                break;

            case 405: // User is not authorized for Facebook App
                ret = Response.NotAuthorized;
                if(teakRequest.ServiceType != ServiceType.Metrics) this.Status = AuthStatus.NotAuthorized;
                break;

            case 424: // Dynamic OG object not created due to parameter error
                ret = Response.ParameterError;
                if(teakRequest.ServiceType != ServiceType.Metrics) this.Status = AuthStatus.Ready;
                break;
        }
        if(callback != null) callback(ret, errorText, reply);
    }
    /// @endcond
    #endregion

    #region SSL Cert Validator
    /// @cond hide_from_doxygen
    private static bool TeakCertValidator(object sender, X509Certificate certificate,
                                            X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // This is not ideal
        return true;
    }
    /// @endcond
    #endregion

    #region Request
    /// @cond hide_from_doxygen
    public class Request
    {
        public Dictionary<string, object> Parameters
        {
            get;
            internal set;
        }

        public Teak.ServiceType ServiceType
        {
            get;
            internal set;
        }

        public string Endpoint
        {
            get;
            internal set;
        }

        public string RequestId
        {
            get;
            internal set;
        }

        public long RequestDate
        {
            get;
            internal set;
        }

        public float DelayInSeconds
        {
            get;
            internal set;
        }

        public Request() {}

        public Request(ServiceType serviceType, string endpoint, Dictionary<string, object> parameters)
        {
            this.ServiceType = serviceType;
            this.Endpoint = endpoint;
            this.Parameters = parameters;
            this.RequestDate = (long)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
            this.RequestId = System.Guid.NewGuid().ToString();
            this.DelayInSeconds = 0.0f;
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} {2} - {3}: {4}", '-', this.ServiceType, this.RequestId, this.Endpoint, Json.Serialize(this.Parameters));
        }
    }
    /// @endcond
    #endregion

    #region Member Variables
    private static Teak mInstance = null;
    private AuthStatus mAuthStatus;
    private string mUserId;
    private string mServicesDiscoveryHost = "services.gocarrot.com";
    private string mPostHostname;
    private string mAuthHostname;
    private string mMetricsHostname;
    private string mFacebookAppId;
    private string mTeakAppSecret;
    private string mBundleVersion;
    private string mAccessTokenOrFacebookId;
    private string mLaunchURL;
    private string mAttributionId;
    private string mSessionId;
    private TeakCache mTeakCache;
    private long mSessionStartTime;
    private FacebookSDKType mFacebookSDKType = FacebookSDKType.None;
    private string mUnityObject2Instance = null;
    private MethodInfo mOfficialFBSDKFeedMethod;
    private PropertyInfo mFBResultPropertyText;
    private PropertyInfo mFBResultPropertyError;
    private Type mFacebookDelegateType;
    #endregion
}
