using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using MiniJSON.Teak;

public class MainMenu : MonoBehaviour
{
    public int buttonHeight = 150;

#if UNITY_IOS
    string pushTokenString = null;
#endif
    string teakUserId = null;
    string teakSdkVersion = null;
    string teakDeepLinkLaunch = null;
    string teakScheduledNotification = null;

    void Awake()
    {
        Teak.Instance.RegisterRoute("/store/:sku", "Store", "Open the store to an SKU", (Dictionary<string, object> parameters) => {
            Debug.Log("Got store deep link: " + Json.Serialize(parameters));
        });
    }

    void Start()
    {
        teakUserId = SystemInfo.deviceUniqueIdentifier;
        teakSdkVersion = "Teak SDK Version: " + Teak.Version;

#if !UNITY_EDITOR
        FB.Init(() => {
            Debug.Log("Facebook initialized.");
        });
#endif

        Teak.Instance.IdentifyUser(teakUserId);

        Teak.Instance.OnLaunchedFromNotification += OnLaunchedFromNotification;
        Teak.Instance.OnReward += OnReward;
    }

    void OnApplicationPause(bool isPaused)
    {
        if(isPaused)
        {
            // Pause
        }
        else
        {
            // Resume
        }
    }

    void OnLaunchedFromNotification(Dictionary<string, object> notificationPayload)
    {
        Debug.Log("OnLaunchedFromNotification: " + Json.Serialize(notificationPayload));
    }

    void OnReward(Dictionary<string, object> notificationPayload)
    {
        Debug.Log("OnReward: " + Json.Serialize(notificationPayload));
    }

#if UNITY_IOS
    void FixedUpdate()
    {
        if(pushTokenString == null)
        {
            byte[] token = NotificationServices.deviceToken;
            if(token != null)
            {
                // Teak will take care of storing this automatically
                pushTokenString = System.BitConverter.ToString(token).Replace("-", "").ToLower();
            }
        }
    }
#endif

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

        GUILayout.Label(teakSdkVersion);
        GUILayout.Label(teakUserId);
        GUILayout.Label(teakDeepLinkLaunch);

#if UNITY_IOS
        if(pushTokenString != null)
        {
            GUILayout.Label("Push Token: " + pushTokenString);
        }
        else
        {
            if(GUILayout.Button("Request Push Notifications", GUILayout.Height(buttonHeight)))
            {
                NotificationServices.RegisterForRemoteNotificationTypes(RemoteNotificationType.Alert |  RemoteNotificationType.Badge |  RemoteNotificationType.Sound);
            }
        }
#endif

#if !UNITY_EDITOR
        if(FB.IsLoggedIn)
        {
            GUILayout.Label("Facebook UserId: " + FB.UserId);
            if(GUILayout.Button("Feed Post", GUILayout.Height(buttonHeight)))
            {
                Teak.Instance.FeedPost("test_post", null, (Dictionary<string,object> responses) => {
                    Debug.Log(Json.Serialize(responses));
                });
            }
        }
        else
        {
            if(GUILayout.Button("Login With Facebook", GUILayout.Height(buttonHeight)))
            {
                FB.Login("public_profile,email,user_friends,publish_actions");
            }
        }
#endif

        if(teakScheduledNotification == null)
        {
            if(GUILayout.Button("Schedule Notification", GUILayout.Height(buttonHeight)))
            {
                StartCoroutine(TeakNotification.ScheduleNotification("test", "Test notification", 10, (string scheduleId) => {
                    teakScheduledNotification = scheduleId;
                }));
            }
        }
        else
        {
            if(GUILayout.Button("Cancel Notification " + teakScheduledNotification, GUILayout.Height(buttonHeight)))
            {
                StartCoroutine(TeakNotification.CancelScheduledNotification(teakScheduledNotification, (string scheduleId) => {
                    teakScheduledNotification = null;
                }));
            }
        }

        GUILayout.EndArea();
    }
}
