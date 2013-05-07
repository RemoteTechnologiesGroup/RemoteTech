using System;
using UnityEngine;
using System.Collections.Generic;

namespace RemoteTechExtended
{
    public class DelayedToggleButton<T> : Button<T> {

        public struct StateStamp {
            public readonly long Timestamp;
            public readonly bool IsActive;
            
            public StateStamp(long timestamp, bool isActive) {
                this.Timestamp = timestamp;
                this.IsActive = isActive;
            }
        }

        // Properties
        public bool IsActive { get; private set; }

        // Fields
        Queue<StateStamp> mStateQueue = new Queue<StateStamp>();

        public DelayedToggleButton(Button::ClickDelegate<T> click, String name, T action)
            : base(click, name, action) {
            this.IsActive = false;
        }

        public void Draw() {
            while (mStateQueue.Peek().Timestamp < Common.GetGameTime()) {
                IsActive = mStateQueue.Dequeue().IsActive;
            }
            String time = mStateQueue.Count > 0 ? 
                Common.FormatTime(mStateQueue.Peek().Timestamp - Common.GetGameTime()) : "";
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Name, GUI.skin.textField, GUILayout.Width(100.0f))) {
                Fire();
            }
            GUILayout.Label(Common.GetAnimationArrows(), GUI.skin.textField, GUILayout.Width(50.0f));
            GUILayout.Label(time, GUI.skin.textField, GUILayout.Width(100.0f));
            GUILayout.EndHorizontal();
        }

        public void PutState(StateStamp stateStamp, bool force) {
            if (stateStamp.IsActive != this.IsActive || force) {
                mStateQueue.Enqueue(stateStamp);
            }
        }
    }
}

