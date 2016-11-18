using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using UnityEngine;

namespace RemoteTech.FlightComputer
{
    public static class UIPartActionMenuPatcher
    {
        // UI types that we are actually hooking
        public static Type[] UIPartActionFieldItemAllowedTypes = { typeof(UIPartActionToggle), typeof(UIPartActionFloatRange), typeof(UIPartActionCycle) };

        public static List<string> ParsedPartActions = new List<string>();

        public static void WrapPartActionEventItem(Part part, Action<BaseEvent, bool> passthrough)
        {
            var controller = UIPartActionController.Instance;
            if (!controller)
                return;

            // get the part action window corresponding to the part
            var window = controller.GetItem(part);
            if (window == null)
                return;

            // get all the items that makes this window (toggle buttons, sliders, etc.)
            var partActionItems = window.ListItems;

            // loop through all of those UI components
            for (var i = 0; i < partActionItems.Count(); i++)
            {
                // check that the part action item is actually a UIPartActionFieldItem (it could be a UIPartActionEventItem)
                var uiPartActionEventItem = (partActionItems[i] as UIPartActionEventItem);
                if (uiPartActionEventItem == null)
                    continue;

                // get event from button
                BaseEvent originalEvent = uiPartActionEventItem.Evt;                   

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
                bool skipControl = customAttributes.Any(a => ((KSPEvent)a).category.Contains("skip_control"));
                if (skipControl)
                    continue;
                
                /*
                 * Override the old BaseEvent with our BaseEvent to the button
                 */

                // fix problems with other mods (behavior not seen with KSP) when the customAttributes list is empty.
                KSPEvent kspEvent = !customAttributes.Any() ? WrappedEvent.KspEventFromBaseEvent(originalEvent) : (KSPEvent)customAttributes[0];

                // Look for the custom attribute skip_delay
                bool ignoreDelay = customAttributes.Any(a => ((KSPEvent)a).category.Contains("skip_delay"));

                // create the new BaseEvent
                BaseEvent hookedEvent = EventWrapper.CreateWrapper(originalEvent, passthrough, ignoreDelay, kspEvent);

                // get the original event index in the event list
                BaseEventList eventList = originalEvent.listParent;
                int listIndex = eventList.IndexOf(originalEvent);

                // remove the original event in the event list and add our hooked event
                eventList.RemoveAt(listIndex);
                eventList.Add(hookedEvent);

                // get the baseEvent field from UIPartActionEventItem (note: this is uiPartActionEventItem.Evt, but we can't set its value...)
                FieldInfo baseEventField = typeof(UIPartActionEventItem).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .First(fi => fi.FieldType == typeof(BaseEvent));

                // replace the button baseEvent value with our hooked event
                baseEventField.SetValue(uiPartActionEventItem, hookedEvent);
            }
        }

        public static void WrapPartActionFieldItem(Part part, Action<BaseField, bool> passthrough)
        {
            var controller = UIPartActionController.Instance;
            if (!controller)
                return;

            // get the part action window corresponding to the part
            var window = controller.GetItem(part);
            if (window == null)
                return;

            // get all the items that makes this window (toggle buttons, sliders, etc.)
            var partActionItems = window.ListItems;

            // loop through all of those UI components
            for (var i = 0; i < partActionItems.Count(); i++)
            {
                // check that the part action item is actually a UIPartActionFieldItem (it could be a UIPartActionEventItem)
                var uiPartActionFieldItem = (partActionItems[i] as UIPartActionFieldItem);
                if (uiPartActionFieldItem == null)
                    continue;

                // now check that the UIPartActionFieldItem type (e.g UIPartActionToggle; UIPartActionCycle; UIPartActionFloatRange, etc.) 
                // is actually something we can handle.
                if (UIPartActionFieldItemAllowedTypes.All(type => uiPartActionFieldItem.GetType() != type))
                    continue;

                var fieldWrapper = new FieldWrapper(uiPartActionFieldItem, passthrough, false);
            }
            
        }

        #region FieldWrapper        
        public class WrappedField : BaseField
        {
            public WrappedField(BaseField baseField, KSPField field) : base(field, baseField.FieldInfo, baseField.host)
            {   
            }

            /// <summary>Gets or sets the future field value.</summary>
            public object NewValue { get; set; }

            public Type NewValueType => FieldInfo.FieldType;

            public bool NewValueFromString(string stringValue)
            {
                if (string.IsNullOrEmpty(stringValue))
                    return false;

                try
                {
                    if (NewValueType != typeof(string))
                        NewValue = Convert.ChangeType(NewValue, this.NewValueType, CultureInfo.InvariantCulture);
                    else
                        NewValue = stringValue;

                    return true;
                }
                catch(Exception ex) when(ex is InvalidCastException || ex is FormatException || ex is OverflowException)
                {
                    RTLog.Notify("WrappedField.NewValueFromString() : can't convert {0} to new type: {1} ; for field name: {2}", stringValue, NewValueType, FieldInfo.Name);
                    return false;
                }
            }

            /// <summary>
            /// Effectively change the value of the underlying field.
            /// </summary>
            /// <remarks>This gets called by the flight computer either immediately if there's no delay or later if the command is queued.</remarks>
            public void Invoke()
            {
                if(NewValue != null)
                    FieldInfo.SetValue(host, NewValue);
            }

            public static KSPField KspFieldFromBaseField(BaseField baseField)
            {
                var kspField = new KSPField
                {
                    isPersistant = baseField.isPersistant,
                    guiActive = baseField.guiActive,
                    guiActiveEditor = baseField.guiActiveEditor,
                    guiName = baseField.guiName,
                    guiUnits = baseField.guiUnits,
                    guiFormat = baseField.guiFormat,
                    category = baseField.category,
                    advancedTweakable = baseField.advancedTweakable
                };

                return kspField;
            }
        }

        public class FieldWrapper
        {
            private readonly Action<BaseField, bool> _passthrough;
            private readonly bool _ignoreDelay;
            private readonly UIPartActionFieldItem _uiPartAction;
            private readonly WrappedField _wrappedField;

            private Action<float> _delayInvoke;
            private object _lastNewValue;

            public FieldWrapper(UIPartActionFieldItem uiPartAction, Action<BaseField, bool> passthrough, bool ignoreDelay)
            {
                _uiPartAction = uiPartAction;
                SetDefaultListener();
               
                _passthrough = passthrough;
                _ignoreDelay = ignoreDelay;
                _wrappedField = new WrappedField(uiPartAction.Field, WrappedField.KspFieldFromBaseField(uiPartAction.Field));
            }

            public void Invoke()
            {
                if (_passthrough == null)
                    return;

                _wrappedField.NewValue = _lastNewValue;
                _passthrough.Invoke(_wrappedField, _ignoreDelay);
            }

            public void DelayInvoke(float waitTime)
            {
                HighLogic.fetch.StartCoroutine(DelayInvokeCoroutine(waitTime));
            }

            private IEnumerator DelayInvokeCoroutine(float waitTime)
            {
                yield return new WaitForSeconds(waitTime);
                _delayInvoke = null;
                Invoke();
            }

            private void SetDefaultListener()
            {
                switch (_uiPartAction.GetType().Name)
                {
                    case nameof(UIPartActionCycle):
                        {
                            var partCycle = _uiPartAction as UIPartActionCycle;
                            if (partCycle != null)
                            {
                                partCycle.toggle.onToggle.RemoveListener(partCycle.OnTap);
                                partCycle.toggle.onToggle.AddListener(GetNewValue0);
                            }
                        }
                        break;

                    case nameof(UIPartActionToggle):
                        {
                            var partToggle = _uiPartAction as UIPartActionToggle;
                            if (partToggle != null)
                            {
                                partToggle.toggle.onToggle.RemoveListener(partToggle.OnTap);
                                partToggle.toggle.onToggle.AddListener(GetNewValue0);
                            }
                        }
                        break;

                    case nameof(UIPartActionFloatRange):
                        var partFloat = _uiPartAction as UIPartActionFloatRange;
                        if(partFloat != null)
                        {
                            partFloat.slider.onValueChanged.RemoveAllListeners();
                            partFloat.slider.onValueChanged.AddListener(GetNewValueFloat);

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
                switch (_uiPartAction.GetType().Name)
                {
                    // Handle toggle button, usually just a ON/OFF feature
                    case nameof(UIPartActionToggle):
                        {
                            var partToggle = _uiPartAction as UIPartActionToggle;
                            if (partToggle != null)
                            {
                                var uiToggle = (partToggle.Control as UI_Toggle);
                                if (uiToggle != null)
                                {
                                    _lastNewValue = partToggle.toggle.state ^ uiToggle.invertButton;
                                    // invoke now
                                    Invoke();
                                }
                            }
                        }
                        break;

                    // handle cycle button
                    case nameof(UIPartActionCycle):
                        {
                            var partCycle = _uiPartAction as UIPartActionCycle;
                            if (partCycle != null)
                            {
                                var uiCycle = (partCycle.Control as UI_Cycle);
                                if (uiCycle != null)
                                {
                                    // get current value
                                    int currentValue;
                                    if (partCycle.PartModule != null)
                                        currentValue = partCycle.Field.GetValue<int>(partCycle.PartModule);
                                    else
                                        currentValue = partCycle.Field.GetValue<int>(partCycle.Part);

                                    _lastNewValue = (currentValue + 1) % uiCycle.stateNames.Length;
                                    // invoke now
                                    Invoke();
                                }
                            }
                        }
                        break;

                    // handle sliders (using float value)
                    case nameof(UIPartActionFloatRange):
                        {
                            var partFloat = _uiPartAction as UIPartActionFloatRange;
                            if (partFloat != null)
                            {
                                var uiFloatRange = (partFloat.Control as UI_FloatRange);
                                if (uiFloatRange != null)
                                {
                                    // get current value
                                    float currentValue;
                                    if (partFloat.PartModule != null)
                                        currentValue = partFloat.Field.GetValue<float>(partFloat.PartModule);
                                    else
                                        currentValue = partFloat.Field.GetValue<float>(partFloat.Part);

                                    // get new value
                                    var newValue = HandleFloatRange(currentValue, uiFloatRange, partFloat.slider);
                                    if (!float.IsNaN(newValue))
                                    {
                                        _lastNewValue = newValue;
                                        if (_delayInvoke == null)
                                        {
                                            // invoke later
                                            _delayInvoke = DelayInvoke;
                                            _delayInvoke(0.5f);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            private static float HandleFloatRange(float fieldValue, UI_FloatRange uiFloatRange, UnityEngine.UI.Slider slider)
            {
                var lerpedValue = Mathf.Lerp(uiFloatRange.minValue, uiFloatRange.maxValue, slider.value);
                var moddedValue = lerpedValue % uiFloatRange.stepIncrement;
                var num = fieldValue;
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
                fieldValue = (float)Math.Round(fieldValue, 5);
                return Mathf.Abs(fieldValue - num) > uiFloatRange.stepIncrement * 0.98f ? fieldValue : float.NaN;
            }
        }
        #endregion

        #region EventWrapper
        public class WrappedEvent : BaseEvent
        {
            private readonly BaseEvent _originalEvent;

            public WrappedEvent(BaseEvent originalEvent, BaseEventList baseParentList, string name, BaseEventDelegate baseActionDelegate, KSPEvent kspEvent)
                : base(baseParentList, name, baseActionDelegate, kspEvent)
            {
                _originalEvent = originalEvent;
            }

            public void InvokeOriginalEvent()
            {
                _originalEvent.Invoke();
            }

            /// <summary>
            /// Given a BaseEvent, obtain a KSPEvent.
            /// Note : This is used in UIPartActionMenuPatcher.Wrap in case there no KSPEvent in the custom attributes of the BaseEventDelegate from the button event.
            /// </summary>
            /// <param name="baseEvent">BaseEvent from which to obtain a KSPEvent.</param>
            /// <returns>KSPEvent instance from the BaseEvent parameter.</returns>
            public static KSPEvent KspEventFromBaseEvent(BaseEvent baseEvent)
            {
                var kspEvent = new KSPEvent
                {
                    active = baseEvent.active,
                    guiActive = baseEvent.guiActive,
                    requireFullControl = baseEvent.requireFullControl,
                    guiActiveEditor = baseEvent.guiActiveEditor,
                    guiActiveUncommand = baseEvent.guiActiveUncommand,
                    guiIcon = baseEvent.guiIcon,
                    guiName = baseEvent.guiName,
                    category = baseEvent.category,
                    advancedTweakable = baseEvent.advancedTweakable,
                    guiActiveUnfocused = baseEvent.guiActiveUnfocused,
                    unfocusedRange = baseEvent.unfocusedRange,
                    externalToEVAOnly = baseEvent.externalToEVAOnly,
                    isPersistent = baseEvent.isPersistent
                };

                return kspEvent;
            }
        }

        public class EventWrapper
        {
            private readonly Action<BaseEvent, bool> _passthrough;
            private readonly BaseEvent _event;
            private readonly bool _ignoreDelay;

            private EventWrapper(BaseEvent original, Action<BaseEvent, bool> passthrough, bool ignoreDelay)
            {
                _passthrough = passthrough;
                _event = original;
                _ignoreDelay = ignoreDelay;
            }

            public static BaseEvent CreateWrapper(BaseEvent original, Action<BaseEvent, bool> passthrough, bool ignoreDelay, KSPEvent kspEvent)
            {
                // Create a new configuration node and fill this node with the original base event with the values
                ConfigNode cn = new ConfigNode();
                original.OnSave(cn);

                // create the wrapper (used solely for its Invoke() method)
                // this class keeps the:
                // * pass through event (leading to the ModuleSPU.InvokeEvent() method)
                // * the original event (button click event)
                // * the ignore delay boolean value (true if the event ignore delay, false otherwise)
                EventWrapper wrapper = new EventWrapper(original, passthrough, ignoreDelay);
                // Create a new event, its main features are:
                // 1. It retains its original base event invokable method: invokable directly through its InvokeOriginalEvent() method [useful for other mods, e.g. kOS]
                // 2. Its new invoke() method which is in this wrapper class and decorated with and new KSPEvent category, namely "skip_control" (meaning we have already seen this event).
                BaseEvent newEvent = new WrappedEvent(original, original.listParent, original.name, wrapper.Invoke, kspEvent);

                // load the original base event values into the new base event
                newEvent.OnLoad(cn);

                return newEvent;
            }

            [KSPEvent(category = "skip_control")]
            public void Invoke()
            {
                // call the pass through event, leading to call the ModuleSPU.InvokeEvent() method
                _passthrough.Invoke(_event, _ignoreDelay);
            }
        }
        #endregion
    }
}