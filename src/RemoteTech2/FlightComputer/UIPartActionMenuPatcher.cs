using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RemoteTech
{
    public static class UIPartActionMenuPatcher
    {
        public static void Patch(IVessel vessel)
        {
            UIPartActionMenuPatcher.Wrap(vessel, (e, ignore_delay) =>
            {
                if (RTCore.Instance == null)
                {
                    e.Invoke();
                    return;
                }
                var v = RTCore.Instance.Vessels.ActiveVessel;
                var vs = RTCore.Instance.Satellites[v];
                if (v == null || v.IsEVA || vs == null || vs.HasLocalControl)
                {
                    e.Invoke();
                    return;
                }
                if (vs.FlightComputer != null && vs.FlightComputer.InputAllowed)
                {
                    if (ignore_delay)
                    {
                        e.Invoke();
                    }
                    else
                    {
                        vs.SignalProcessor.FlightComputer.Enqueue(EventCommand.Event(e));
                    }
                }
                else
                {
                    ScreenMessages.PostScreenMessage(new ScreenMessage("No connection to send command on.", 4.0f, ScreenMessageStyle.UPPER_LEFT));
                }
            });
        }
        private static void Wrap(IVessel parent, Action<BaseEvent, bool> pass)
        {
            var controller = UIPartActionController.Instance;
            if (!controller) return;
            var listFieldInfo = controller.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(fi => fi.FieldType == typeof(List<UIPartActionWindow>));

            var list = (List<UIPartActionWindow>)listFieldInfo.GetValue(controller);
            foreach (var window in list.Where(l => (VesselProxy) l.part.vessel == parent))
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
                BaseEvent new_event = new BaseEvent(original.listParent, original.name, wrapper.Invoke);
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