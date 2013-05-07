using System;
using UnityEngine;
using System.Collections.Generic;

namespace RemoteTech
{
    public class DelayedStateButton<T> : Button<T> {

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

        public DelayedStateButton(ClickDelegate<T> click, String name, T action)
            : base(click, name, action) {
            this.IsActive = false;
        }

        public override void Draw() {
            while (mStateQueue.Peek().Timestamp < Common.GetGameTime()) {
                IsActive = mStateQueue.Dequeue().IsActive;
            }
            String time = mStateQueue.Count > 0 ? 
                Common.FormatTime(mStateQueue.Peek().Timestamp - Common.GetGameTime()) : "";

            GUILayout.BeginHorizontal();

            GUI.backgroundColor = IsActive ? Color.green : Color.black;
            if (GUILayout.Button(Name, GUI.skin.textField, GUILayout.Width(100.0f))) {
                Fire();
            }

            GUI.backgroundColor = Color.black;
            GUILayout.Label(Common.GetAnimationArrows(), GUI.skin.textField, GUILayout.Width(50.0f));

            GUI.backgroundColor = (time != "")
                ? (mStateQueue.Peek().IsActive ? Color.green : Color.red)
                    : Color.black;
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

