Working with Notifications, Rewards and Deep Links inside Unity
====================================================================
.. highlight:: csharp
Push Notifications and Local Notifications
------------------------------------------
Whenever your game is launched via a push notification, or local notification Teak will let you know by sending out an event to all listeners.

You can listen for that event during by first writing a listener function, for example::

    void MyOnLaunchedFromNotificationListener(Dictionary<string, object> notificationPayload)
    {
        // notificationPayload will usually be empty, but can contain custom information
        // specified by your notification campaigns
        Debug.Log("OnLaunchedFromNotification: " + Json.Serialize(notificationPayload));
    }

And then adding it to the ``Teak.Instance.OnLaunchedFromNotification`` event during ``Start()`` in any ``MonoBehaviour``::

    void Start()
    {
        Teak.Instance.OnLaunchedFromNotification += MyOnLaunchedFromNotificationListener;
    }

Rewards
-------
Whenever your game should grant a reward to a user Teak will let you know by sending out an event to all listeners.

You can listen for that event during by first writing a listener function, for example::

    void MyRewardListener(Dictionary<string, object> rewardPayload)
    {
        // Check to make sure the status is 'grant_reward', a list of other possible status
        // and the meaning is located at:
        // https://teak.io/docs/claiming_rewards/
        if (rewardPayload["status"] as string == "grant_reward")
        {
            Dictionary<string, object> rewards = rewardPayload["reward"] as Dictionary<string, object>;
            foreach(KeyValuePair<string, object> entry in rewards)
            {
                Debug.Log("OnReward -- Give the user " + entry.Value + " instances of " + entry.Key);
            }
        }
    }

And then adding it to the ``Teak.Instance.OnReward`` event during ``Start()`` in any ``MonoBehaviour``::

    void Start()
    {
        Teak.Instance.OnReward += MyRewardListener;
    }

Deep Links
----------

Adding deep link targets in your game is easy with Teak.

You can add routes during the ``Awake()`` function of any ``MonoBehaviour``. For example::

    void Awake()
    {
        Teak.Instance.RegisterRoute("/store/:sku", "Store", "Open the store to an SKU", (Dictionary<string, object> parameters) => {
            // Any URL query parameters, or path parameters will be contained in the dictionary
            Debug.Log("Open the store to this sku - " + parameters["sku"]);
        });
    }

.. The route system that Teak uses is very flexible, let's look at a slightly more complicated example.

.. What if we wanted to make a deep link which opened the game to a specific slot machine.
