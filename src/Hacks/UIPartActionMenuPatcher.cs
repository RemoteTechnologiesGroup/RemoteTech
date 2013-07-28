using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RemoteTech {
    public static class UIPartActionMenuPatcher {
        public static void Wrap(Vessel parent, Action<BaseEvent> pass) {
            var controller = UIPartActionController.Instance;
            if (!controller)
                return;
            var listFieldInfo = controller.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(fi => fi.FieldType == typeof(List<UIPartActionWindow>));

            var list = (List<UIPartActionWindow>) listFieldInfo.GetValue(controller);
            foreach (var window in list.Where(l => l.part.vessel == parent)) {
                var itemsFieldInfo = window.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .First(fi => fi.FieldType == typeof(List<UIPartActionItem>));

                var item = (List<UIPartActionItem>) itemsFieldInfo.GetValue(window);
                foreach (var it in item) {
                    var button = it as UIPartActionModuleButton;
                    if (button != null) {
                        
                        var partEventFieldInfo = button.partEvent.GetType()
                            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                            .First(fi => fi.FieldType == typeof(BaseEventDelegate));

                        var partEvent = (BaseEventDelegate)
                            partEventFieldInfo.GetValue(button.partEvent);
                        if (partEvent.Method.GetCustomAttributes(
                                typeof(IgnoreSignalDelayAttribute), true).Length == 0) {
                            button.partEvent = Wrapper.Wrap(button.partEvent, pass); 
                        }
                    }
                }
            }
        }

        private class Wrapper {
            private Action<BaseEvent> mPassthrough;
            private BaseEvent mEvent;

            private Wrapper(BaseEvent original, Action<BaseEvent> passthrough) {
                mPassthrough = passthrough;
                mEvent = original;
            }

            public static BaseEvent Wrap(BaseEvent original, Action<BaseEvent> passthrough) {
                ConfigNode cn = new ConfigNode();
                original.OnSave(cn);
                Wrapper wrapper = new Wrapper(original, passthrough);
                BaseEvent new_event = new BaseEvent(original.listParent, original.name, 
                                                                         wrapper.Invoke);
                new_event.OnLoad(cn);

                return new_event;
            }

            [IgnoreSignalDelayAttribute]
            public void Invoke() {
                mPassthrough.Invoke(mEvent);
            }
        }
    }
    public class IgnoreSignalDelayAttribute : System.Attribute { }
}