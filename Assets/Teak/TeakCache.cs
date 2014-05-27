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
#   define CACHE_ENABLED
#endif

#region References
using System;
using System.IO;
using UnityEngine;
using GoCarrotInc.MiniJSON;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#endregion

/// @cond hide_from_doxygen
public class TeakCache : List<Teak.CachedRequest>, ISerializable
{
    public const int VERSION = 1;
    public const string FILENAME = "teak.db";

    public static string Filename
    {
        get
        {
            return string.Format("{0}/{1}", Application.persistentDataPath, TeakCache.FILENAME);
        }
    }

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

    public bool Dirty
    {
        get { return mDirty; }
        set
        {
            if(value != mDirty)
            {
                mDirty = true;
            }
        }
    }

    public void markInstallMetricSent()
    {
        // TODO
    }

    public static TeakCache Create()
    {
        TeakCache ret = null;

#if CACHE_ENABLED
        IFormatter formatter = new BinaryFormatter();
        FileStream s = null;
        try
        {
            Debug.Log("Cache file: " + TeakCache.Filename);
            s = new FileStream(TeakCache.Filename, FileMode.Open);
            ret = (TeakCache)formatter.Deserialize(s);
        }
        catch {}
        finally
        {
            if(s != null) s.Close();
        }
#endif

        if(ret == null)
        {
            ret = new TeakCache();
        }

        return ret;
    }

    private TeakCache()
    {
        this.InstallDate = (long)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
        this.InstallMetricSent = false;
#if CACHE_ENABLED
        Debug.Log("CREATING NEW CACHE");
#endif
        mDirty = false;
    }

    public TeakCache(SerializationInfo info, StreamingContext context)
    {
        int version = (int)info.GetValue("version", typeof(int));
        if(version == TeakCache.VERSION)
        {
            this.InstallDate = (long)info.GetValue("install_date", typeof(long));
            this.InstallMetricSent = (bool)info.GetValue("install_metric_sent", typeof(bool));
            List<Teak.CachedRequest> requests = info.GetValue("requests", typeof(List<Teak.CachedRequest>)) as List<Teak.CachedRequest>;
            // TODO: prune requests older than 3 days
            if(requests != null && requests.Count > 0) this.AddRange(requests);
        }
        else
        {
            // Handle upgrade path when it becomes needed
            throw new NotImplementedException();
        }
        mDirty = false;
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
            this.Dirty = true;
        }

        return ret;
    }

    #region ISerializable
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("version", TeakCache.VERSION, typeof(int));
        info.AddValue("install_date", this.InstallDate, typeof(long));
        info.AddValue("install_metric_sent", this.InstallMetricSent, typeof(bool));
        info.AddValue("requests", this, typeof(List<Teak.CachedRequest>));
    }
    #endregion

    #region Data
    bool mDirty;
    #endregion
}
// @endcond
