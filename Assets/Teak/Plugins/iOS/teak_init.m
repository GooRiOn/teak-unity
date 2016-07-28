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

// From Classes/main.mm
extern const char* AppControllerClassName;

// From TeakHooks.m
extern void Teak_Plant(Class appDelegateClass, NSString* appId, NSString* appSecret);

// From TeakCExtern.m
extern void* TeakRewardRewardForId(NSString* teakRewardId);
extern BOOL TeakRewardIsCompleted(void* notif);
extern const char* TeakRewardGetJson(void* reward);

// From Teak.m
extern NSString* const TeakNotificationAppLaunch;

// Unity
extern void UnitySendMessage(const char*, const char*, const char*);

void checkTeakNotifLaunch(NSDictionary* userInfo)
{
   // TODO: userInfo also can contain a 'deepLink' key
   NSString* teakRewardId = [userInfo objectForKey:@"teakRewardId"];
   if(teakRewardId != nil)
   {
      void* reward = TeakRewardRewardForId(teakRewardId);
      if(reward != nil)
      {
         __block NSObject* o = CFBridgingRetain(reward);
         dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^(void) {
            while(!TeakRewardIsCompleted(reward))
            {
               sleep(1);
            }

            UnitySendMessage("TeakGameObject", "NotificationLaunch", TeakRewardGetJson(o));
            CFRelease(o);
         });
      }
      else
      {
         UnitySendMessage("TeakGameObject", "NotificationLaunch", "");
      }
   }
   else
   {
      UnitySendMessage("TeakGameObject", "NotificationLaunch", "");
   }
}

__attribute__((constructor))
static void teak_init()
{
   NSString* appId = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"TeakAppId"];
   NSString* apiKey = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"TeakApiKey"];
   Teak_Plant(NSClassFromString([NSString stringWithUTF8String:AppControllerClassName]), appId, apiKey);

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakNotificationAppLaunch
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    checkTeakNotifLaunch(notification.userInfo);
                                                 }];
}
