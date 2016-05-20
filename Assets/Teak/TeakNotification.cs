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
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;

using TeakEditor.MiniJSON;
#endif
#endregion

/// <summary>
/// Interface for manipulating notifications from Teak.
/// </summary>
public partial class TeakNotification
{
    public string TeakNotifId
    {
        get { return mTeakNotifId; }
    }

    public bool HasReward
    {
        get
        {
#if UNITY_EDITOR
            return (TeakSettings.SimulateRewardReply ? true : !String.IsNullOrEmpty(TeakSettings.SimulatedTeakRewardId));
#elif UNITY_ANDROID
            return mTeakNotification.Call<bool>("hasReward");
#elif UNITY_IOS
            return TeakNotificationHasReward(mTeakNotification);
#endif
        }
    }

    public Dictionary<string, object> UserData
    {
        get
        {
            // TODO: Native->C# user data conversion
#if UNITY_EDITOR
            return new Dictionary<string, object>();
#elif UNITY_ANDROID
            return new Dictionary<string, object>();
#elif UNITY_IOS
            return new Dictionary<string, object>();
#endif
        }
    }

    public IEnumerator ConsumeNotification(System.Action<Reward> callback)
    {
        Reward ret = null;
#if UNITY_EDITOR
        if(TeakSettings.SimulateRewardReply)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["status"] = TeakSettings.SimulatedTeakRewardStatus;
            dict["json"] = TeakSettings.SimulatedTeakRewardJson;
            ret = new Reward(dict);
            yield return null;
        }
        else
        {
            string hostname = "rewards.gocarrot.com";
            string endpoint = String.Format("/{0}/clicks", TeakSettings.SimulatedTeakRewardId);
            Dictionary<string, object> urlParams  = new Dictionary<string, object> {
                {"clicking_user_id", Teak.Instance.UserId},
                {"no_status_code", true}
            };
            string sig = Teak.signParams(hostname, endpoint, TeakSettings.APIKey, urlParams);

            // Use System.Net.WebRequest due to crossdomain.xml bug in Unity Editor mode
            string postData = String.Format("clicking_user_id={0}&no_status_code=true&sig={1}",
                WWW.EscapeURL(Teak.Instance.UserId),
                WWW.EscapeURL(sig));
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            WebRequest request = WebRequest.Create(String.Format("https://{0}{1}", hostname, endpoint));
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            IAsyncResult asyncResult = request.BeginGetResponse((IAsyncResult result) => {
                HttpWebResponse response = (result.AsyncState as HttpWebRequest).EndGetResponse(result) as HttpWebResponse;

                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();

                Dictionary<string, object> reply = null;
                reply = Json.Deserialize(responseFromServer) as Dictionary<string, object>;
                
                Dictionary<string, object> responseJson = reply["response"] as Dictionary<string, object>;

                Dictionary<string, object> rewardDict = new Dictionary<string, object>();
                switch(responseJson["status"] as string)
                {
                    case "grant_reward": rewardDict["status"] = Reward.RewardStatus.GrantReward; break;
                    case "self_click": rewardDict["status"] = Reward.RewardStatus.SelfGrant; break;
                    case "already_clicked": rewardDict["status"] = Reward.RewardStatus.AlreadyGranted; break;
                    case "too_many_clicks": rewardDict["status"] = Reward.RewardStatus.TooManyGrants; break;
                    case "exceed_max_clicks_for_day": rewardDict["status"] = Reward.RewardStatus.ExceedMaxGrantsForDay; break;
                    case "expired": rewardDict["status"] = Reward.RewardStatus.Expired; break;
                    case "invalid_post": rewardDict["status"] = Reward.RewardStatus.Invalid; break;
                    default: rewardDict["status"] = Reward.RewardStatus.Unknown; break;
                }

                if((Reward.RewardStatus)rewardDict["status"] == Reward.RewardStatus.GrantReward)
                {
                    rewardDict["json"] = responseJson["reward"] as string;
                }
                ret = new Reward(rewardDict);
            }, request);

            while(!asyncResult.IsCompleted) yield return null;
        }
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
        }
        private Dictionary<string, object> mReward;
#elif UNITY_ANDROID
        internal Reward(AndroidJavaObject reward)
        {
            mReward = reward;
        }
        private AndroidJavaObject mReward;
#elif UNITY_IOS
        internal Reward(IntPtr reward)
        {
            mReward = reward;
        }
        private IntPtr mReward;
#endif
    }

    // Returns an id that can be used to cancel a scheduled notification
    public static string ScheduleNotification(string creativeId, string defaultMessage, long delayInSeconds)
    {
        return "temporary-code-notification-id";
    }

    // Cancel an existing notification
    public static bool CancelScheduledNotification(string scheduleId)
    {
        return true;
    }

    /// @cond hide_from_doxygen
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

        ret.mTeakNotifId = teakNotifId;

        return ret;
    }

    public override string ToString()
    {
        // Funny space formatting since the Unity editor doesn't use a fixed width font
        return string.Format(
@"TeakNotification {{
    TeakNotifId : '{0}',
    HasReward  : '{1}',
    UserData     : '{2}'
}}",
            this.TeakNotifId, this.HasReward, this.UserData);
    }

    string mTeakNotifId;

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
    /// @endcond
}