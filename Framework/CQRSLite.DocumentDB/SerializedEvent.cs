using CQRSlite.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CQRSLite.DocumentDB
{
    public class SerializedEvent
    {
        public SerializedEvent()
        {

        }

        public SerializedEvent(IEvent evt)
        {
            this.EventRaw = JsonConvert.SerializeObject(evt);
            this.EventType = evt.GetType();
            this.Id = evt.Id;
            this.Version = evt.Version;
            this.TimeStamp = evt.TimeStamp;
            
        }

       
        public string EventRaw { get; set; }
        public Type EventType { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid Id { get; set; }
     
        public IEvent GetEventSource()
        {
            return JsonConvert.DeserializeObject(this.EventRaw, this.EventType) as IEvent;
        }
    }
}
