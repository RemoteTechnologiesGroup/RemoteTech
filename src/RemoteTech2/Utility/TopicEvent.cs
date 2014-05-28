using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public interface ITopicEvent<TKey, TValue>
    {
        void Subscribe(TKey key, Action<TValue> subscriber);
        void Unsubscribe(TKey key, Action<TValue> subscriber);
    }
    public class TopicEventOwner<TKey, TValue> : ITopicEvent<TKey, TValue>
    {
        private Dictionary<TKey, Action<TValue>> eventMap = new Dictionary<TKey, Action<TValue>>();
        private TopicEvent client;
        public ITopicEvent<TKey, TValue> Client
        {
            get { return client ?? (client = new TopicEvent(this)); }
        }
        public void Trigger(TKey key, TValue value)
        {
            if (!eventMap.ContainsKey(key)) return;
            eventMap[key].Invoke(value);
        }

        public void Subscribe(TKey key, Action<TValue> subscriber)
        {
            if (!eventMap.ContainsKey(key)) eventMap[key] = delegate { };
            eventMap[key] += subscriber;
        }

        public void Unsubscribe(TKey key, Action<TValue> subscriber)
        {
            if (!eventMap.ContainsKey(key)) return;
            eventMap[key] = (Action<TValue>)Delegate.RemoveAll(eventMap[key], subscriber);
        }

        private class TopicEvent : ITopicEvent<TKey, TValue>
        {
            private TopicEventOwner<TKey, TValue> owner;
            public TopicEvent(TopicEventOwner<TKey, TValue> owner)
            {
                this.owner = owner;
            }

            public void Subscribe(TKey key, Action<TValue> subscriber)
            {
                owner.Subscribe(key, subscriber);
            }

            public void Unsubscribe(TKey key, Action<TValue> subscriber)
            {
                owner.Unsubscribe(key, subscriber);
            }
        }
    }
}
