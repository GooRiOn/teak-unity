using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    public int buttonHeight = 60;
    public int buttonWidth = 200;
    public int buttonSpacing = 8;

    string scoreString = "100";
    string achieveString = "achievement_id";
    string authStatus = "";
    string actionString = "action_id";
    string objectString = "object_id";

    void Start()
    {
        FB.Init(() => {
            Debug.Log("Facebook initialized");
        });
        authStatus = Teak.authStatusString(Teak.AuthStatus.Undetermined);

        Teak.AuthenticationStatusChanged += (object sender, Teak.AuthStatus status) => {
            authStatus = Teak.authStatusString(status);
        };

        Teak.Instance.UserId = "zerostride@gmail.com";
        Teak.Instance.validateUser("532815528");
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

        // Display auth status
        GUILayout.Label(authStatus);

        // High Score
        GUILayout.Space(buttonSpacing);
        scoreString = GUILayout.TextField(scoreString, buttonWidth);
        if(GUILayout.Button("Earn High Score", GUILayout.Height(buttonHeight)))
        {
            Teak.Instance.postHighScore(System.Convert.ToUInt32(scoreString));
        }

        // Achievement
        GUILayout.Space(buttonSpacing);
        achieveString = GUILayout.TextField(achieveString, buttonWidth);
        if(GUILayout.Button("Earn Achievement", GUILayout.Height(buttonHeight)))
        {
            Teak.Instance.postAchievement(achieveString);
        }

        // Action Post
        GUILayout.Space(buttonSpacing);
        actionString = GUILayout.TextField(actionString, buttonWidth);
        objectString = GUILayout.TextField(objectString, buttonWidth);
        if(GUILayout.Button("Post Object/Action", GUILayout.Height(buttonHeight)))
        {
            //Teak.Instance.postAction(actionString, objectString);
            Teak.Instance.popupFeedPost(objectString);
        }

        // Dynamic Action Post
        GUILayout.Space(buttonSpacing);
        if(GUILayout.Button("Post Screenshot", GUILayout.Height(buttonHeight)))
        {
            StartCoroutine(postScreenshotCoroutine());
        }

        GUILayout.EndArea();
    }

    IEnumerator postScreenshotCoroutine()
    {
        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false );
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        Teak.Instance.postAction("demo", "template", new Dictionary<string, object>() {
            {"title", "Test Title"},
            {"description", "Test Description"},
            //{"image", tex}
        });
        Destroy(tex);
    }
}
