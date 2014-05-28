using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public static class RTGui
    {
        #region Area
        public static void Area(Action a, Rect rect) { Area(a, rect, GUIStyle.none); }
        public static void Area(Action a, Rect rect, GUIStyle style)
        {
            GUILayout.BeginArea(rect, style);
            a.Invoke();
            GUILayout.EndArea();
        }
        #endregion

        #region HorizontalBlock
        public static void HorizontalBlock(Action a, params GUILayoutOption[] options) { HorizontalBlock(a, GUIStyle.none, options); }
        public static void HorizontalBlock(Action a, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(style, options);
            a.Invoke();
            GUILayout.EndHorizontal();
        } 
        #endregion

        #region VerticalBlock
        public static void VerticalBlock(Action a, params GUILayoutOption[] options) { VerticalBlock(a, GUIStyle.none, options); }
        public static void VerticalBlock(Action a, GUIStyle style = null, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical();
            a.Invoke();
            GUILayout.EndVertical();
        } 
        #endregion

        #region ScrollViewBlock
        public static void ScrollViewBlock(ref Vector2 scrollPosition, Action a, params GUILayoutOption[] options) { ScrollViewBlock(ref scrollPosition, a, GUI.skin.scrollView, options); }
        public static void ScrollViewBlock(ref Vector2 scrollPosition, Action a, GUIStyle style, params GUILayoutOption[] options)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, style, options);
            a.Invoke();
            GUILayout.EndScrollView();
        } 
        #endregion
        public static void ClickableOverlay(Action onClick, Rect r)
        {
            if (Event.current.isMouse && Input.GetMouseButtonUp(0))
            {
                if (r.Contains(Event.current.mousePosition)) onClick.Invoke();
            }
        }
        public static void ClickableOverlay(Action<bool> onClick, Rect r)
        {
            ClickableOverlay(() => RTUtil.ExecuteNextFrame(() => onClick(true)), r);
        }

        #region Button (Texture2D)
        public static void Button(Texture2D icon, Action onClick, params GUILayoutOption[] options) { Button(icon, onClick, GUI.skin.button, options); }
        public static void Button(Texture2D icon, Action onClick, GUIStyle style, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(icon, style, options))
            {
                onClick.Invoke();
            }
        }
        public static void Button(Texture2D icon, Action<bool> onClick, params GUILayoutOption[] options) { Button(icon, onClick, GUI.skin.button, options); }
        public static void Button(Texture2D icon, Action<bool> onClick, GUIStyle style, params GUILayoutOption[] options)
        {
            Button(icon, () => RTUtil.ExecuteNextFrame(() => onClick(true)), style, options);
        }

        #endregion
        #region Button (String)
        public static void Button(String s, Action onClick, params GUILayoutOption[] options) { Button(s, onClick, GUI.skin.button, options); }
        public static void Button(String s, Action onClick, GUIStyle style, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(s, style, options))
            {
                onClick.Invoke();
            }
        } 
        public static void Button(String s, Action<bool> onClick, params GUILayoutOption[] options) { Button(s, onClick, GUI.skin.button, options); }
        public static void Button(String s, Action<bool> onClick, GUIStyle style, params GUILayoutOption[] options)
        {
            Button(s, () => RTUtil.ExecuteNextFrame(() => onClick(true)), style, options);
        }
        #endregion
        #region Button (GUIContent)

        public static void Button(GUIContent c, Action onClick, params GUILayoutOption[] options) { Button(c, onClick, GUI.skin.button, options); }
        public static void Button(GUIContent c, Action onClick, GUIStyle style, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(c, style, options))
            {
                onClick.Invoke();
            }
        }
        public static void Button(GUIContent c, Action<bool> onClick, params GUILayoutOption[] options) { Button(c, onClick, GUI.skin.button, options); }
        public static void Button(GUIContent c, Action<bool> onClick, GUIStyle style, params GUILayoutOption[] options)
        {
            Button(c, () => RTUtil.ExecuteNextFrame(() => onClick(true)), style, options);
        }
        #endregion

        #region HorizontalSlider

        public static void HorizontalSlider(ref float state, float min, float max, params GUILayoutOption[] options)
        {
            state = GUILayout.HorizontalSlider(state, min, max, options);
        }
        #endregion

        #region StateButton (GUIContent)
        public static void StateButton(GUIContent text, int state, int value, Action<int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton(GUIContent text, int state, int value, Action<int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(System.Object.Equals(state, value), text, style, options)) != System.Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? value : ~value);
            }
        }
        public static void StateButton(GUIContent text, int state, int value, Action<bool, int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton(GUIContent text, int state, int value, Action<bool, int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            StateButton(text, state, value, i => RTUtil.ExecuteNextFrame(() => onStateChange(true, i)), style, options);
        }
        public static void StateButton<T>(GUIContent text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton<T>(GUIContent text, T state, T value, Action<int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(System.Object.Equals(state, value), text, style, options)) != System.Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }
        public static void StateButton<T>(GUIContent text, T state, T value, Action<bool, int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton<T>(GUIContent text, T state, T value, Action<bool, int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            StateButton(text, state, value, i => RTUtil.ExecuteNextFrame(() => onStateChange(true, i)), style, options);
        }
        #endregion
        #region StateButton (String)
        public static void StateButton(String text, int state, int value, Action<int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton(String text, int state, int value, Action<int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(System.Object.Equals(state, value), text, style, options)) != System.Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? value : ~value);
            }
        }
        public static void StateButton(String text, int state, int value, Action<bool, int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton(String text, int state, int value, Action<bool, int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            StateButton(text, state, value, i => RTUtil.ExecuteNextFrame(() => onStateChange(true, i)), style, options);
        }

        public static void StateButton<T>(String text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton<T>(String text, T state, T value, Action<int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(System.Object.Equals(state, value), text, style, options)) != System.Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }
        public static void StateButton<T>(String text, T state, T value, Action<bool, int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton<T>(String text, T state, T value, Action<bool, int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            StateButton(text, state, value, i => RTUtil.ExecuteNextFrame(() => onStateChange(true, i)), style, options);
        }
        #endregion
        #region StateButton (Texture2D)

        public static void StateButton(Texture2D tex, int state, int value, Action<int> onStateChange, params GUILayoutOption[] options) { StateButton(tex, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton(Texture2D tex, int state, int value, Action<int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(System.Object.Equals(state, value), tex, style, options)) != System.Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? value : ~value);
            }
        }
        public static void StateButton(Texture2D tex, int state, int value, Action<bool, int> onStateChange, params GUILayoutOption[] options) { StateButton(tex, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton(Texture2D tex, int state, int value, Action<bool, int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            StateButton(tex, state, value, i => RTUtil.ExecuteNextFrame(() => onStateChange(true, i)), style, options);
        }
        public static void StateButton<T>(Texture2D text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton<T>(Texture2D text, T state, T value, Action<int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(System.Object.Equals(state, value), text, style, options)) != System.Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }
        public static void StateButton<T>(Texture2D text, T state, T value, Action<bool, int> onStateChange, params GUILayoutOption[] options) { StateButton(text, state, value, onStateChange, GUI.skin.button, options); }
        public static void StateButton<T>(Texture2D text, T state, T value, Action<bool, int> onStateChange, GUIStyle style, params GUILayoutOption[] options)
        {
            StateButton(text, state, value, i => RTUtil.ExecuteNextFrame(() => onStateChange(true, i)), style, options);
        }
        #endregion

        #region TextField
        public static void TextField(ref String text, params GUILayoutOption[] options) { TextField(ref text, GUIStyle.none, options); }
        public static void TextField(ref String text, GUIStyle style, params GUILayoutOption[] options)
        {
            text = GUILayout.TextField(text, style, options);
        } 
        #endregion

        public static bool ContainsMouse(this Rect window)
        {
            return window.Contains(new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y));
        }
    }
}
