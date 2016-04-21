using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    public int buttonHeight = 60;
    public int buttonWidth = 200;
    public int buttonSpacing = 8;

    string teakUserId = "ffffffff-f71d-e60e-08d4-8f760033c587";

    void Start()
    {
        FB.Init(() => {
            Debug.Log("Facebook initialized");
        });

        Teak.Instance.IdentifyUser(teakUserId);
        Teak.Instance.OnLaunchedFromNotification += OnLaunchedFromNotification;
        Teak.Instance.TrackEvent("foo", "bar", "baz");
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
/*
        // High Score
        GUILayout.Space(buttonSpacing);
        scoreString = GUILayout.TextField(scoreString, buttonWidth);
        if(GUILayout.Button("Earn High Score", GUILayout.Height(buttonHeight)))
        {
            Teak.Instance.postHighScore(System.Convert.ToUInt32(scoreString));
        }
*/
        GUILayout.EndArea();
    }
}
