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
extern NSString* const TeakOnReward;

extern NSDictionary* TeakWrapperSDK;

typedef void (^TeakLinkBlock)(NSDictionary* _Nonnull parameters);
extern void TeakRegisterRoute(const char* route, const char* name, const char* description, TeakLinkBlock block);

extern void TeakRunNSOperation(NSOperation* op);
extern void TeakAssignWaitForDeepLinkOperation(NSOperation* waitForDeepLinkOp);

// TeakNotification
extern NSObject* TeakNotificationSchedule(const char* creativeId, const char* message, uint64_t delay);
extern NSObject* TeakNotificationCancel(const char* scheduleId);

// Unity
extern void UnitySendMessage(const char*, const char*, const char*);

extern NSString* TeakUnitySDKVersion;

NSOperation* waitForDeepLinkOperation = nil;

void TeakRelease(id ptr)
{
#if __has_feature(objc_arc)
   void *retainedThing = (__bridge void *)ptr;
   id unretainedThing = (__bridge_transfer id)retainedThing;
   unretainedThing = nil;
#else
   [ptr release];
#endif
}

void* TeakNotificationSchedule_Retained(const char* creativeId, const char* message, uint64_t delay)
{
#if __has_feature(objc_arc)
   void* notif = (__bridge_retained void*)TeakNotificationSchedule(creativeId, message, delay);
   return notif;
#else
   return [TeakNotificationSchedule(creativeId, message, delay) retain];
#endif
}

void* TeakNotificationCancel_Retained(const char* scheduleId)
{
#if __has_feature(objc_arc)
   void* notif = (__bridge_retained void*)TeakNotificationCancel(scheduleId);
   return notif;
#else
   return [TeakNotificationCancel(scheduleId) retain];
#endif
}

void checkTeakNotifLaunch(NSDictionary* inUserInfo)
{
   NSError* error = nil;

   NSMutableDictionary* userInfo = [NSMutableDictionary dictionaryWithDictionary:inUserInfo];
   userInfo[@"incentivized"] = userInfo[@"teakRewardId"] == nil ? @NO : @YES;
   NSData* jsonData = [NSJSONSerialization dataWithJSONObject:userInfo
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

void teakOnReward(NSDictionary* userInfo)
{
   NSError* error = nil;
   NSData* jsonData = [NSJSONSerialization dataWithJSONObject:userInfo
                                                      options:0
                                                        error:&error];

   if (error != nil) {
      NSLog(@"[Teak:Unity] Error converting to JSON: %@", error);
   } else {
      NSString* jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
      UnitySendMessage("TeakGameObject", "RewardClaimAttempt", [jsonString UTF8String]);
   }
}

void TeakUnityReadyForDeepLinks()
{
   TeakRunNSOperation(waitForDeepLinkOperation);
}

void TeakUnityRegisterRoute(const char* route, const char* name, const char* description)
{
   NSString* nsRoute = [NSString stringWithUTF8String:route];
   TeakRegisterRoute(route, name, description, ^(NSDictionary * _Nonnull parameters) {
      NSError* error = nil;
      NSData* jsonData = [NSJSONSerialization dataWithJSONObject:@{@"route" : nsRoute, @"parameters" : parameters}
                                                         options:0
                                                           error:&error];

      if (error != nil) {
         NSLog(@"[Teak:Unity] Error converting to JSON: %@", error);
      } else {
         NSString* jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
         UnitySendMessage("TeakGameObject", "DeepLink", [jsonString UTF8String]);
      }
   });
}

__attribute__((constructor))
static void teak_init()
{
   TeakWrapperSDK = @{@"unity" : TeakUnitySDKVersion};

   NSString* appId = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"TeakAppId"];
   NSString* apiKey = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"TeakApiKey"];
   Teak_Plant(NSClassFromString([NSString stringWithUTF8String:AppControllerClassName]), appId, apiKey);

   waitForDeepLinkOperation = [NSBlockOperation blockOperationWithBlock:^{
   }];
   TeakAssignWaitForDeepLinkOperation(waitForDeepLinkOperation);

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakNotificationAppLaunch
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    checkTeakNotifLaunch(notification.userInfo);
                                                 }];

   [[NSNotificationCenter defaultCenter] addObserverForName:TeakOnReward
                                                     object:nil
                                                      queue:nil
                                                 usingBlock:^(NSNotification* notification) {
                                                    teakOnReward(notification.userInfo);
                                                 }];
}
