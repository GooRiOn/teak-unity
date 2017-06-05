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

// From Teak.m
extern NSString* const TeakNotificationAppLaunch;

// Unity
extern void UnitySendMessage(const char*, const char*, const char*);

void checkTeakNotifLaunch(NSDictionary* userInfo)
{
   NSMutableDictionary* eventDataDictionary = [NSMutableDictionary dictionary];

   NSDictionary* teakReward = [userInfo objectForKey:@"teakReward"];
   if(teakReward != nil && teakReward != [NSNull null])
   {
      [eventDataDictionary setObject:teakReward forKey:@"reward"];
   }

   NSURL* teakDeepLink = [userInfo objectForKey:@"teakDeepLink"];
   if(teakDeepLink != nil && teakDeepLink != [NSNull null])
   {
      [eventDataDictionary setObject:[teakDeepLink absoluteString] forKey:@"deepLink"];
   }

   NSError* error = nil;
   NSData* jsonData = [NSJSONSerialization dataWithJSONObject:eventDataDictionary
                                                      options:0
                                                        error:&error];

   if (error != nil) {
      NSLog(@"[Teak:Unity] Error converting to JSON: %@", error);
      UnitySendMessage("TeakGameObject", "NotificationLaunch", "{}");
   } else {
      NSString* jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
      UnitySendMessage("TeakGameObject", "NotificationLaunch", [jsonString UTF8String]);
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
