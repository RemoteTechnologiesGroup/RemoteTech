using System;
using UnityEngine;
using System.Collections.Generic;

namespace RemoteTechExtended
{
    public class InputButton<T> : Button<T> {

        // Properties
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int Value { 
            get {
                return MaxValue;
            }
            set {
                if (value > MaxValue) {
                    mValue = MaxValue;
                } else if (value < MinValue) {
                    mValue = MinValue;
                } else {
                    mValue = value;
                }
            }
        }

        // Fields
        int mValue;

        public InputButton(ClickDelegate<T> click, String name, T action, int val, int minVal, int maxVal)
            : base(click, name, action) {
            this.Value = val;
            this.MinValue = minVal;
            this.MaxValue = maxVal;
        }

        public virtual void Draw() {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Name, GUI.skin.textField, GUILayout.Width(100.0f))) {
                Fire();
            }
            GUILayout.Label(Value, GUI.skin.textField, GUILayout.Width(50.0f));
            if (GUILayout.Button("+", GUI.skin.textField, GUILayout.Width(21.0f))) {
                Value = Value++;
            }
            if (GUILayout.Button("-", GUI.skin.textField, GUILayout.Width(21.0f))) {
                Value = Value--;
            }
            GUILayout.EndHorizontal();
        }
    }
}

