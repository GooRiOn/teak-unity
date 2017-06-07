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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using System.IO;
using System.Net;
using System.Text;
#endif
#endregion

/// <summary>
/// Interface for manipulating notifications from Teak.
/// </summary>
public partial class TeakNotification
{
    // Returns an id that can be used to cancel a scheduled notification
    public static IEnumerator ScheduleNotification(string creativeId, string defaultMessage, long delayInSeconds, System.Action<string> callback)
    {
        string ret = null;
#if UNITY_EDITOR
        yield return null;
#elif UNITY_ANDROID
        AndroidJavaClass teakNotification = new AndroidJavaClass("io.teak.sdk.TeakNotification");
        AndroidJavaObject future = teakNotification.CallStatic<AndroidJavaObject>("scheduleNotification", creativeId, defaultMessage, delayInSeconds);
        if(future != null)
        {
            while(!future.Call<bool>("isDone")) yield return null;
            ret = future.Call<string>("get");
        }
#elif UNITY_IPHONE
        IntPtr notif = TeakNotificationSchedule(creativeId, defaultMessage, delayInSeconds);
        if(notif != IntPtr.Zero)
        {
            while(!TeakNotificationIsCompleted(notif)) yield return null;
            ret = Marshal.PtrToStringAnsi(TeakNotificationGetTeakNotifId(notif));
        }
#endif
        callback(string.IsNullOrEmpty(ret) ? null : ret);
    }

    // Cancel an existing notification
    public static IEnumerator CancelScheduledNotification(string scheduleId, System.Action<string> callback)
    {
        string ret = null;
#if UNITY_EDITOR
        yield return null;
#elif UNITY_ANDROID
        AndroidJavaClass teakNotification = new AndroidJavaClass("io.teak.sdk.TeakNotification");
        AndroidJavaObject future = teakNotification.CallStatic<AndroidJavaObject>("cancelNotification", scheduleId);
        if(future != null)
        {
            while(!future.Call<bool>("isDone")) yield return null;
            ret = future.Call<string>("get");
        }
#elif UNITY_IPHONE
        IntPtr notif = TeakNotificationCancel(scheduleId);
        if(notif != IntPtr.Zero)
        {
            while(!TeakNotificationIsCompleted(notif)) yield return null;
            ret = Marshal.PtrToStringAnsi(TeakNotificationGetTeakNotifId(notif));
        }
#endif
        callback(string.IsNullOrEmpty(ret) ? null : ret);
    }

    /// @cond hide_from_doxygen
#if UNITY_IOS
    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationSchedule(string creativeId, string message, long delay);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationCancel(string scheduleId);

    [DllImport ("__Internal")]
    private static extern bool TeakNotificationIsCompleted(IntPtr notif);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationGetTeakNotifId(IntPtr notif);
#endif
    /// @endcond
}