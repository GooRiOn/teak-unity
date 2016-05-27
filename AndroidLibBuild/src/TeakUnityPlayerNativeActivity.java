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

import android.os.Bundle;
import android.content.Intent;

import com.unity3d.player.UnityPlayerNativeActivity;

public class TeakUnityPlayerNativeActivity extends UnityPlayerNativeActivity
{
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        // TODO: Only in development mode?
        Teak.qaInterface = new TeakUnityQAInterface();

        Teak.onCreate(this);
        super.onCreate(savedInstanceState);
    }

    @Override
    protected void onNewIntent(Intent intent) {
        Teak.onNewIntent(intent);
        super.onNewIntent(intent);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        Teak.onActivityResult(requestCode, resultCode, data);
        super.onActivityResult(requestCode, resultCode, data);
    }
}
