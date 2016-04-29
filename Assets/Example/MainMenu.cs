using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    public int buttonHeight = 60;
    public int buttonWidth = 200;
    public int buttonSpacing = 8;

#if UNITY_IOS
    string pushTokenString = null;
#endif
    string teakUserId = null;

    void Start()
    {
        teakUserId = SystemInfo.deviceUniqueIdentifier;

#if UNITY_EDITOR
        Teak.Instance.NavigateToDeepLink();
#else
        FB.Init(() => {
            Debug.Log("Facebook initialized.");
            Teak.Instance.NavigateToDeepLink();
        });
#endif
        Teak.Instance.IdentifyUser(teakUserId);
        Teak.Instance.OnLaunchedFromNotification += OnLaunchedFromNotification;
        Teak.Instance.TrackEvent("foo", "bar", "baz");
    }

    [TeakLink("/store/:page/:sku")]
    void OpenIAPStore(string sku, string page)
    {
        Debug.Log("OpenIAPStore called with sku: " + sku);
    }

    [TeakLink("/iap/:foo/:bar/:baz")]
    void TestDictionary(Dictionary<string, object> parameters)
    {
        Debug.Log("TestDictionary called...");
        foreach(var kvp in parameters)
        {
            Debug.Log(kvp.Key + " = " + kvp.Value);
        }
    }

    [TeakLink("/mixed/:foo")]
    void TestMixed(string foo, Dictionary<string, object> parameters)
    {
        Debug.Log("TestDictionary called with foo: " + foo + " and parameters...");
        foreach(var kvp in parameters)
        {
            Debug.Log(kvp.Key + " = " + kvp.Value);
        }
    }

    void OnLaunchedFromNotification(TeakNotification notif)
    {
        Debug.Log("Launched from Teak Notification: " + notif);
        if(notif.HasReward)
        {
            StartCoroutine(notif.ConsumeNotification((TeakNotification.Reward reward) =>{
                Debug.Log("Got Reward, status: " + reward.Status);
                if(reward.Status == TeakNotification.Reward.RewardStatus.GrantReward)
                {
                    Debug.Log("Reward JSON: " + reward.RewardJson);
                }
            }));
        }
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

        // Display auth status
        GUILayout.Label(teakUserId);

        // FB Login
        if(FB.IsLoggedIn)
        {
            GUILayout.Label("Facebook UserId: " + FB.UserId);
        }
        else
        {
            if(GUILayout.Button("Login With Facebook", GUILayout.Height(buttonHeight)))
            {
                FB.Login("public_profile,email,user_friends");
            }
        }

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

        GUILayout.EndArea();
    }
}
