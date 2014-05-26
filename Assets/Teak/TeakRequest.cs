using System;
using UnityEngine;
using System.Collections;
using GoCarrotInc.MiniJSON;
using System.Collections.Generic;

public partial class Teak
{
    #region Request
    /// @cond hide_from_doxygen
    public class Request : IDictionary
    {
        public const string PARAMETERS_KEY = "parameters";
        public const string SERVICE_TYPE_KEY = "service_type";
        public const string ENDPOINT_KEY = "endpoint";
        public const string REQUEST_ID_KEY = "request_id";
        public const string REQUEST_DATE_KEY = "request_date";

        public Dictionary<string, object> Parameters
        {
            get;
            internal set;
        }

        public Teak.ServiceType ServiceType
        {
            get;
            internal set;
        }

        public string Endpoint
        {
            get;
            internal set;
        }

        public string RequestId
        {
            get;
            internal set;
        }

        public long RequestDate
        {
            get;
            internal set;
        }

        public float DelayInSeconds
        {
            get;
            internal set;
        }

        protected int NumKeys
        {
            get { return 5; }
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

        public Request(Dictionary<string, object> fromJson)
        {
            this.ServiceType = (Teak.ServiceType)fromJson[SERVICE_TYPE_KEY];
            this.Endpoint = fromJson[ENDPOINT_KEY] as string;
            this.Parameters = fromJson[PARAMETERS_KEY] as Dictionary<string, object>;
            this.RequestDate = (long)fromJson[REQUEST_DATE_KEY];
            this.RequestId = fromJson[REQUEST_ID_KEY] as string;
            this.DelayInSeconds = 0.0f;
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} {2} - {3}: {4}", '-', this.ServiceType, this.RequestId, this.Endpoint, Json.Serialize(this.Parameters));
        }

        #region IDictionary Members
        public object this[object keyObj]
        {
            get
            {
                string key = keyObj as string;
                switch(key)
                {
                    case PARAMETERS_KEY:    return this.Parameters;
                    case SERVICE_TYPE_KEY:  return (int)this.ServiceType;
                    case ENDPOINT_KEY:      return this.Endpoint;
                    case REQUEST_ID_KEY:    return this.RequestId;
                    case REQUEST_DATE_KEY:  return this.RequestDate;
                    default:                return null;
                }
            }
            set { throw new NotImplementedException(); }
        }

        public ICollection Keys
        {
            get
            {
                return new object[] {
                    PARAMETERS_KEY,
                    SERVICE_TYPE_KEY,
                    ENDPOINT_KEY,
                    REQUEST_ID_KEY,
                    REQUEST_DATE_KEY,
                };
            }
        }

        public ICollection Values
        {
            get
            {
                return new object[] {
                    this.Parameters,
                    (int)this.ServiceType,
                    this.Endpoint,
                    this.RequestId,
                    this.RequestId,
                    this.RequestDate
                };
            }
        }

        public bool IsReadOnly { get { return true; } }
        public bool IsFixedSize { get { return true; } }
        public IDictionaryEnumerator GetEnumerator() { throw new NotImplementedException(); }
        public void Clear() { throw new NotImplementedException(); }
        public void Remove(object key) { throw new NotImplementedException(); }
        public bool Contains(object key) { throw new NotImplementedException(); }
        public void Add(object key, object value) { throw new NotImplementedException(); }
        #endregion

        #region ICollection Members
        public bool IsSynchronized { get { return false; } }
        public object SyncRoot { get { throw new NotImplementedException(); } }
        public int Count { get { return this.NumKeys; } }
        public void CopyTo(Array array, int index) { throw new NotImplementedException(); }
        #endregion

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        #endregion
    }
    /// @endcond
    #endregion

    #region CachedRequest
    public class CachedRequest : Request
    {
        public const string RETRIES_KEY = "retries";

        public int Retries
        {
            get;
            internal set;
        }

        internal TeakCache Cache
        {
            get;
            set;
        }

        protected new int NumKeys
        {
            get { return base.NumKeys + 1; }
        }

        internal CachedRequest() {}

        public CachedRequest(Dictionary<string, object> fromJson) : base(fromJson)
        {
            this.Retries = (int)fromJson[RETRIES_KEY];
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} {2} - {3}: {4}", "#", this.ServiceType, this.RequestId, this.Endpoint, Json.Serialize(this.Parameters));
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

        #region IDictionary Members
        public new object this[object keyObj]
        {
            get
            {
                object ret = base[keyObj];
                if(ret == null)
                {
                    string key = keyObj as string;
                    switch(key)
                    {
                        case RETRIES_KEY:   return this.Retries;
                    }
                }
                return ret;
            }
        }

        public new ICollection Keys
        {
            get
            {
                object[] keys = new object[this.NumKeys];
                Array.Copy(base.Keys as object[], keys, base.NumKeys);
                keys[base.NumKeys + 0] = RETRIES_KEY;
                return keys;
            }
        }

        public new ICollection Values
        {
            get
            {
                object[] values = new object[this.NumKeys];
                Array.Copy(base.Values as object[], values, base.NumKeys);
                values[base.NumKeys + 0] = this.Retries;
                return values;
            }
        }
        #endregion
    }
    #endregion
}
