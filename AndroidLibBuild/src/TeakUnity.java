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

class TeakUnity {
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
            TeakUnity.unitySendMessage = unityPlayerClass.getMethod("UnitySendMessage", String.class, String.class, String.class);
        } catch (Exception e) {
            if (Teak.isDebug) {
                Teak.log.exception(e);
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
            Log.e("Teak:Unity", "Teak.localBroadcastManager is null, initialization order is incorrect.");
        }
    }

    public static void readyForDeepLinks() {
        deepLinksReadyTask.run();
    }

    public static boolean isAvailable() {
        return TeakUnity.unitySendMessage != null;
    }

    public static void UnitySendMessage(String gameObject, String method, String message) {
        if (TeakUnity.isAvailable()) {
            try {
                TeakUnity.unitySendMessage.invoke(null, gameObject, method, message);
            } catch (Exception e) {
                Teak.log.exception(e);
            }
        }
    }

    public static void registerRoute(final String route, final String name, final String description) {
        try {
            DeepLink.registerRoute(route, name, description, new DeepLink.Call() {
                @Override
                public void call(Map<String, Object> parameters) {
                    try {
                        if (TeakUnity.isAvailable()) {
                            JSONObject eventData = new JSONObject();
                            eventData.put("route", route);
                            eventData.put("parameters", new JSONObject(parameters));
                            TeakUnity.UnitySendMessage("TeakGameObject", "DeepLink", eventData.toString());
                        }
                    } catch (Exception e) {
                        Teak.log.exception(e);
                    }
                }
            });
        } catch(Exception e) {
            Teak.log.exception(e);
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

                    if (bundle.getString("teakRewardId") != null) {
                        eventDataDict.put("incentivized", true);
                        eventDataDict.put("teakRewardId", bundle.getString("teakRewardId"));
                    } else {
                        eventDataDict.put("incentivized", false);
                    }
                    if (bundle.getString("teakScheduleName") != null) eventDataDict.put("teakScheduleName", bundle.getString("teakScheduleName"));
                    if (bundle.getString("teakCreativeName") != null) eventDataDict.put("teakCreativeName", bundle.getString("teakCreativeName"));

                    eventData = new JSONObject(eventDataDict).toString();
                } catch(Exception e) {
                    Teak.log.exception(e);
                } finally {
                    if (TeakUnity.isAvailable()) {
                        TeakUnity.UnitySendMessage("TeakGameObject", "NotificationLaunch", eventData);
                    }
                }
            } else if (Teak.REWARD_CLAIM_ATTEMPT.equals(action)) {
                Bundle bundle = intent.getExtras();
                try {
                    HashMap<String, Object> reward = (HashMap<String, Object>) bundle.getSerializable("reward");
                    if (TeakUnity.isAvailable() && reward != null) {
                        String eventData = new JSONObject(reward).toString();
                        TeakUnity.UnitySendMessage("TeakGameObject", "RewardClaimAttempt", eventData);
                    }
                } catch(Exception e) {
                    Teak.log.exception(e);
                }
            }
        }
    };
}
