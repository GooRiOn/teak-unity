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

import android.util.Log;
import android.os.Build;

import java.util.LinkedList;
import java.util.HashMap;

import org.json.JSONObject;

import com.unity3d.player.UnityPlayer;

import io.teak.sdk.Teak;

public class TeakUnityQAInterface extends TeakQAInterface {
    @Override
    public void identifyClient(String clientId) {
        _clientId = clientId;
        JSONObject payload = new JSONObject();
        try {
            payload.put("type", "identify");
            payload.put("id", _clientId);

            JSONObject android = new JSONObject();
            android.put("api_level", Build.VERSION.SDK_INT);
            android.put("version", Build.VERSION.RELEASE);
            payload.put("android", android);
        } catch (Exception ignored) {
        }
        _clientIdentifier = payload.toString();
    }

    @Override
    public void reportEvent(String eventType, String eventName, HashMap<String, Object> extras) {
        JSONObject payload = new JSONObject();
        try {
            payload.put("type", eventType);
            payload.put("name", eventName);
            payload.put("id", _clientId);
            if (extras != null) {
                payload.putOpt("extras", new JSONObject(extras));
            }
        } catch (Exception ignored) {
        }
        _eventQueue.offer(payload.toString());

        try {
            UnityPlayer.UnitySendMessage("TeakGameObject", "RemoteQAEvent", payload.toString());
        } catch (UnsatisfiedLinkError ignored) {
            Log.e(Teak.LOG_TAG, ignored.toString());
        }
    }

    // For Unity use
    static String getClientIdentifier() {
        return _clientIdentifier;
    }

    static void sendEventBacklog() {
        for (String event : _eventQueue) {
            UnityPlayer.UnitySendMessage("TeakGameObject", "RemoteQAEvent", event);
        }
    }

    static String _clientId;
    static String _clientIdentifier;
    static LinkedList<String> _eventQueue = new LinkedList<String>();
}
