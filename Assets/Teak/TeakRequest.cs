using System;
using UnityEngine;
using GoCarrotInc.MiniJSON;
using System.Collections.Generic;

public partial class Teak
{
    #region Request
    /// @cond hide_from_doxygen
    public class Request : Dictionary<string, object>
    {
        public Dictionary<string, object> Parameters
        {
            get
            {
                return this["parameters"] as Dictionary<string, object>;
            }
            internal set
            {
                this["parameters"] = value;
            }
        }

        public Teak.ServiceType ServiceType
        {
            get
            {
                return (Teak.ServiceType)this["service_type"];
            }
            internal set
            {
                this["service_type"] = value;
            }
        }

        public string Endpoint
        {
            get
            {
                return this["endpoint"] as string;
            }
            internal set
            {
                this["endpoint"] = value;
            }
        }

        public string RequestId
        {
            get
            {
                return this["request_id"] as string;
            }
            internal set
            {
                this["request_id"] = value;
            }
        }

        public long RequestDate
        {
            get
            {
                return (long)this["request_date"];
            }
            internal set
            {
                this["request_date"] = value;
            }
        }

        public float DelayInSeconds
        {
            get;
            internal set;
        }

        public Request() {}

        public Request(ServiceType serviceType, string endpoint, Dictionary<string, object> parameters)
        {
            this.ServiceType = serviceType;
            this.Endpoint = endpoint;
            this.Parameters = parameters;
            this.RequestDate = (long)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
            this.RequestId = System.Guid.NewGuid().ToString();
            this.DelayInSeconds = 0.0f;
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} {2} - {3}: {4}", '-', this.ServiceType, this.RequestId, this.Endpoint, Json.Serialize(this.Parameters));
        }
    }
    /// @endcond
    #endregion

    #region CachedRequest
    public class CachedRequest : Request
    {
        public int Retries
        {
            get
            {
                return (int)this["retries"];
            }
            internal set
            {
                this["retries"] = value;
            }
        }

        internal long CacheId
        {
            get
            {
                return (long)this["cache_id"];
            }
            set
            {
                this["cache_id"] = value;
            }
        }

        internal TeakCache Cache
        {
            get;
            set;
        }

        internal CachedRequest() {}

        public override string ToString()
        {
            return string.Format("[{0}] {1} {2} - {3}: {4}", this.CacheId, this.ServiceType, this.RequestId, this.Endpoint, Json.Serialize(this.Parameters));
        }

        public bool RemoveFromCache()
        {
            bool ret = true;
            lock(this.Cache)
            {
                this.Cache.Remove(this);
            }
#if CACHE_ENABLED
            // Mark dirty
#endif
            return ret;
        }

        public bool AddRetryInCache()
        {
            bool ret = true;
            this.DelayInSeconds = (this.DelayInSeconds > 0.0f ? this.DelayInSeconds * 2.0f : 1.0f) + UnityEngine.Random.Range(0.0f, 3.0f);
#if CACHE_ENABLED
            // Mark dirty
#endif
            return ret;
        }
    }
    #endregion
}
