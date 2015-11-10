using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RemoteTech.FlightComputer
{
    public static class UIPartActionMenuPatcher
    {
        public static void Wrap(Vessel parent, Action<BaseEvent, bool> pass)
        {
            UIPartActionController controller = UIPartActionController.Instance;
            if (!controller) return;

            // Get the open context menus
            FieldInfo listFieldInfo = controller.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                      .First(fi => fi.FieldType == typeof(List<UIPartActionWindow>));

            List<UIPartActionWindow> openWindows = (List<UIPartActionWindow>)listFieldInfo.GetValue(controller);

            foreach (UIPartActionWindow window in openWindows.Where(l => l.part.vessel == parent))
            {
                // Get the list of all UIPartActionItem's
                FieldInfo itemsFieldInfo = typeof(UIPartActionWindow).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                           .First(fi => fi.FieldType == typeof(List<UIPartActionItem>));

                List<UIPartActionItem> item = (List<UIPartActionItem>)itemsFieldInfo.GetValue(window);
                // We only need the UIPartActionEventItem's
                IEnumerable<UIPartActionItem> actionEventButtons = item.Where(l => (l as UIPartActionEventItem) != null);

                foreach (UIPartActionEventItem button in actionEventButtons)
                {
                    BaseEvent originalEvent = button.Evt;
                    // Get the BaseEventDelegate object from the button
                    FieldInfo partEventFieldInfo = typeof(BaseEvent).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                                   .First(fi => fi.FieldType == typeof(BaseEventDelegate));

                    BaseEventDelegate partEvent = (BaseEventDelegate)partEventFieldInfo.GetValue(originalEvent);
                    object[] customAttributes = partEvent.Method.GetCustomAttributes(typeof(KSPEvent), true);

                    // Look for the custom attribute skip_control
                    bool skip_control = customAttributes.Any(a => ((KSPEvent)a).category.Contains("skip_control"));

                    if (!skip_control)
                    {
                        // Look for the custom attribute skip_delay
                        bool ignore_delay = customAttributes.Any(a => ((KSPEvent)a).category.Contains("skip_delay"));

                        // Override the old BaseEvent with our BaseEvent to the button
                        FieldInfo eventField = typeof(UIPartActionEventItem).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                               .First(fi => fi.FieldType == typeof(BaseEvent));

                        BaseEventList eventList = originalEvent.listParent;
                        int listIndex = eventList.IndexOf(originalEvent);

                        // create the new BaseEvent
                        BaseEvent hookedEvent = Wrapper.CreateWrapper(originalEvent, pass, ignore_delay);
                        eventList.RemoveAt(listIndex);
                        eventList.Add(hookedEvent);

                        eventField.SetValue(button, hookedEvent);
                    }
                }
            }
        }

        public class WrappedEvent : BaseEvent
        {
            private BaseEvent originalEvent;

            public WrappedEvent(BaseEvent originalEvent, BaseEventList baseParentList, string name, BaseEventDelegate baseActionDelegate)
                : base(baseParentList, name, baseActionDelegate)
            {
                this.originalEvent = originalEvent;
            }

            public void InvokeOriginalEvent()
            {
                originalEvent.Invoke();
            }
        }

        private class Wrapper
        {
            private Action<BaseEvent, bool> mPassthrough;
            private BaseEvent mEvent;
            private bool mIgnoreDelay;

            private Wrapper(BaseEvent original, Action<BaseEvent, bool> passthrough, bool ignore_delay)
            {
                mPassthrough = passthrough;
                mEvent = original;
                mIgnoreDelay = ignore_delay;
            }

            public static BaseEvent CreateWrapper(BaseEvent original, Action<BaseEvent, bool> passthrough, bool ignore_delay)
            {
                ConfigNode cn = new ConfigNode();
                original.OnSave(cn);
                Wrapper wrapper = new Wrapper(original, passthrough, ignore_delay);
                BaseEvent new_event = new WrappedEvent(original, original.listParent, original.name, wrapper.Invoke);
                new_event.OnLoad(cn);

                return new_event;
            }

            [KSPEvent(category = "skip_control")]
            public void Invoke()
            {
                mPassthrough.Invoke(mEvent, mIgnoreDelay);
            }
        }
    }
}