using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RemoteTech.FlightComputer
{
    public static class UIPartActionMenuPatcher
    {
        public static Type[] AllowedFieldTypes = { typeof(UIPartActionToggle), typeof(UIPartActionFloatRange) };

        public static List<string> parsedPartActions = new List<string>();

        public static void WrapEvent(Vessel parent, Action<BaseEvent, bool> passthrough)
        {
            UIPartActionController controller = UIPartActionController.Instance;
            if (!controller) return;

            // Get the open context menus
            FieldInfo listFieldInfo = controller.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                      .First(fi => fi.FieldType == typeof(List<UIPartActionWindow>));

            List<UIPartActionWindow> openWindows = (List<UIPartActionWindow>)listFieldInfo.GetValue(controller);

            foreach (UIPartActionWindow window in openWindows.Where(window => window.part.vessel == parent))
            {
                // Get the list of all UIPartActionItem's
                FieldInfo itemsFieldInfo = typeof(UIPartActionWindow).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                           .First(fi => fi.FieldType == typeof(List<UIPartActionItem>));

                List<UIPartActionItem> partActionItems = (List<UIPartActionItem>)itemsFieldInfo.GetValue(window);

                // We only need the UIPartActionEventItem's
                UIPartActionItem[] actionEventButtons = partActionItems.Where(l => (l as UIPartActionEventItem) != null).ToArray();
                for (int i = 0; i < actionEventButtons.Count(); i++)
                {
                    // get event from button
                    UIPartActionEventItem button = (actionEventButtons[i] as UIPartActionEventItem);
                    BaseEvent originalEvent = button.Evt;                   

                    // Search for the BaseEventDelegate (BaseEvent.onEvent) field defined for the current BaseEvent type.
                    // Note that 'onEvent' is protected, so we have to go through reflection.
                    FieldInfo partEventFieldInfo = typeof(BaseEvent).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                                   .First(fi => fi.FieldType == typeof(BaseEventDelegate));

                    // Get the actual value of the 'onEvent' field 
                    BaseEventDelegate partEvent = (BaseEventDelegate)partEventFieldInfo.GetValue(originalEvent);

                    // Gets the method represented by the delegate and from this method returns an array of custom attributes applied to this member.
                    // Simply put, we want all [KSPEvent] attributes applied to the BaseEventDelegate.Method field.
                    object[] customAttributes = partEvent.Method.GetCustomAttributes(typeof(KSPEvent), true);

                    // Look for the custom attribute skip_control
                    bool skip_control = customAttributes.Any(a => ((KSPEvent)a).category.Contains("skip_control"));
                    if (!skip_control)
                    {
                        /*
                         * Override the old BaseEvent with our BaseEvent to the button
                         */                        

                        // fix problems with other mods (behavior not seen with KSP) when the customAttributes list is empty.
                        KSPEvent kspEvent = customAttributes.Count() == 0 ? WrappedEvent.KspEventFromBaseEvent(originalEvent) : (KSPEvent)customAttributes[0];

                        // Look for the custom attribute skip_delay
                        bool ignore_delay = customAttributes.Any(a => ((KSPEvent)a).category.Contains("skip_delay"));

                        // create the new BaseEvent
                        BaseEvent hookedEvent = EventWrapper.CreateWrapper(originalEvent, passthrough, ignore_delay, kspEvent);

                        // get the original event index in the event list
                        BaseEventList eventList = originalEvent.listParent;
                        int listIndex = eventList.IndexOf(originalEvent);

                        // remove the original event in the event list and add our hooked event
                        eventList.RemoveAt(listIndex);
                        eventList.Add(hookedEvent);

                        // get the baseEvent field from UIPartActionEventItem
                        FieldInfo baseEventField = typeof(UIPartActionEventItem).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                               .First(fi => fi.FieldType == typeof(BaseEvent));

                        // replace the button baseEvent value with our hooked event
                        baseEventField.SetValue(button, hookedEvent);
                    }
                }
            }
        }

        public static void WrapPartAction(Vessel parent, Action<BaseField, bool> passthrough)
        {
            UIPartActionController controller = UIPartActionController.Instance;
            if (!controller) return;

            // Get the open context menus
            FieldInfo listFieldInfo = controller.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                      .First(fi => fi.FieldType == typeof(List<UIPartActionWindow>));

            List<UIPartActionWindow> openWindows = (List<UIPartActionWindow>)listFieldInfo.GetValue(controller);

            foreach (UIPartActionWindow window in openWindows.Where(window => window.part.vessel == parent))
            {
                // Get the list of all UIPartActionItem's
                FieldInfo itemsFieldInfo = typeof(UIPartActionWindow).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                           .First(fi => fi.FieldType == typeof(List<UIPartActionItem>));

                List<UIPartActionItem> partActionItems = (List<UIPartActionItem>)itemsFieldInfo.GetValue(window);

                UIPartActionItem[] actionToogleButtons = partActionItems.Where(l => AllowedFieldTypes.Any(t => l.GetType() == t) && (l as UIPartActionFieldItem) != null).ToArray();
                for (int i = 0; i < actionToogleButtons.Count(); i++)
                {
                    UIPartActionFieldItem actionField = (actionToogleButtons[i] as UIPartActionFieldItem);
                    if (parsedPartActions.Contains(actionField.Field.name))
                        continue;
                    else
                        parsedPartActions.Add(actionField.Field.name);

                    object[] customAttributes = actionField.Field.FieldInfo.GetCustomAttributes(typeof(KSPField), true);

                    // Look for the custom attribute skip_control
                    bool skip_control = customAttributes.Any(attribute => ((KSPField)attribute).category.Contains("skip_control"));
                    if (!skip_control)
                    {
                        KSPField kspField = customAttributes.Count() == 0 ? FieldWrapper.KspFieldFromBaseField(actionField.Field) : (KSPField)customAttributes[0];


                        // Look for the custom attribute skip_delay
                        bool ignore_delay = customAttributes.Any(atrribute => ((KSPField)atrribute).category.Contains("skip_delay"));

                        var fieldWrapper = new FieldWrapper(actionField, kspField, passthrough, ignore_delay);
                    }
                }
            }
        }

        #region FieldWrapper        
        public class WrappedField : BaseField
        {
            /// <summary>
            /// The future value (when set by the flight computer) of the field.
            /// </summary>
            private object m_newValue;

            public WrappedField(BaseField baseField, KSPField field) : base(field, baseField.FieldInfo, baseField.host)
            {   
            }

            /// <summary>
            /// Gets or sets the future field value.
            /// </summary>
            public object NewValue
            {
                get { return m_newValue; }
                set { m_newValue = value; }
            }

            public Type NewValueType
            {
                get { return this.FieldInfo.FieldType; }
            }

            /// <summary>
            /// Effectively change the value of the underlying field.
            /// </summary>
            /// <remarks>This gets called by the flight computer either immediately if there's no delay or later if the command is queued.</remarks>
            public void Invoke()
            {
                if(m_newValue != null)
                    this.FieldInfo.SetValue(this.host, m_newValue);
            }
        }

        public class FieldWrapper
        {
            private Action<BaseField, bool> m_Passthrough;
            private bool m_IgnoreDelay;

            private UIPartActionFieldItem m_UiPartAction;

            private WrappedField m_WrappedField;

            private Action<float> m_delayInvoke;
            private object m_lastNewValue;

            public FieldWrapper(UIPartActionFieldItem uiPartAction, KSPField kspField, Action<BaseField, bool> passthrough, bool ignore_delay)
            {
                m_UiPartAction = uiPartAction;
                SetDefaultListener();

                BaseField baseField = uiPartAction.Field;
                if(kspField == null)
                {
                    kspField = KspFieldFromBaseField(baseField);
                }
                
                m_Passthrough = passthrough;
                m_IgnoreDelay = ignore_delay;
                m_WrappedField = new WrappedField(baseField, kspField);
            }

            public void Invoke()
            {
                if (m_Passthrough != null)
                {
                    m_WrappedField.NewValue = m_lastNewValue;
                    m_Passthrough.Invoke(m_WrappedField, m_IgnoreDelay);
                }
            }

            public void DelayInvoke(float waitTime)
            {
                HighLogic.fetch.StartCoroutine(DelayInvokeCoroutine(waitTime));
            }

            private IEnumerator DelayInvokeCoroutine(float waitTime)
            {
                yield return new WaitForSeconds(waitTime);
                if (m_delayInvoke != null)
                    m_delayInvoke = null;
                Invoke();
            }

            public static KSPField KspFieldFromBaseField(BaseField baseField)
            {
                var ksp_field = new KSPField();
                ksp_field.isPersistant = baseField.isPersistant;
                ksp_field.guiActive = baseField.guiActive;
                ksp_field.guiActiveEditor = baseField.guiActiveEditor;
                ksp_field.guiName = baseField.guiName;
                ksp_field.guiUnits = baseField.guiUnits;
                ksp_field.guiFormat = baseField.guiFormat;
                ksp_field.category = baseField.category;
                ksp_field.advancedTweakable = baseField.advancedTweakable;

                return ksp_field;
            }

            private void SetDefaultListener()
            {
                switch (m_UiPartAction.GetType().Name)
                {
                    case nameof(UIPartActionToggle):
                        var part_toggle = m_UiPartAction as UIPartActionToggle;
                        if (part_toggle != null)
                        {
                            part_toggle.toggle.onToggle.RemoveListener(part_toggle.OnTap);
                            part_toggle.toggle.onToggle.AddListener(GetNewValue0);
                        }
                        break;

                    case nameof(UIPartActionFloatRange):
                        var part_float = m_UiPartAction as UIPartActionFloatRange;
                        if(part_float != null)
                        {
                            part_float.slider.onValueChanged.RemoveAllListeners();
                            part_float.slider.onValueChanged.AddListener(GetNewValueFloat);

                        }
                        break;
                }
            }

            private void GetNewValue0()
            {
                GetNewValue();
            }

            private void GetNewValueFloat(float obj)
            {
                GetNewValue();
            }

            private void GetNewValue()
            {
                switch (m_UiPartAction.GetType().Name)
                {
                    case nameof(UIPartActionToggle):
                        var part_toggle = m_UiPartAction as UIPartActionToggle;
                        if (part_toggle != null)
                        {
                            var uiToggle = (part_toggle.Control as UI_Toggle);
                            if (uiToggle != null)
                            {
                                m_lastNewValue = part_toggle.toggle.state ^ uiToggle.invertButton;
                                // invoke now
                                Invoke();
                            }
                        }
                        break;

                    case nameof(UIPartActionFloatRange):
                        var part_float = m_UiPartAction as UIPartActionFloatRange;
                        if (part_float != null)
                        {
                            var uiFloatRange = (part_float.Control as UI_FloatRange);
                            if (uiFloatRange != null)
                            {
                                // get current value
                                float currentValue = float.NaN;
                                bool isModule = (part_float.PartModule != null);
                                if (isModule)
                                    currentValue = part_float.Field.GetValue<float>(part_float.PartModule);
                                else
                                    currentValue = part_float.Field.GetValue<float>(part_float.Part);

                                // get new value
                                float newValue = HandleFloatRange(currentValue, uiFloatRange, part_float.slider);
                                if (!float.IsNaN(newValue))
                                {
                                    m_lastNewValue = newValue;
                                    if (m_delayInvoke == null)
                                    {
                                        m_delayInvoke = new Action<float>(DelayInvoke);
                                        m_delayInvoke(0.5f);
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            private float HandleFloatRange(float fieldValue, UI_FloatRange uiFloatRange, UnityEngine.UI.Slider slider)
            {
                var lerpedValue = Mathf.Lerp(uiFloatRange.minValue, uiFloatRange.maxValue, slider.value);
                var moddedValue = lerpedValue % uiFloatRange.stepIncrement;
                float num = fieldValue;
                if (moddedValue != 0f)
                {
                    if (moddedValue < uiFloatRange.stepIncrement * 0.5f)
                    {
                        fieldValue = lerpedValue - moddedValue;
                    }
                    else
                    {
                        fieldValue = lerpedValue + (uiFloatRange.stepIncrement - moddedValue);
                    }
                }
                else
                {
                    fieldValue = lerpedValue;
                }
                slider.value = Mathf.InverseLerp(uiFloatRange.minValue, uiFloatRange.maxValue, fieldValue);
                fieldValue = (float)Math.Round((double)fieldValue, 5);
                if (Mathf.Abs(fieldValue - num) > uiFloatRange.stepIncrement * 0.98f)
                {
                    return fieldValue;
                }

                return float.NaN;
            }
        }
        #endregion

        #region EventWrapper
        public class WrappedEvent : BaseEvent
        {
            private BaseEvent originalEvent;

            public WrappedEvent(BaseEvent originalEvent, BaseEventList baseParentList, string name, BaseEventDelegate baseActionDelegate, KSPEvent kspEvent)
                : base(baseParentList, name, baseActionDelegate, kspEvent)
            {
                this.originalEvent = originalEvent;
            }

            public void InvokeOriginalEvent()
            {
                originalEvent.Invoke();
            }

            /// <summary>
            /// Given a BaseEvent, obtain a KSPEvent.
            /// Note : This is used in UIPartActionMenuPatcher.Wrap in case there no KSPEvent in the custom attributes of the BaseEventDelegate from the button event.
            /// </summary>
            /// <param name="baseEvent">BaseEvent from which to abtain a KSPEvent.</param>
            /// <returns>KSPEvent instance from the BaseEvent parameter.</returns>
            public static KSPEvent KspEventFromBaseEvent(BaseEvent baseEvent)
            {
                KSPEvent kspEvent = new KSPEvent();
                kspEvent.active = baseEvent.active;
                kspEvent.guiActive = baseEvent.guiActive;
                kspEvent.requireFullControl = baseEvent.requireFullControl;
                kspEvent.guiActiveEditor = baseEvent.guiActiveEditor;
                kspEvent.guiActiveUncommand = baseEvent.guiActiveUncommand;
                kspEvent.guiIcon = baseEvent.guiIcon;
                kspEvent.guiName = baseEvent.guiName;
                kspEvent.category = baseEvent.category;
                kspEvent.advancedTweakable = baseEvent.advancedTweakable;
                kspEvent.guiActiveUnfocused = baseEvent.guiActiveUnfocused;
                kspEvent.unfocusedRange = baseEvent.unfocusedRange;
                kspEvent.externalToEVAOnly = baseEvent.externalToEVAOnly;
                kspEvent.isPersistent = baseEvent.isPersistent;

                return kspEvent;
            }
        }

        private class EventWrapper
        {
            private Action<BaseEvent, bool> mPassthrough;
            private BaseEvent mEvent;
            private bool mIgnoreDelay;

            private EventWrapper(BaseEvent original, Action<BaseEvent, bool> passthrough, bool ignore_delay)
            {
                mPassthrough = passthrough;
                mEvent = original;
                mIgnoreDelay = ignore_delay;
            }

            public static BaseEvent CreateWrapper(BaseEvent original, Action<BaseEvent, bool> passthrough, bool ignore_delay, KSPEvent kspEvent)
            {
                // Create a new config node and fill this config node with the original base event with the values
                ConfigNode cn = new ConfigNode();
                original.OnSave(cn);

                // create the wrapper (used solely for its Invoke() method)
                // this class keeps the:
                // * passthrough event (leading to the ModuleSPU.InvokeEvent() method)
                // * the original event (button click event)
                // * the ignore delay boolean value (true if the event ignore delay, false otherwise)
                EventWrapper wrapper = new EventWrapper(original, passthrough, ignore_delay);
                // Create a new event, its main features are:
                // 1. It retains its original base event invokable method: invokable directly through its InvokeOriginalEvent() method [useful for other mods, e.g. kOS]
                // 2. Its new invoke() method which is in this wrapper class and decorated with and new KSPEvent category, namely "skip_control" (meaning we have already seen this event).
                BaseEvent new_event = new WrappedEvent(original, original.listParent, original.name, wrapper.Invoke, kspEvent);

                // load the original base event values into the new base event
                new_event.OnLoad(cn);

                return new_event;
            }

            [KSPEvent(category = "skip_control")]
            public void Invoke()
            {
                // call the passthrough event, leading to call the ModuleSPU.InvokeEvent() method
                mPassthrough.Invoke(mEvent, mIgnoreDelay);
            }
        }
        #endregion
    }
}