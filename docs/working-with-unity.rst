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
        switch (rewardPayload["status"] as string) {
            case "grant_reward": {
                // The user has been issued this reward by Teak
                Dictionary<string, object> rewards = rewardPayload["reward"] as Dictionary<string, object>;
                foreach(KeyValuePair<string, object> entry in rewards)
                {
                    Debug.Log("OnReward -- Give the user " + entry.Value + " instances of " + entry.Key);
                }
            }
            break;

            case "self_click": {
                // The user has attempted to claim a reward from their own social post
            }
            break;

            case "already_clicked": {
                // The user has already been issued this reward
            }
            break;

            case "too_many_clicks": {
                // The reward has already been claimed its maximum number of times globally
            }
            break;

            case "exceed_max_clicks_for_day": {
                // The user has already claimed their maximum number of rewards of this type for the day
            }
            break;

            case "expired": {
                // This reward has expired and is no longer valid
            }
            break;

            case "invalid_post": {
                //Teak does not recognize this reward id
            }
            break;
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
