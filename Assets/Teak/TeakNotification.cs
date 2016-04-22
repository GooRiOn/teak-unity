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
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using System.Collections.Generic;
#endif
#endregion

/// <summary>
/// Interface for manipulating notifications from Teak.
/// </summary>
public class TeakNotification
{
    public static TeakNotification FromTeakNotifId(string teakNotifId)
    {
        TeakNotification ret = null;
#if UNITY_EDITOR
        ret = new TeakNotification();
#elif UNITY_ANDROID
        AndroidJavaClass teakNotification = new AndroidJavaClass("io.teak.sdk.TeakNotification");
        AndroidJavaObject notif = teakNotification.CallStatic<AndroidJavaObject>("byTeakNotifId", teakNotifId);

        if(notif != null)
        {
            ret = new TeakNotification(notif);
        }
#elif UNITY_IOS
        IntPtr notif = TeakNotificationFromTeakNotifId(teakNotifId);
        if(notif != IntPtr.Zero)
        {
            ret = new TeakNotification(notif);
        }
#endif

        return ret;
    }

    public bool HasReward
    {
        get
        {
#if UNITY_EDITOR
            return !String.IsNullOrEmpty(TeakSettings.SimulatedTeakRewardId);
#elif UNITY_ANDROID
            return mTeakNotification.Call<bool>("hasReward");
#elif UNITY_IOS
            return TeakNotificationHasReward(mTeakNotification);
#endif
        }
    }

    public IEnumerator ConsumeNotification(System.Action<Reward> callback)
    {
        Reward ret = null;
#if UNITY_EDITOR
        // TODO
        yield return null;
#elif UNITY_ANDROID
        AndroidJavaObject rewardFuture = mTeakNotification.Call<AndroidJavaObject>("consumeNotification");
        if(rewardFuture != null)
        {
            while(!rewardFuture.Call<bool>("isDone")) yield return null;
            ret = new Reward(rewardFuture.Call<AndroidJavaObject>("get"));
        }
#elif UNITY_IOS
        IntPtr reward = TeakNotificationConsume(mTeakNotification);
        if(reward != IntPtr.Zero)
        {
            while(!TeakRewardIsCompleted(reward)) yield return null;
            ret = new Reward(reward);
        }
#endif
        callback(ret);
    }

    public class Reward
    {
        public enum RewardStatus : int
        {
            /** An unknown error occured while processing the reward. */
            Unknown = 1,

            /** Valid reward claim, grant the user the reward. */
            GrantReward = 0,

            /** The user has attempted to claim a reward from their own social post. */
            SelfGrant = -1,

            /** The user has already been issued this reward. */
            AlreadyGranted = -2,

            /** The reward has already been claimed its maximum number of times globally. */
            TooManyGrants = -3,

            /** The user has already claimed their maximum number of rewards of this type for the day. */
            ExceedMaxGrantsForDay = -4,

            /** This reward has expired and is no longer valid. */
            Expired = -5,

            /** Teak does not recognize this reward id. */
            Invalid = -6
        }

        public RewardStatus Status
        {
            get
            {
#if UNITY_EDITOR
                return (RewardStatus)mReward["status"];
#elif UNITY_ANDROID
                return (RewardStatus)mReward.Get<int>("status");
#elif UNITY_IOS
                return (RewardStatus)TeakRewardGetStatus(mReward);
#endif
            }
        }

        public string RewardJson
        {
            get
            {
#if UNITY_EDITOR
                return mReward["json"] as string;
#elif UNITY_ANDROID
                AndroidJavaObject reward = mReward.Get<AndroidJavaObject>("reward");
                if(reward != null)
                {
                    return reward.Call<string>("toString");
                }
                return null;
#elif UNITY_IOS
                return Marshal.PtrToStringAnsi(TeakRewardGetJson(mReward));
#endif
            }
        }

#if UNITY_EDITOR
        internal Reward(Dictionary<string, object> reward)
        {
            mReward = reward;
            Debug.Log("There's a reward that got made from magic! Status: " + this.Status);
        }
        private Dictionary<string, object> mReward;
#elif UNITY_ANDROID
        internal Reward(AndroidJavaObject reward)
        {
            mReward = reward;
            Debug.Log("There's a reward that got made from Java! Status: " + this.Status);
        }
        private AndroidJavaObject mReward;
#elif UNITY_IOS
        internal Reward(IntPtr reward)
        {
            mReward = reward;
            Debug.Log("There's a reward that got made from Obj-C! Status: " + this.Status);
        }
        private IntPtr mReward;
#endif
    }
#if UNITY_EDITOR
    private TeakNotification()
    {
    }
#elif UNITY_ANDROID
    private TeakNotification(AndroidJavaObject teakNotification)
    {
        mTeakNotification = teakNotification;
    }

    AndroidJavaObject mTeakNotification;
#elif UNITY_IOS
    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationFromTeakNotifId(string teakNotifId);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakNotificationConsume(IntPtr notif);

    [DllImport ("__Internal")]
    private static extern bool TeakNotificationHasReward(IntPtr notif);

    [DllImport ("__Internal")]
    private static extern bool TeakRewardIsCompleted(IntPtr reward);

    [DllImport ("__Internal")]
    private static extern int TeakRewardGetStatus(IntPtr reward);

    [DllImport ("__Internal")]
    private static extern IntPtr TeakRewardGetJson(IntPtr reward);

    private TeakNotification(IntPtr teakNotification)
    {
        mTeakNotification = teakNotification;
    }
    IntPtr mTeakNotification;
#endif
}