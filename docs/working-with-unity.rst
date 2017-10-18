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

Working with Local Notifications
--------------------------------
You can use Teak to schedule notifications for the future.

.. note:: You get the full benefit of Teak's analytics, A/B testing, and Content Management System.

.. note:: All local notification related methods are coroutines. You may need to wrap calls to them in StartCoroutine()

Scheduling a Local Notification
^^^^^^^^^^^^^^^^^^^^^^^^^
To schedule a notification from your game, simply use::

    IEnumerator TeakNotification.ScheduleNotification(string creativeId, string defaultMessage, long delayInSeconds, System.Action<string, string> callback)

Parameters
    ``creativeId`` - A value used to identify the message creative in the Teak CMS e.g. "daily_bonus"

    ``defaultMessage`` - The text to use in the notification if there are no modifications in the Teak CMS.

    ``delayInSeconds`` - The number of seconds from the current time before the notification should be sent.

    ``callback`` - The callback to be called after the notification is scheduled

Callback
    The callback takes two string parameters. The first parameter contains any data from the call, and the second indicates the status of the call. The status can be one of
        ``ok`` - The notification was successfully scheduled

        ``invalid_device`` - The current device has not been registered with Teak

        ``unconfigured_key`` - The current device cannot display notifications

        ``error.internal`` - An unknown error occurred and the call should be retried

    If the call succeeded, the data in the second string will be an opaque identifer that can be passed to ``CancelScheduledNotification`` to cancel the notification.

Canceling a Local Notification
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
To cancel a previously scheduled local notification, use::

    IEnumerator TeakNotification.ScheduleNotification(string scheduledId, System.Action<string, string> callback)

Parameters
    ``scheduleId`` - The id received from the ``ScheduleNotification()`` callback

Callback
    The callback takes two string parameters. The first parameter contains any data from the call, and the second indicates the status of the call. The status can be one of
        ``ok`` - The notification was successfully cancelled

        ``error.internal`` - An unknown error occurred and the call should be retried

    If the call succeeded, the data in the second string will the ``scheduleId`` that was canceled

Canceling all Local Notifications
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
To cancel all previously scheduled local notifications, use::

    IEnumerator TeaKNotification.CancelAllScheduledNotifications(System.Action<string, string> callback)

Callback
    The callback takes two string parameters. The first parameter contains any data from the call, and the second indicates the status of the call. The status can be one of
        ``ok`` The request was succesfully processed

        ``invalid_device`` The current device has not been registered with Teak. This is likely caused by ```identifyUser()``` not being called

        ``error.internal`` An unexpected error occurred and the request should be retried

    If status is ``ok`` then the second string will be a JSON encoded array. Each entry in the array will be a
    dictionary with ``scheduleId`` and ``creativeId`` entries. ``scheduleId`` is the id originally received from the
    ``TeakNotification.ScheduleNotification`` call. ``creativeId`` is the ``creativeId`` originally passed to
    ``TeakNotification.ScheduleNotification()``

.. note:: This call is processed asynchronously. If you immediately call ``TeakNotification.ScheduleNotification()`` after calling ``TeakNotification.CancelAllScheduledNotifications()`` it is possible for your newly scheduled notification to also be canceled. We recommend waiting until the callback has fired before scheduling any new notifications.

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
