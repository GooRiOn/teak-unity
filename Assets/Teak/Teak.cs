﻿#region License
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
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

using MiniJSON.Teak;
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
                        teakGameObject.AddComponent<Teak>();
                    }
                    mInstance = teakGameObject.GetComponent<Teak>();
                }
            }
            return mInstance;
        }
    }

    /// <summary>Teak SDK version.</summary>
    public static string Version
    {
        get
        {
            return TeakVersion.Version;
        }
    }

    /// <summary>The user identifier for the current user.</summary>
    public string UserId
    {
        get;
        private set;
    }

    /// <summary>
    /// Tell Teak how it should identify the current user.
    /// </summary>
    /// <remarks>
    /// This should be the same way you identify the user in your backend.
    /// </remarks>
    /// <param name="userIdentifier">An identifier which is unique for the current user.</param>
    public void IdentifyUser(string userIdentifier)
    {
        this.UserId = userIdentifier;

#if UNITY_EDITOR
        Debug.Log("[Teak] IdentifyUser(): " + userIdentifier);
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("identifyUser", userIdentifier);
#elif UNITY_IPHONE
        TeakIdentifyUser(userIdentifier);
#endif
    }

    /// <summary>
    /// Track an arbitrary event in Teak.
    /// </summary>
    /// <param name="actionId">The identifier for the action, e.g. 'complete'.</param>
    /// <param name="objectTypeId">The type of object that is being posted, e.g. 'quest'.</param>
    /// <param name="objectInstanceId">The specific instance of the object, e.g. 'gather-quest-1'</param>
    public void TrackEvent(string actionId, string objectTypeId, string objectInstanceId)
    {
#if UNITY_EDITOR
        Debug.Log("[Teak] TrackEvent(): " + actionId + " - " + objectTypeId + " - " + objectInstanceId);
#elif UNITY_ANDROID
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("trackEvent", actionId, objectTypeId, objectInstanceId);
#elif UNITY_IPHONE
        TeakTrackEvent(actionId, objectTypeId, objectInstanceId);
#endif
    }

    /// <summary>
    /// Delegate used by OnLaunchedFromNotification and OnReward
    /// </summary>
    /// <param name="parameters">A Dictionary containing reward or other information sent with the event.</param>
    public delegate void TeakEventListener(Dictionary<string, object> parameters);

    /// <summary>
    /// An event which gets fired when the app is launched via a push notification.
    /// </summary>
    public event TeakEventListener OnLaunchedFromNotification;

    /// <summary>
    /// An event which gets fired when a Teak Reward has been processed (successfully or unsuccessfully).
    /// </summary>
    public event TeakEventListener OnReward;

    /// <summary>
    /// Method used to register a deep link route.
    /// </summary>
    public void RegisterRoute(string route, string name, string description, Action<Dictionary<string, object>> action)
    {
        mDeepLinkRoutes[route] = action;
#if UNITY_EDITOR
        Debug.Log("[Teak] RegisterRoute(): " + route + " - " + name + " - " + description);
#elif UNITY_ANDROID
        AndroidJavaClass teakUnity = new AndroidJavaClass("io.teak.sdk.Unity");
        teakUnity.CallStatic("registerRoute", route, name, description);
#elif UNITY_IPHONE
        TeakUnityRegisterRoute(route, name, description);
#endif
    }

    /// <summary>
    /// Feed Post
    /// </summary>
    public void FeedPost(string objectInstanceId, Dictionary<string, object> objectProperties = null, Action<Dictionary<string, object>> callback = null)
    {
        string callbackId = System.DateTime.Now.ToString();
        if(callback != null)
        {
            mFeedPostCallbacks[callbackId] = callback;
        }

#if UNITY_EDITOR
        Debug.Log("[Teak] PopupFeedPost");
#elif UNITY_ANDROID
        AndroidJavaClass teakUnity = new AndroidJavaClass("io.teak.sdk.Unity");
        teakUnity.CallStatic("popupFeedPost", objectInstanceId, Json.Serialize(objectProperties), callbackId);
#elif UNITY_IPHONE

#endif
    }

    /// @cond hide_from_doxygen
    private static Teak mInstance;
    Dictionary<string, Action<Dictionary<string, object>>> mDeepLinkRoutes = new Dictionary<string, Action<Dictionary<string, object>>>();
    Dictionary<string, Action<Dictionary<string, object>>> mFeedPostCallbacks = new Dictionary<string, Action<Dictionary<string, object>>>();
    /// @endcond

    /// @cond hide_from_doxygen
#if UNITY_ANDROID
    private void Prime31PurchaseSucceded<T>(T purchase)
    {
        PropertyInfo originalJson = purchase.GetType().GetProperty("originalJson");
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("prime31PurchaseSucceeded", originalJson.GetValue(purchase, null));
    }

    private void Prime31PurchaseFailed(string error, int errorCode)
    {
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("pluginPurchaseFailed", errorCode);
    }

    private void OpenIABPurchaseSucceded<T>(T purchase)
    {
        MethodInfo serialize = purchase.GetType().GetMethod("Serialize");
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("openIABPurchaseSucceeded", serialize.Invoke(purchase, null));
    }

    private void OpenIABPurchaseFailed(int errorCode, string error)
    {
        AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
        teak.CallStatic("pluginPurchaseFailed", errorCode);
    }

#elif UNITY_IPHONE
    [DllImport ("__Internal")]
    private static extern void TeakIdentifyUser(string userId);

    [DllImport ("__Internal")]
    private static extern void TeakTrackEvent(string actionId, string objectTypeId, string objectInstanceId);

    [DllImport ("__Internal")]
    private static extern void TeakUnityRegisterRoute(string route, string name, string description);

    [DllImport ("__Internal")]
    private static extern void TeakUnityReadyForDeepLinks();
#endif
    /// @endcond

    #region UnitySendMessage
    /// @cond hide_from_doxygen
    void ShowFacebookShareDialog(string jsonString)
    {
        if(FB.IsLoggedIn) // TODO: Reflection?
        {
            Dictionary<string, object> json = Json.Deserialize(jsonString) as Dictionary<string, object>;
            Dictionary<string, object> fb_data = json["fb_data"] as Dictionary<string, object>;
            FB.Feed("" /* toID */, fb_data["link"] as string, fb_data["name"] as string, fb_data["caption"] as string, fb_data["description"] as string, fb_data["picture"] as string,
                "" /* mediaSource */, "" /* actionName */, "" /* actionLink */, "" /* reference */, null /* properties */,
                (FBResult result) => {
                    if (result.Error != null)
                    {
                        Debug.LogError(result.Error);
                    }

#if UNITY_EDITOR
    // Nothing currently
#elif UNITY_ANDROID
                    AndroidJavaClass teakUnity = new AndroidJavaClass("io.teak.sdk.Unity");
                    teakUnity.CallStatic("facebookWrapperCallback", json["callbackId"] as string, result.Text);
#elif UNITY_IPHONE
    // TODO
#endif
                });
        }
    }

    void PopupFeedPostCallback(string jsonString)
    {
        Dictionary<string, object> json = Json.Deserialize(jsonString) as Dictionary<string, object>;
        string callbackId = json["callbackId"] as string;

        if (mFeedPostCallbacks.ContainsKey(callbackId))
        {
            mFeedPostCallbacks[callbackId](json);
            mFeedPostCallbacks.Remove(callbackId);
        }
    }

    void MakeGraphFeedPost(string jsonString)
    {
        Dictionary<string, object> json = Json.Deserialize(jsonString) as Dictionary<string, object>;
        Dictionary<string, object> fb_data_json = json["fb_data"] as Dictionary<string, object>;

        // Delete 'method' attribute to not overwrite HTTP method
        fb_data_json.Remove("method");

        WWWForm fb_data = new WWWForm();
        foreach(KeyValuePair<string, object> entry in fb_data_json)
        {
            fb_data.AddField(entry.Key, entry.Value.ToString());
        }

        FB.API("/me/feed", Facebook.HttpMethod.POST, (FBResult result) => {
#if UNITY_EDITOR
    // Nothing currently
#elif UNITY_ANDROID
            AndroidJavaClass teakUnity = new AndroidJavaClass("io.teak.sdk.Unity");
            teakUnity.CallStatic("facebookWrapperCallback", json["callbackId"] as string, result.Text);
#elif UNITY_IPHONE
    // TODO
#endif
        }, fb_data);
    }

    void NotificationLaunch(string jsonString)
    {
        Dictionary<string, object> json = Json.Deserialize(jsonString) as Dictionary<string, object>;
        json.Remove("teakReward");
        json.Remove("teakDeepLink");
        OnLaunchedFromNotification(json);
    }

    void RewardClaimAttempt(string jsonString)
    {
        Dictionary<string, object> json = Json.Deserialize(jsonString) as Dictionary<string, object>;
        OnReward(json);
    }

    void DeepLink(string jsonString)
    {
        Dictionary<string, object> json = Json.Deserialize(jsonString) as Dictionary<string, object>;
        string route = json["route"] as string;
        if (mDeepLinkRoutes.ContainsKey(route))
        {
            try
            {
                mDeepLinkRoutes[route](json["parameters"] as Dictionary<string, object>);
            }
            catch(Exception e)
            {
                Debug.LogError("[Teak] Error executing Action for route: " + route + "\n" + e.Message);
            }
        }
        else
        {
            Debug.LogError("[Teak] Unable to find Action for route: " + route);
        }
    }
    /// @endcond
    #endregion

    #region MonoBehaviour
    /// @cond hide_from_doxygen
    void Awake()
    {
        Debug.Log("[Teak] Unity SDK Version: " + Teak.Version);
        DontDestroyOnLoad(this);
    }

    void Start()
    {
#if UNITY_EDITOR
        // Nothing currently
#elif UNITY_ANDROID
        AndroidJavaClass teakUnity = new AndroidJavaClass("io.teak.sdk.Unity");
        teakUnity.CallStatic("readyForDeepLinks");
#elif UNITY_IPHONE
        TeakUnityReadyForDeepLinks();
#endif

#if UNITY_ANDROID
        // Try and find an active store plugin
        if(Type.GetType("OpenIABEventManager, Assembly-CSharp-firstpass") != null)
        {
            Debug.Log("[Teak] Found OpenIAB, adding event handlers.");
            Type onePF = Type.GetType("OpenIABEventManager, Assembly-CSharp-firstpass");
            EventInfo successEvent = onePF.GetEvent("purchaseSucceededEvent");
            EventInfo failEvent = onePF.GetEvent("purchaseFailedEvent");

            Type purchase = Type.GetType("OnePF.Purchase, Assembly-CSharp-firstpass");
            MethodInfo magic = GetType().GetMethod("OpenIABPurchaseSucceded", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(purchase);
            successEvent.AddEventHandler(null, Delegate.CreateDelegate(successEvent.EventHandlerType, this, magic));
            failEvent.AddEventHandler(null, Delegate.CreateDelegate(failEvent.EventHandlerType, this, "OpenIABPurchaseFailed"));
        }
        else if(Type.GetType("Prime31.GoogleIABManager, Assembly-CSharp-firstpass") != null)
        {
            Debug.Log("[Teak] Found Prime31, adding event handlers.");
            Type prime31 = Type.GetType("Prime31.GoogleIABManager, Assembly-CSharp-firstpass");

            EventInfo successEvent = prime31.GetEvent("purchaseSucceededEvent");
            EventInfo failEvent = prime31.GetEvent("purchaseFailedEvent");

            Type purchase = Type.GetType("Prime31.GooglePurchase, Assembly-CSharp-firstpass");
            MethodInfo magic = GetType().GetMethod("Prime31PurchaseSucceded", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(purchase);
            successEvent.AddEventHandler(null, Delegate.CreateDelegate(successEvent.EventHandlerType, this, magic));
            failEvent.AddEventHandler(null, Delegate.CreateDelegate(failEvent.EventHandlerType, this, "Prime31PurchaseFailed"));
        }
        else
        {
            Debug.LogWarning("[Teak] No known store plugin found.");
        }
#endif
    }

    void OnApplicationQuit()
    {
        Destroy(this);
    }
    /// @endcond
    #endregion
}