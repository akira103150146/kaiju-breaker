using NUnit.Framework;
using KaijuBreaker.Core;

namespace KaijuBreaker.Tests.EditMode.Core
{
    /// <summary>
    /// Story 004 — verifies the event bus contract: delivery, payload fidelity,
    /// unsubscribe, no-subscriber safety, re-entrant publish, and that subscribe/
    /// unsubscribe issued during a dispatch are deferred (list not mutated mid-iteration).
    /// </summary>
    public sealed class TypedEventBusTests
    {
        private struct Ping : IGameEvent
        {
            public int Value;
            public Ping(int v) { Value = v; }
        }

        [Test]
        public void Publish_InvokesSubscriber_WithPayload()
        {
            var bus = new TypedEventBus();
            int received = -1;
            bus.Subscribe<Ping>(p => received = p.Value);

            bus.Publish(new Ping(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            var bus = new TypedEventBus();
            Assert.DoesNotThrow(() => bus.Publish(new Ping(1)));
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            var bus = new TypedEventBus();
            int count = 0;
            void Handler(Ping p) => count++;

            bus.Subscribe<Ping>(Handler);
            bus.Publish(new Ping(0));
            bus.Unsubscribe<Ping>(Handler);
            bus.Publish(new Ping(0));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void MultipleSubscribers_AllInvoked()
        {
            var bus = new TypedEventBus();
            int a = 0, b = 0;
            bus.Subscribe<Ping>(_ => a++);
            bus.Subscribe<Ping>(_ => b++);

            bus.Publish(new Ping(0));

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }

        [Test]
        public void ReentrantPublish_OfAnotherType_IsDeliveredSynchronously()
        {
            var bus = new TypedEventBus();
            bool innerSeen = false;

            bus.Subscribe<LaserHit>(_ => bus.Publish(new MissileHit(1, 1, 30f, WeaponId.M3)));
            bus.Subscribe<MissileHit>(_ => innerSeen = true);

            bus.Publish(new LaserHit(1, 1, 5f));

            Assert.IsTrue(innerSeen, "Nested publish should be dispatched synchronously within the outer dispatch.");
        }

        [Test]
        public void UnsubscribeDuringDispatch_IsDeferred_AndDoesNotThrow()
        {
            var bus = new TypedEventBus();
            int calls = 0;
            void SelfRemoving(Ping p)
            {
                calls++;
                bus.Unsubscribe<Ping>(SelfRemoving); // mutate during dispatch → must be deferred
            }

            bus.Subscribe<Ping>(SelfRemoving);

            Assert.DoesNotThrow(() => bus.Publish(new Ping(0))); // first publish: handler runs, defers its removal
            bus.Publish(new Ping(0));                            // second publish: handler already removed

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void SubscribeDuringDispatch_DoesNotReceiveTheInFlightEvent()
        {
            var bus = new TypedEventBus();
            int lateCalls = 0;
            void Late(Ping p) => lateCalls++;

            bus.Subscribe<Ping>(_ => bus.Subscribe<Ping>(Late)); // adds Late mid-dispatch
            bus.Publish(new Ping(0)); // Late must NOT fire for this event
            Assert.AreEqual(0, lateCalls);

            bus.Publish(new Ping(0)); // now Late is live
            Assert.AreEqual(1, lateCalls);
        }
    }
}
