using CQRSlite.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQRSLite.DocumentDB
{
    class AggregateDocument
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }

        public object PartitionKey { get; set; }

        public SerializedEvent[] Events = new SerializedEvent[] { };
    }
}
