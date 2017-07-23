using CQRSlite.Events;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;

namespace CQRSLite.DocumentDB
{
    public class DocumentDBEventStore : IEventStore
    {
        private readonly DocumentClient client;
        private readonly IEventPublisher eventPublisher;
        private readonly DocumentDBEventStoreOptions options;

        public DocumentDBEventStore(IEventPublisher eventPublisher, IOptions<DocumentDBEventStoreOptions> options)
        {
            this.eventPublisher = eventPublisher;
            this.options = options.Value;
            this.client = new DocumentClient(this.options.EndpointUri, this.options.AuthToken);
            this.InitCollection();
        }

        private void InitCollection()
        {
            var db = new Database() { Id = this.options.DatabaseName };
            this.client.CreateDatabaseIfNotExistsAsync(db).Wait();

            var databaseUri = UriFactory.CreateDatabaseUri(this.options.DatabaseName);
            var collection = new DocumentCollection()
            {
                Id = this.options.CollectionName,
                PartitionKey = new PartitionKeyDefinition() { Paths = { "/PartitionKey" } }
            };

            this.client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, collection).Wait();
        }

        public async Task<IEnumerable<IEvent>> Get(Guid aggregateId, int fromVersion, CancellationToken cancellationToken = default(CancellationToken))
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(this.options.DatabaseName, this.options.CollectionName);
            var query = this.client.CreateDocumentQuery<AggregateDocument>(collectionUri, new FeedOptions() { EnableCrossPartitionQuery = true })
                 .Where(agg => agg.Id == aggregateId)
                 .SelectMany(agg => agg.Events)
                 .Where(evt => evt.Version >= fromVersion).AsDocumentQuery();

            var response = await query.ExecuteNextAsync<SerializedEvent>();

            return response.Select(ev => ev.GetEventSource());
        }

        public async Task Save(IEnumerable<IEvent> events, CancellationToken cancellationToken = default(CancellationToken))
        {
            var writer = events.GroupBy(evt => evt.Id).Select(grp => CreateOrUpdateDocument(grp));
            Task.WaitAll(writer.ToArray());

            foreach (var evt in events)
            {
                await this.eventPublisher.Publish(evt);
            }
        }

        private async Task CreateOrUpdateDocument(IGrouping<Guid, IEvent> eventsById)
        {

            var partitionKey = this.options.PartitionKeyProperty == null ? "CQRSLitePartitionEmpty" : this.options.PartitionKeyProperty.GetValue(eventsById.FirstOrDefault()).ToString();

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.options.DatabaseName, this.options.CollectionName, eventsById.Key.ToString());
                var aggregateDocument = await this.client.ReadDocumentAsync<AggregateDocument>(documentUri, new RequestOptions() { PartitionKey = new PartitionKey(partitionKey) });
                aggregateDocument.Document.Events = aggregateDocument.Document.Events.Union(eventsById.Select(evt => new SerializedEvent(evt))).ToArray();
                await this.client.UpsertDocumentAsync(documentUri, aggregateDocument);
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    var aggregateDocument = new AggregateDocument()
                    {
                        Id = eventsById.Key,
                        PartitionKey = partitionKey
                    };

                    aggregateDocument.Events = aggregateDocument.Events.Union(eventsById.Select(evt => new SerializedEvent(evt))).ToArray();
                    await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(this.options.DatabaseName, this.options.CollectionName), aggregateDocument, null, true);
                }
            }
        }
    }
}
