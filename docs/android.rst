Android
=======
.. important:: Teak requires that Google Play Services and Android Support v4 are included in your build.

    The Facebook SDK requires them as well, so you should be all set.

Make Teak your Main Activity
----------------------------
.. highlight:: xml

The easiest way to get Teak working on Android is to edit your ``Assets/Plugins/Android/AndroidManifest.xml`` and change the main activity from::

    <activity android:name="com.unity3d.player.UnityPlayerActivity" android:label="@string/app_name">

to::

    <activity android:name="io.teak.sdk.wrapper.unity.TeakUnityPlayerActivity" android:label="@string/app_name">

.. note:: This should work just fine for most games. You can go right to :ref:`skip-to-edit-teak-xml`

If there's a reason that you can't do this, then follow the steps below.

Otherwise Add Teak to your Main Activity
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
If you can't change your main activity, then you need to add the Teak initialization calls into your custom activity.

.. highlight:: java

Import Teak into your main activity::

    import io.teak.sdk.Teak;

Call Teak.onCreate **before** the call to super.onCreate::

    protected void onCreate(Bundle savedInstanceState) {
        Teak.onCreate(this);
        // ... etc
        super.onCreate(savedInstanceState);
        // ... etc
    }

Call Teak.onActivityResult **before** the call to super.onActivityResult::

    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        Teak.onActivityResult(requestCode, resultCode, data);
        super.onActivityResult(requestCode, resultCode, data);
        // ... etc
    }

Call Teak.onNewIntent **before** the call to super.onNewIntent::

    protected void onNewIntent(Intent intent) {
        Teak.onNewIntent(intent);
        super.onNewIntent(intent);
        // ... etc
    }

What This Does
^^^^^^^^^^^^^^
This lets Teak hook into the Android app lifecycle and configure itself, listen for Facebook logins, billing events, and begin sending information to the Teak Service.

Testing It
^^^^^^^^^^
Run your game on an Android device, and look at the Android debug log output.

You Should See
^^^^^^^^^^^^^^
::

    I/Teak: {
      "event_type":"teak.state",
      "log_level":"INFO",
      "timestamp":<timestamp>,
      "event_data": {
        "state":"Created",
        "old_state":"Allocated"
      },
      "event_id":1,
      "sdk_version": {
        "unity":"<unity-sdk-version>",
        "android":"<android-sdk-version>"
      },
      "run_id":"<some-guid>"
    }

And many other Teak log entries.

.. note:: If You Don't See Teak debug log messages, check to make sure your game is being built in debug mode.

If You See
^^^^^^^^^^
    java.lang.RuntimeException: Failed to find R.string.io_teak_api_key

It means that the ``res/values/teak.xml`` file was not found. See below...

.. _skip-to-edit-teak-xml:

Edit res/values/teak.xml
------------------------
.. highlight:: xml

The Teak Editor scripts for Unity will attempt to manage this file for you, if your build environment needs to provide the file itself, this is what it should look like::

    <?xml version="1.0" encoding="utf-8"?>
    <resources>
        <string name="io_teak_app_id">YOUR_TEAK_APP_ID</string>
        <string name="io_teak_api_key">YOUR_TEAK_API_KEY</string>
        <string name="io_teak_gcm_sender_id">YOUR_GCM_SENDER_ID</string>

        <!-- You can customize notification settings with these keys -->
        <integer name="io_teak_notification_accent_color">0xfff15a29</integer> <!-- Color: 0xAARRGGBB -->
        <drawable name="io_teak_small_notification_icon">@drawable/icon</drawable>

        <!-- An additional value for io_teak_small_notification_icon should be placed in values-v21
           with a white and transparent icon for Lollipop. -->
    </resources>

.. note:: Replace ``YOUR_TEAK_APP_ID``, ``YOUR_TEAK_API_KEY``, and ``YOUR_GCM_SENDER_ID`` with your game's values.

Your Teak App Id and API Key can be found in the Settings for your app on the Teak dashboard:

.. warning:: TODO: Screenshot

What This Does
^^^^^^^^^^^^^^
This provides Teak with the credentials needed to send information to the Teak Service.

Setting Up Push Notifications
-----------------------------
Add the following to the ``<application>`` section of your ``Assets/Plugins/Android/AndroidManifest.xml``::

    <receiver android:name="io.teak.sdk.Teak" android:exported="true"
              android:permission="com.google.android.c2dm.permission.SEND">
        <intent-filter>
            <action android:name="YOUR_ANDROID_BUNDLE_ID.intent.TEAK_NOTIFICATION_OPENED" />
            <action android:name="YOUR_ANDROID_BUNDLE_ID.intent.TEAK_NOTIFICATION_CLEARED" />
            <action android:name="com.google.android.c2dm.intent.RECEIVE" />
            <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
            <category android:name="YOUR_ANDROID_BUNDLE_ID" />
        </intent-filter>
    </receiver>

    <service android:name="io.teak.sdk.InstanceIDListenerService" android:exported="false" >
        <intent-filter>
            <action android:name="com.google.android.gms.iid.InstanceID" />
        </intent-filter>
    </service>

.. note:: Replace ``YOUR_ANDROID_BUNDLE_ID`` with the bundle id for your game.

What This Does
^^^^^^^^^^^^^^
This tells Android to inform Teak about push notifications.

Setting Up Deep Linking
-----------------------
Add the following to the ``<activity>`` section of your ``Assets/Plugins/Android/AndroidManifest.xml``::

    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="http" android:host="YOUR_SUBDOMAIN.jckpt.me" />
        <data android:scheme="https" android:host="YOUR_SUBDOMAIN.jckpt.me" />
    </intent-filter>
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="teakYOUR_TEAK_APP_ID" android:host="*" />
    </intent-filter>

.. note:: Replace ``YOUR_TEAK_APP_ID`` with your Teak App Id and ``YOUR_SUBDOMAIN`` with your Teak Subdomain.

Your Teak App Id and Teak Subdomain can be found in the Settings for your app on the Teak dashboard:

.. warning:: TODO: Screenshot

What This Does
^^^^^^^^^^^^^^
This tells Android to look for deep link URLs created by Teak.
