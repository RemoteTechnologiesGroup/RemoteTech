using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Andvari.EventManager
{
    public class EventManager : IEventManager, IDisposable
    {
        private IServiceContainer container;

        private IDictionary<String, IList<Event>> eventMap = new Dictionary<String, IList<Event>>();
        private IDictionary<IService, IList<String>> publisherMap = new Dictionary<IService, IList<String>>();
        private IDictionary<String, IList<Pair<IService, Event>>> subscriberMap = new Dictionary<String, IList<Pair<IService, Event>>>();

        public EventManager(IServiceContainer container)
        {
            this.container = container;

            container.ServiceRegistered += OnServiceRegistered;
            container.ServiceUnregistered += OnServiceUnregistered;
        }

        public void Dispose()
        {
            container.ServiceRegistered -= OnServiceRegistered;
            container.ServiceUnregistered -= OnServiceUnregistered;

            foreach (var service in publisherMap.Keys)
            {
                service.OnServiceStateChanged -= OnSingletonChangeState;
            }
        }

        private void OnServiceRegistered(ServiceRegistrationEventArgs args)
        {
            if (args.Service.IsSingleton) 
            {
                args.Service.OnServiceStateChanged += OnSingletonChangeState;
            }
        }

        private void OnServiceUnregistered(ServiceUnregistrationEventArgs args)
        {
            if (args.Service.IsSingleton)
            {
                args.Service.OnServiceStateChanged -= OnSingletonChangeState;
            }
        }

        private void OnSingletonChangeState(IService service, ServiceState state)
        {
            if (state == ServiceState.Active)
            {
                RegisterPublisher(service);
                RegisterSubscribers(service);
            }

            if (state == ServiceState.Inactive)
            {
                UnregisterPublisher(service);
                UnregisterSubscribers(service);
            }
        }

        private void RegisterPublisher(IService service)
        {
            var instance = service.GetInstance();
            var implementation = service.Implementation;

            var events = implementation.GetEvents(BindingFlags.Instance | BindingFlags.Public);

            foreach (var e in events)
            {
                var annotation = (Publishes) e.GetCustomAttributes(typeof(Publishes), true).FirstOrDefault();
                if (annotation == null) continue;

                var add = (Action<Delegate>) (d => e.AddEventHandler(service.GetInstance(), d));
                var remove = (Action<Delegate>) (d => e.RemoveEventHandler(service.GetInstance(), d));
                var del = e.EventHandlerType;

                var name = annotation.Name;
                var stub = new Event(name, implementation, del, add, remove);

                if (!eventMap.ContainsKey(name))
                {
                    eventMap[name] = new List<Event>();
                } 
                eventMap[name].Add(stub);

                if (!publisherMap.ContainsKey(service))
                {
                    publisherMap[service] = new List<String>();
                }
                publisherMap[service].Add(name);

                LinkSubscribers(e.Name);
            }
        }

        private void UnregisterPublisher(IService service)
        {
            if (!publisherMap.ContainsKey(service)) return;

            var events = publisherMap[service];

            foreach (var name in events)
            {
                // Remove the event from the event list kept by event name. Delete the entry if the list is empty.
                var eventList = eventMap[name];
                var affectedEvent = eventList.Where(e => e.Implementation.Name == name).FirstOrDefault();

                eventList.Remove(affectedEvent);
                if (!eventList.Any())
                {
                    eventMap.Remove(name);
                }

                // Remove the event from the event list kept by publisher. Delete the entry if the list is empty;
                publisherMap[service].Remove(name);
                if (!publisherMap[service].Any())
                {
                    publisherMap.Remove(service);
                }

                // Unlink all subscribers to this event.
                if (subscriberMap.ContainsKey(name))
                {
                    foreach (var subscriber in subscriberMap[name])
                    {
                        if (subscriber.Value == affectedEvent)
                        {
                            subscriber.Value = null;
                        }
                    }
                }

                // Copy data from the event and dispose of it.
                var clients = affectedEvent.Subscribers.ToList();
                affectedEvent.Dispose();

                // Reregister the events if possible.
                foreach (var del in clients)
                {
                    LinkSubscriber(name, del);
                }
            }
        }

        private void LinkSubscriber(String name, Delegate del)
        {
            // Variation on LinkSubscriber(name, service) that doesn't require method lookup.
            if (!eventMap.ContainsKey(name)) return;

            var e = eventMap[name].First();
            e.Add(del);
        }

        private Event LinkSubscriber(String name, IService service)
        {
            if (!eventMap.ContainsKey(name)) return null;

            var instance = service.GetInstance();
            var implementation = service.Implementation;
            var methods = implementation.GetMethods();
            var e = eventMap[name].First();

            // Find all methods linking to the specified event name and add them to the event.
            foreach (var m in methods)
            {
                var annotations = m.GetCustomAttributes(typeof(SubscribesTo), true).Cast<SubscribesTo>().Where(s => s.Name == name);

                foreach (var annotation in annotations)
                {
                    var del = Delegate.CreateDelegate(e.Delegate, instance, m);
                    e.Add(del);
                }
            }

            return e;
        }

        private void LinkSubscribers(String name)
        {
            if (!subscriberMap.ContainsKey(name)) return;

            // Attempts to link all subscribers to the named event.
            foreach (var subscriber in subscriberMap[name])
            {
                subscriber.Value = subscriber.Value ?? LinkSubscriber(name, subscriber.Key);
            }
        }

        private void RegisterSubscribers(IService service)
        {
            var instance = service.GetInstance();
            var implementation = service.Implementation;

            var methods = implementation.GetMethods();

            // Attempt to link all subscribers if possible, and if not, keep note that the events are yet to be linked.
            foreach (var m in methods)
            {
                var annotation = (SubscribesTo) m.GetCustomAttributes(typeof(SubscribesTo), true).FirstOrDefault();
                if (annotation == null) continue;

                if (!subscriberMap.ContainsKey(annotation.Name))
                {
                    subscriberMap[annotation.Name] = new List<Pair<IService, Event>>();
                }
                subscriberMap[annotation.Name].Add(new Pair<IService, Event>() { Key = service });

                LinkSubscriber(annotation.Name, service);
            }
        }

        private void UnregisterSubscribers(IService service)
        {

        }

        private class Event : IDisposable
        {
            public String Name { get; private set; }
            public Type Implementation { get; private set; }
            public Type Delegate { get; private set; }
            public IList<Delegate> Subscribers { get { return addedDelegates.AsReadOnly(); } }

            private Delegate add;
            private Delegate remove;
            private List<Delegate> addedDelegates = new List<Delegate>();
            public Event(String name, Type implementation, Type del, Delegate add, Delegate remove)
            {
                this.Name = name;
                this.Implementation = implementation;
                this.Delegate = del;
                this.add = add;
                this.remove = remove;
            }

            public void Dispose()
            {
                foreach (var d in addedDelegates)
                {
                    Remove(d);
                }
            }

            public void Add(Delegate del)
            {
                addedDelegates.Add(add);

                add.DynamicInvoke(del);
            }

            public void Remove(Delegate del)
            {
                addedDelegates.Remove(add);

                remove.DynamicInvoke(del);
            }
        }
    }
}
