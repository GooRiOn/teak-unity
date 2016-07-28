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

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.util.Log;

import java.lang.reflect.Method;
import java.util.concurrent.Future;

class Unity {
    private static final String LOG_TAG = "Teak:Unity";

    private static Method unitySendMessage;

    static {
        try {
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
        filter.addAction(TeakNotification.LAUNCHED_FROM_NOTIFICATION_INTENT);
        if (Teak.localBroadcastManager != null) {
            Teak.localBroadcastManager.registerReceiver(broadcastReceiver, filter);
        } else {
            Log.e(LOG_TAG, "Teak.localBroadcastManager is null, initialization order is incorrect.");
        }
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

    static BroadcastReceiver broadcastReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();
            if (TeakNotification.LAUNCHED_FROM_NOTIFICATION_INTENT.equals(action)) {
                try {
                    String teakRewardId = intent.getStringExtra("teakRewardId");
                    if(teakRewardId != null) {
                        final Future<TeakNotification.Reward> rewardFuture = TeakNotification.Reward.rewardFromRewardId(teakRewardId);
                        if(rewardFuture != null) {
                            new Thread(new Runnable() {
                                @Override
                                public void run() {
                                    String eventData = "";
                                    try {
                                        TeakNotification.Reward reward = rewardFuture.get();
                                        eventData = reward.originalJson.toString();
                                    } catch(Exception e) {
                                        Log.e(LOG_TAG, Log.getStackTraceString(e));
                                    } finally {
                                        Unity.UnitySendMessage("TeakGameObject", "NotificationLaunch", eventData);
                                    }
                                }
                            }).start();
                        } else {
                            Unity.UnitySendMessage("TeakGameObject", "NotificationLaunch", "");
                        }
                    }
                } catch(Exception e) {
                    Log.e(LOG_TAG, Log.getStackTraceString(e));
                }
            }
        }
    };
}
