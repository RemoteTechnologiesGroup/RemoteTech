using System;
using UnityEngine;

namespace RemoteTech
{
    public class AttitudeControlFragment {

        public enum Action {
            BtnClickOff,
            BtnClickKillRot,
            BtnClickNode,
            BtnClickSurf,
            BtnClickOrb,
        }

        bool mFoldSurface = true;
        bool mRollEnabled = false;

        int[] mToggleGroupStates = { 0, 0, 0, 0 };

        public AttitudeControlFragment(SPUPartModule attachedTo) {

        }

        public void Draw() {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            GUILayout.EndVertical();
        }

        public void OnClick(Action action) {

        }

        public void OnClick(Attitude attitude) {

        }

        void DrawStateButton(String name, Action action, ref int state, int stateVal,
                                                                         float height,
                                                                         float width) {
            if (GUILayout.Toggle(state == stateVal, name, GUI.skin.button,
                                                          GUILayout.Width(width),
                                                          GUILayout.Height(height))) {
            }
        }

        void DrawInput(String name, Action action, ref double value) {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(name, GUI.skin.textField, GUILayout.Width(200.0f))) {
                OnClick(action);
            }
            GUILayout.Label(value.ToString(), GUI.skin.textField, GUILayout.Width(50.0f));
            if (GUILayout.Button("+", GUI.skin.textField, GUILayout.Width(21.0f))) {
                value++;
            }
            if (GUILayout.Button("-", GUI.skin.textField, GUILayout.Width(21.0f))) {
                value--;
            }
            GUILayout.EndHorizontal();
        }
    }


}

