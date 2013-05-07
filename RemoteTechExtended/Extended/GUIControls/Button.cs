using System;
using UnityEngine;

namespace RemoteTechExtended
{
    public class Button<T> {
        public delegate void ClickDelegate<TAction>(TAction action);

        // Properties
        public String Name { get; set; }

        // Fields
        ClickDelegate<T> mClickDelegate;
        T mAction;

        public Button(ClickDelegate<T> click, String name, T action) {
            this.mClickDelegate = click;
            this.mAction = action;
            this.Name = name;
        }

        public virtual void Draw() {
            GUILayout.BeginHorizontal();
            GUILayout.Button(Name, GUI.skin.textField, GUILayout.Width(250.0f));
            GUILayout.EndHorizontal();
        }

        public void Fire() {
            if (mClickDelegate != null) {
                mClickDelegate(mAction);
            }
        }
    }

}

