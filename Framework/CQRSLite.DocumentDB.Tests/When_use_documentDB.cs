using CQRSlite.Events;
using Moq;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace CQRSLite.DocumentDB.Tests
{
    public class When_use_documentDB
    {
        private class FakeEvent : IEvent
        {
            public Guid Id { get; set; }
            public int Version { get; set; }
            public DateTimeOffset TimeStamp { get; set; }
        }

        [Fact]
        public async void Should_save_get_events()
        {

            var options = new DocumentDBEventStoreOptions()
            {
                EndpointUri = new Uri("https://localhost:8081"),
                AuthToken = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                DatabaseName = "CQRSLiteDocumentDB",
                CollectionName = "Events"
            };

            var eventPublisher = new Mock<IEventPublisher>();
            var fakeEvent = new FakeEvent() { Id = Guid.NewGuid() };
            var eventStore = new DocumentDBEventStore(eventPublisher.Object, options);

            await eventStore.Save(new IEvent[] { fakeEvent });

            eventPublisher.Verify(e => e.Publish<IEvent>(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()));

            var t = await eventStore.Get(fakeEvent.Id, 0);
            Assert.Equal(t.FirstOrDefault().Id, fakeEvent.Id);
        }

        [Fact]
        public async void Should_save_get_events_with_new_version()
        {

            var options = new DocumentDBEventStoreOptions()
            {
                EndpointUri = new Uri("https://localhost:8081"),
                AuthToken = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                DatabaseName = "CQRSLiteDocumentDB",
                CollectionName = "Events"
            };

            var eventPublisher = new Mock<IEventPublisher>();

            var aggregateId = Guid.NewGuid();
            var fakeEventV1 = new FakeEvent() { Id = aggregateId, Version = 1 };
            var fakeEventV2 = new FakeEvent() { Id = aggregateId, Version = 2 };
            var eventStore = new DocumentDBEventStore(eventPublisher.Object, options);

            await eventStore.Save(new IEvent[] { fakeEventV1, fakeEventV2 });

            eventPublisher.Verify(e => e.Publish<IEvent>(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()));

            var t = await eventStore.Get(aggregateId, 0);
            Assert.Equal(t.Count(), 2);

            var res = await eventStore.Get(aggregateId, 2);
            Assert.Equal(res.Count(), 1);

        }

        [Fact]
        public async void Should_save_get_events_with_partitionKey()
        {

            var options = new DocumentDBEventStoreOptions()
            {
                EndpointUri = new Uri("https://localhost:8081"),
                AuthToken = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                DatabaseName = "CQRSLiteDocumentDB",
                CollectionName = "Events"
            };

            options.PartitionKey<FakeEvent, Guid>(evt => evt.Id);

            var eventPublisher = new Mock<IEventPublisher>();
            var fakeEvent = new FakeEvent() { Id = Guid.NewGuid() };
            var eventStore = new DocumentDBEventStore(eventPublisher.Object, options);

            await eventStore.Save(new IEvent[] { fakeEvent });

            eventPublisher.Verify(e => e.Publish<IEvent>(It.IsAny<IEvent>(), It.IsAny<CancellationToken>()));

            var t = await eventStore.Get(fakeEvent.Id, 0);
            Assert.Equal(t.FirstOrDefault().Id, fakeEvent.Id);
        }


    }
}
