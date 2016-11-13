using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech.Common.Utils
{
    public static class GuiUtil
    {
        /// <summary>This time member is needed to debounce the RepeatButton</summary>
        private static double _timeDebouncer = (HighLogic.LoadedSceneHasPlanetarium) ? TimeUtil.GameTime : 0;

        public static void Button(Texture2D icon, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(icon, options))
            {
                onClick.Invoke();
            }
        }

        public static void Button(string text, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                onClick.Invoke();
            }
        }

        public static void Button(GUIContent text, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                onClick.Invoke();
            }
        }

        /// <summary>
        /// Draws a repeat button. If you hold the mouse click the <paramref name="onClick"/>-callback
        /// will be triggered at least every 0.05 seconds.
        /// </summary>
        /// <param name="text">Text for the button</param>
        /// <param name="onClick">Callback to trigger for every repeat</param>
        /// <param name="options">GUILayout params</param>
        public static void RepeatButton(string text, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.RepeatButton(text, options) && (_timeDebouncer + 0.05) < TimeUtil.GameTime)
            {
                onClick.Invoke();
                // set the new time to the debouncer
                _timeDebouncer = TimeUtil.GameTime;
            }
        }

        /// <summary>
        /// Draw a fake toggle button. It is an action button with a toggle functionality. When <param name="state" /> and
        /// <param name="value" /> are equal the background of the button will change to black.
        /// </summary>
        public static void FakeStateButton(GUIContent text, Action onClick, int state, int value, params GUILayoutOption[] options)
        {
            var pushBgColor = GUI.backgroundColor;
            if (state == value)
            {
                GUI.backgroundColor = Color.black;
            }

            if (GUILayout.Button(text, options))
            {
                onClick.Invoke();
            }
            GUI.backgroundColor = pushBgColor;
        }

        public static void HorizontalSlider(ref float state, float min, float max, params GUILayoutOption[] options)
        {
            state = GUILayout.HorizontalSlider(state, min, max, options);
        }

        public static void GroupButton(int wide, string[] text, ref int group, params GUILayoutOption[] options)
        {
            group = GUILayout.SelectionGrid(group, text, wide, options);
        }

        public static void GroupButton(int wide, string[] text, ref int group, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            int group2;
            if ((group2 = GUILayout.SelectionGrid(group, text, wide, options)) != group)
            {
                group = group2;
                onStateChange.Invoke(group2);
            }
        }

        public static void StateButton(GUIContent text, int state, int value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Equals(state, value), text, GUI.skin.button, options)) != Equals(state, value))
            {
                onStateChange.Invoke(result ? value : ~value);
            }
        }

        public static void StateButton<T>(GUIContent text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Equals(state, value), text, GUI.skin.button, options)) != Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }

        public static void StateButton<T>(string text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Equals(state, value), text, GUI.skin.button, options)) != Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }

        public static void TextField(ref string text, params GUILayoutOption[] options)
        {
            text = GUILayout.TextField(text, options);
        }

        /// <summary>
        /// Draws a Textfield with a functionality to use the mouse wheel to trigger
        /// the events <paramref name="onWheelDown"/> and <paramref name="onWheelUp"/>.
        /// The callbacks will only be triggered if the textfield is focused while using
        /// the mouse wheel.
        /// </summary>
        /// <param name="text">Reference to the input value</param>
        /// <param name="fieldName">Name for this field</param>
        /// <param name="onWheelDown">Action trigger for the mousewheel down event</param>
        /// <param name="onWheelUp">Action trigger for the mousewheel up event</param>
        /// <param name="options">GUILayout params</param>
        public static void MouseWheelTriggerField(ref string text, string fieldName, Action onWheelDown, Action onWheelUp, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(fieldName);
            text = GUILayout.TextField(text, options);

            // Current textfield under control?
            if ((GUI.GetNameOfFocusedControl() == fieldName))
            {
                if (Input.GetAxis("Mouse ScrollWheel") > 0 && (_timeDebouncer + 0.05) < TimeUtil.GameTime)
                {
                    onWheelDown.Invoke();
                    _timeDebouncer = TimeUtil.GameTime;
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0 && (_timeDebouncer + 0.05) < TimeUtil.GameTime)
                {
                    onWheelUp.Invoke();
                    _timeDebouncer = TimeUtil.GameTime;
                }
            }
        }

        public static bool ContainsMouse(this Rect window)
        {
            return window.Contains(new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y));
        }

        public static IEnumerable<Transform> FindTransformsWithCollider(Transform input)
        {
            if (input.GetComponent<Collider>() != null)
            {
                yield return input;
            }

            foreach (Transform t in input)
            {
                foreach (var x in FindTransformsWithCollider(t))
                {
                    yield return x;
                }
            }
        }

        public static void ScreenMessage(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4.0f, ScreenMessageStyle.UPPER_LEFT));
        }
    }

}
