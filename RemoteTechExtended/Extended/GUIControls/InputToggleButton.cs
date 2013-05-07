using System;
using UnityEngine;

namespace RemoteTechExtended
{
    public class InputToggleButton<T> : InputButton<T> {

        public bool IsActive { get; set; }

        public InputToggleButton(ClickDelegate<T> click, String name, T action, int val, int minVal, int maxVal
                                 , bool active) : base(click, name, action, val, minVal, maxVal) {
            this.IsActive = active;
        }

        public override void Draw() {
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
            if (GUILayout.Toggle(IsActive, " ", GUI.skin.toggle, GUILayout.Width(21.0f))) {
                IsActive = ~IsActive;
            }
            GUILayout.EndHorizontal();
        }
    }
}

