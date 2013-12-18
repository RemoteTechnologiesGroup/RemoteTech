using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RemoteTech
{
    public static class UIPartActionMenuPatcher
    {
        public static void Wrap(Vessel parent, Action<BaseEvent, bool> pass)
        {
            var controller = UIPartActionController.Instance;
            if (!controller) return;
            var listFieldInfo = controller.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(fi => fi.FieldType == typeof(List<UIPartActionWindow>));

            var list = (List<UIPartActionWindow>)listFieldInfo.GetValue(controller);
            foreach (var window in list.Where(l => l.part.vessel == parent))
            {
                var itemsFieldInfo = window.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .First(fi => fi.FieldType == typeof(List<UIPartActionItem>));

                var item = (List<UIPartActionItem>)itemsFieldInfo.GetValue(window);
                foreach (var it in item)
                {
                    var button = it as UIPartActionEventItem;
                    if (button != null)
                    {
                        var partEventFieldInfo = button.Evt.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                            .First(fi => fi.FieldType == typeof(BaseEventDelegate));

                        var partEvent = (BaseEventDelegate)partEventFieldInfo.GetValue(button.Evt);
                        if (!partEvent.Method.GetCustomAttributes(typeof(KSPEvent), true).Any(a => ((KSPEvent)a).category.Contains("skip_control")))
                        {
                            bool ignore_delay = partEvent.Method.GetCustomAttributes(typeof(KSPEvent), true).Any(a => ((KSPEvent)a).category.Contains("skip_delay"));
                            var eventField = typeof(UIPartActionEventItem).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                .First(fi => fi.FieldType == typeof(BaseEvent));
                            eventField.SetValue(button, Wrapper.CreateWrapper(button.Evt, pass, ignore_delay));
                        }
                    }
                }
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
                BaseEvent new_event = new BaseEvent(original.listParent, original.name,
                                                                         wrapper.Invoke);
                new_event.OnLoad(cn);

                return new_event;
            }

            [KSPEvent(category="skip_control")]
            public void Invoke()
            {
                mPassthrough.Invoke(mEvent, mIgnoreDelay);
            }
        }
    }
}