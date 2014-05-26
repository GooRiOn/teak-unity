#region License
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
#endregion
#if UNITY_IPHONE || UNITY_ANDROID
//#   define CACHE_ENABLED
#endif

#region References
using System;
using UnityEngine;
using GoCarrotInc.MiniJSON;
using System.Collections.Generic;
#endregion

/// @cond hide_from_doxygen
public class TeakCache : List<Teak.CachedRequest>
{
    public bool InstallMetricSent
    {
        get;
        private set;
    }

    public long InstallDate
    {
        get;
        private set;
    }

    public void markInstallMetricSent()
    {
        // TODO
    }

    public TeakCache()
    {
#if CACHE_ENABLED
        // Read from disk
#endif
    }

    public Teak.CachedRequest CacheRequest(Teak.ServiceType serviceType, string endpoint, Dictionary<string, object> parameters)
    {
        Teak.CachedRequest ret = new Teak.CachedRequest();
        ret.ServiceType = serviceType;
        ret.Endpoint = endpoint;
        ret.Parameters = parameters;
        ret.RequestDate = (long)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
        ret.RequestId = System.Guid.NewGuid().ToString();
        ret.Retries = 0;

        lock(this)
        {
            ret.Cache = this;
            this.Add(ret);
        }

        return ret;
    }

    #region Data
    
    #endregion
}
// @endcond
