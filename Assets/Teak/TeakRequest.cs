using System;
using UnityEngine;
using GoCarrotInc.MiniJSON;
using System.Collections.Generic;
using System.Runtime.Serialization;

public partial class Teak
{
    #region Request
    /// @cond hide_from_doxygen
    public class Request : ISerializable
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

        public Request(SerializationInfo info, StreamingContext context)
        {
            this.ServiceType = (Teak.ServiceType)info.GetValue(SERVICE_TYPE_KEY, typeof(int));
            this.Endpoint = info.GetValue(SERVICE_TYPE_KEY, typeof(string)) as string;
            this.Parameters = info.GetValue(PARAMETERS_KEY, typeof(Dictionary<string, object>)) as Dictionary<string, object>;
            this.RequestDate = (long)info.GetValue(REQUEST_DATE_KEY, typeof(long));
            this.RequestId = info.GetValue(REQUEST_ID_KEY, typeof(string)) as string;
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} {2} - {3}: {4}", '-', this.ServiceType, this.RequestId, this.Endpoint, Json.Serialize(this.Parameters));
        }

        #region ISerializable
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(SERVICE_TYPE_KEY, this.ServiceType, typeof(int));
            info.AddValue(ENDPOINT_KEY, this.Endpoint, typeof(string));
            info.AddValue(PARAMETERS_KEY, this.Parameters, typeof(Dictionary<string, object>));
            info.AddValue(REQUEST_DATE_KEY, this.RequestDate, typeof(long));
            info.AddValue(REQUEST_ID_KEY, this.RequestId, typeof(string));
        }
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

        internal CachedRequest() {}

        public CachedRequest(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.Retries = (int)info.GetValue(RETRIES_KEY, typeof(int));
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
                this.Cache.Dirty = true;
            }
            return ret;
        }

        public bool AddRetryInCache()
        {
            bool ret = true;
            this.DelayInSeconds = (this.DelayInSeconds > 0.0f ? this.DelayInSeconds * 2.0f : 1.0f) + UnityEngine.Random.Range(0.0f, 3.0f);
            lock(this.Cache)
            {
                this.Cache.Dirty = true;
            }
            return ret;
        }

        #region ISerializable
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(RETRIES_KEY, this.Retries, typeof(int));
        }
        #endregion
    }
    #endregion
}
