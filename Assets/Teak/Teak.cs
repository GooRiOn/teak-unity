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
using System.Reflection;
using System.Runtime.InteropServices;
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
            }
            return mInstance;
        }
    }

    /// <summary>Teak SDK version.</summary>
    public static readonly string SDKVersion = "2.0.1";

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

    public delegate void LaunchedFromNotification(TeakNotification notif);
    public event LaunchedFromNotification OnLaunchedFromNotification;

#if UNITY_ANDROID
    /// @cond hide_from_doxygen
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
    private static extern IntPtr TeakLaunchedFromTeakNotifId();
    /// @endcond
#endif

    #region MonoBehaviour
    /// @cond hide_from_doxygen
    void Start()
    {
        DontDestroyOnLoad(this);

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

#if UNITY_EDITOR
    if(TeakSettings.SimulateOpenedWithPush)
    {
        TeakNotification notif = TeakNotification.FromTeakNotifId("");
        if(notif != null)
        {
            // Send event
            OnLaunchedFromNotification(notif);
        }
    }
#endif
    }

#if UNITY_IPHONE || UNITY_ANDROID
    void OnApplicationPause(bool isPaused)
    {
        if(isPaused)
        {
            // Pause
        }
        else
        {
            // Resume
#if UNITY_EDITOR
            string launchedFromTeakNotifId = null;
#elif UNITY_ANDROID
            AndroidJavaClass teak = new AndroidJavaClass("io.teak.sdk.Teak");
            string launchedFromTeakNotifId = teak.GetStatic<string>("launchedFromTeakNotifId");
#elif UNITY_IPHONE
            string launchedFromTeakNotifId = Marshal.PtrToStringAnsi(TeakLaunchedFromTeakNotifId());
#endif
            if(launchedFromTeakNotifId != null)
            {
                TeakNotification notif = TeakNotification.FromTeakNotifId(launchedFromTeakNotifId);
                if(notif != null)
                {
                    // Send event
                    OnLaunchedFromNotification(notif);
                }
            }
        }
    }
#endif

    void OnApplicationQuit()
    {
        Destroy(this);
    }
    /// @endcond
    #endregion
}