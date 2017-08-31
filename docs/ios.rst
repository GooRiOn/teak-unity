iOS
===
Edit Info.plist
------------------------
.. highlight:: xml

The Teak Editor scripts for Unity will attempt to manage this file for you, if your build environment needs to provide the file itself, this is what it should look like::

    <key>CFBundleURLTypes</key>
    <array>
        <dict>
            <key>CFBundleTypeRole</key>
            <string>Editor</string>
            <key>CFBundleURLSchemes</key>
            <array>
                <string>teakYOUR_TEAK_APP_ID</string>
            </array>
        </dict>
    </array>
    <key>TeakApiKey</key>
    <string>YOUR_TEAK_API_KEY</string>
    <key>TeakAppId</key>

.. note:: Replace ``YOUR_TEAK_APP_ID``, and ``YOUR_TEAK_API_KEY`` with your game's values.

Your Teak App Id and API Key can be found in the Settings for your app on the Teak dashboard:

.. warning:: TODO: Screenshot

Edit Unity-iPhone.entitlements
------------------------------
You'll need to add your Teak subdomain to the associated domains in order for deep linking to work properly on iOS. Add the following to your ``Unity-iPhone.entitlements``::

    <key>com.apple.developer.associated-domains</key>
    <array>
        <string>applinks:YOUR_SUBDOMAIN.jckpt.me</string>
    </array>

.. note:: Replace ``YOUR_SUBDOMAIN`` with your game's subdomain.

Your Teak Subdomain can be found in the Settings for your app on the Teak dashboard:

.. warning:: TODO: Screenshot
