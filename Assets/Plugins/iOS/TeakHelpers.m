/* Teak -- Copyright (C) 2014 GoCarrot Inc.
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

#import <AdSupport/AdSupport.h>
#import <objc/runtime.h>

extern "C" {

// Grab most appropriate advertising id, server can resolve to a custom audience id
size_t TeakHelper_GetAttributionId(const char* buffer, size_t bufferSize)
{
    NSString* advertiserID = nil;
    Class ASIdentifierManagerClass = NSClassFromString(@"ASIdentifierManager");
    if(ASIdentifierManagerClass)
    {
        ASIdentifierManager *manager = [ASIdentifierManagerClass sharedManager];
        advertiserID = [[manager advertisingIdentifier] UUIDString];
    }

    if(advertiserID == nil)
    {
        advertiserID = [[UIPasteboard pasteboardWithName:@"fb_app_attribution" create:NO] string];
    }

    if(advertiserID == nil || ([advertiserID length] + 1) > bufferSize)
    {
        return 0;
    }

    strcpy(buffer, [advertiserID UTF8String]);

    return [advertiserID length];
}

}; // extern "C"
