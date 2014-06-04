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
public partial class Teak
{
    [Serializable]
    private class Cache : List<Teak.CachedRequest>, ISerializable
    {
        public const int VERSION = 1;
        public const string FILENAME = "teak.db";

        public static string Filename
        {
            get
            {
                return string.Format("{0}/{1}", Application.persistentDataPath, Cache.FILENAME);
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
                    lock(this)
                    {
                        mDirty = value;
                        if(mDirty)
                        {
                            this.Serialize();
                        }
                    }
                }
            }
        }

        public bool AdvertisingIdsSent
        {
            get;
            private set;
        }

        public string CustomAudienceId
        {
            get;
            set;
        }

        public string MobileAdvertisingId
        {
            get;
            set;
        }

        public void MarkInstallMetricSent()
        {
            lock(this)
            {
                this.InstallMetricSent = true;
                this.Serialize();
            }
        }

        public static Cache Create()
        {
            Cache ret = null;

#if CACHE_ENABLED
            FileStream s = null;
            try
            {
                IFormatter formatter = new BinaryFormatter();
                Debug.Log("Cache file: " + Cache.Filename);
                s = new FileStream(Cache.Filename, FileMode.Open);
                ret = (Cache)formatter.Deserialize(s);
                ret.mFileStream = s;
            }
            catch
            {
                if(s != null) s.Close();
            }
#endif

            if(ret == null)
            {
                ret = new Cache();
#if CACHE_ENABLED
                ret.Serialize();
                Debug.Log("CREATING NEW CACHE");
#endif
                ret.mDirty = false;
            }

            return ret;
        }

        private Cache()
        {
            this.InstallDate = (long)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
            this.InstallMetricSent = false;
            this.AdvertisingIdsSent = false;
            this.CustomAudienceId = null;
            mDirty = true;
#if CACHE_ENABLED
            mStreamFormatter = new BinaryFormatter();
            mFileStream = new FileStream(Cache.Filename, FileMode.Create);
#endif
        }

        public void Close()
        {
#if CACHE_ENABLED
            this.Serialize();
            mFileStream.Close();
            mFileStream = null;
#endif
        }

#if CACHE_ENABLED
        public Cache(SerializationInfo info, StreamingContext context)
        {
            int version = (int)info.GetValue("version", typeof(int));
            if(version == Cache.VERSION)
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
            mStreamFormatter = new BinaryFormatter();
            // Create filestream in Create()
        }
#endif

        public Teak.CachedRequest CacheRequest(Teak.ServiceType serviceType, string endpoint, Dictionary<string, object> parameters)
        {
            Teak.CachedRequest ret = new Teak.CachedRequest();
            ret.ServiceType = serviceType;
            ret.Endpoint = endpoint;
            ret.Parameters = parameters;
            ret.RequestDate = (long)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
            ret.RequestId = System.Guid.NewGuid().ToString();
            ret.Retries = 0;
            ret.Cache = this;

            lock(this)
            {
                this.Add(ret);
            }
            this.Dirty = true;

            return ret;
        }

        public void Serialize()
        {
#if CACHE_ENABLED
            lock(this)
            {
                mFileStream.Seek(0, SeekOrigin.Begin);
                mStreamFormatter.Serialize(mFileStream, this);
                mFileStream.Flush();
                this.Dirty = false;
            }
#endif
        }

        #region ISerializable
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("version", Cache.VERSION, typeof(int));
            info.AddValue("install_date", this.InstallDate, typeof(long));
            info.AddValue("install_metric_sent", this.InstallMetricSent, typeof(bool));
            info.AddValue("requests", this, typeof(List<Teak.CachedRequest>));
        }
        #endregion

        #region Data
        bool mDirty;
#if CACHE_ENABLED
        FileStream mFileStream;
        IFormatter mStreamFormatter;
#endif
        #endregion
    }
    // @endcond
}
