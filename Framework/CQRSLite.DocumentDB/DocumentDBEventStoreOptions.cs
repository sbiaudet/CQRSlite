using CQRSlite.Events;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CQRSLite.DocumentDB
{
    public class DocumentDBEventStoreOptions : IOptions<DocumentDBEventStoreOptions>
    {

        public string CollectionName { get; set; }
        public string DatabaseName { get; set; }

        public Uri EndpointUri { get; set; }
        public string AuthToken { get; set; }

        internal PropertyInfo PartitionKeyProperty { get; set; }

        public DocumentDBEventStoreOptions PartitionKey<TEntity,TProperty>(Expression<Func<TEntity, TProperty>> expression) where TEntity : class, IEvent, new()
        {

            var memberExpression = (MemberExpression)expression.Body;
            this.PartitionKeyProperty = (PropertyInfo)memberExpression.Member;

            return this;
        }

        public DocumentDBEventStoreOptions Value => this;
    }
}
