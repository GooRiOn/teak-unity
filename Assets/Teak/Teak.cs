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
using UnityEngine;
using System.Reflection;
using System.Collections;
using GoCarrotInc.MiniJSON;
using System.Collections.Generic;
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
                mInstance.mTeakAPIKey = TeakSettings.APIKey;
                mInstance.mBundleVersion = TeakSettings.BundleVersion;

                if(string.IsNullOrEmpty(mInstance.mFacebookAppId))
                {
                    throw new ArgumentException("Teak App Id has not been configured. Use the configuration tool in the 'Edit/Teak' menu to assign your Teak App Id and API Key.");
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

        /// <summary>Authentication error, Teak API key incorrect.</summary>
        BadApiKey,

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
                Debug.Log("Auth status changed from " + authStatusString(mAuthStatus) + " to " + authStatusString(value));
                mAuthStatus = value;
                if(AuthenticationStatusChanged != null)
                {
                    AuthenticationStatusChanged(this, mAuthStatus);
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

    /// <summary>
    /// The install date reported by Teak.
    /// </summary>
    /// <value>The install date.</value>
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

    #region MonoBehaviour
    /// @cond hide_from_doxygen
    void Start()
    {
        DontDestroyOnLoad(this);

        // Load appropriate advertising ids from iOS/Android
        StartCoroutine(loadMobileAdvertisingIds());

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
        mCache.Close();
        Destroy(this);
    }
    /// @endcond
    #endregion
}
