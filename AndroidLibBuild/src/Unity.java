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
package io.teak.sdk;

import org.json.JSONObject;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.util.Log;
import android.os.Bundle;

import java.lang.reflect.Method;
import java.util.Map;
import java.util.HashMap;

import java.util.concurrent.FutureTask;

class Unity {
    private static final String LOG_TAG = "Teak:Unity";

    private static Method unitySendMessage;

    private static FutureTask<Void> deepLinksReadyTask;

    static {
        try {
            deepLinksReadyTask = new FutureTask<Void>(new Runnable() {
                @Override
                public void run() {
                }
            }, null);
            Teak.waitForDeepLink = deepLinksReadyTask;

            Class<?> unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer");
            Unity.unitySendMessage = unityPlayerClass.getMethod("UnitySendMessage", String.class, String.class, String.class);
            if (Teak.isDebug) {
                Log.d(LOG_TAG, "Teak Unity extensions successfully enabled.");
            }
        } catch (Exception e) {
            if (Teak.isDebug) {
                Log.e(LOG_TAG, Log.getStackTraceString(e));
            }
        }
    }

    public static void initialize() {
        IntentFilter filter = new IntentFilter();
        filter.addAction(Teak.REWARD_CLAIM_ATTEMPT);
        filter.addAction(Teak.LAUNCHED_FROM_NOTIFICATION_INTENT);
        if (Teak.localBroadcastManager != null) {
            Teak.localBroadcastManager.registerReceiver(broadcastReceiver, filter);
        } else {
            Log.e(LOG_TAG, "Teak.localBroadcastManager is null, initialization order is incorrect.");
        }
    }

    public static void readyForDeepLinks() {
        deepLinksReadyTask.run();
    }

    public static boolean isAvailable() {
        return Unity.unitySendMessage != null;
    }

    public static void UnitySendMessage(String gameObject, String method, String message) {
        if (Unity.isAvailable()) {
            try {
                Unity.unitySendMessage.invoke(null, gameObject, method, message);
            } catch (Exception e) {
                Log.e(LOG_TAG, Log.getStackTraceString(e));
            }
        }
    }

    public static void registerRoute(final String route, final String name, final String description) {
        try {
            DeepLink.registerRoute(route, name, description, new DeepLink.Call() {
                @Override
                public void call(Map<String, Object> parameters) {
                    try {
                        JSONObject eventData = new JSONObject();
                        eventData.put("route", route);
                        eventData.put("parameters", new JSONObject(parameters));
                        Unity.UnitySendMessage("TeakGameObject", "DeepLink", eventData.toString());
                    } catch (Exception e) {
                        Log.e(LOG_TAG, Log.getStackTraceString(e));
                    }
                }
            });
        } catch(Exception e) {
            Log.e(LOG_TAG, Log.getStackTraceString(e));
        }
    }

    static BroadcastReceiver broadcastReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();
            if (Teak.LAUNCHED_FROM_NOTIFICATION_INTENT.equals(action)) {
                Bundle bundle = intent.getExtras();
                String eventData = "{}";
                try {
                    HashMap<String, Object> eventDataDict = new HashMap<String, Object>();
                    // TODO: In the future this dict may include more things.
                    eventData = new JSONObject(eventDataDict).toString();
                } catch(Exception e) {
                    Log.e(LOG_TAG, Log.getStackTraceString(e));
                } finally {
                    Unity.UnitySendMessage("TeakGameObject", "NotificationLaunch", eventData);
                }
            } else if (Teak.REWARD_CLAIM_ATTEMPT.equals(action)) {
                Bundle bundle = intent.getExtras();
                try {
                    HashMap<String, Object> reward = (HashMap<String, Object>) bundle.getSerializable("reward");
                    if (reward != null) {
                        String eventData = new JSONObject(reward).toString();
                        Unity.UnitySendMessage("TeakGameObject", "RewardClaimAttempt", eventData);
                    }
                } catch(Exception e) {
                    Log.e(LOG_TAG, Log.getStackTraceString(e));
                }
            }
        }
    };
}
