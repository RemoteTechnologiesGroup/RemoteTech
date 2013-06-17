using System;
using UnityEngine;

namespace RemoteTech {
    public class AttitudeFragment : IFragment {
        private static String[] MODE_TEXT = new String[] {
            "OFF", "KILL", "NOD",
            "ORB", "SRF",  "TGT",
        };

        private static String[] ATTITUDE_TEXT = new String[] {
            "GRD\n+", "RAD\n+", "NRM\n+",
            "GRD\n-", "RAD\n-", "NRM\n-",
        };

        private OnClick mOnClickQueue;

        private int mModeState;
        private int mAttitudeState;
        private float mThrottleState;

        private String mPitch;
        private String mRoll;
        private bool mRollEnabled;
        private String mHeading;
        private String mDuration;

        public AttitudeFragment(OnClick queue) {
            mOnClickQueue = queue;
            mPitch = "90";
            mRoll = "90";
            mHeading = "90";
            mDuration = "0";
        }

        public void Draw() {
            GUILayout.BeginVertical();
            {
                RTUtil.GroupButton(3, MODE_TEXT, ref mModeState);
                GUILayout.Space(5);
                RTUtil.GroupButton(3, ATTITUDE_TEXT, ref mAttitudeState);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Pitch: ");
                    RTUtil.Button("+", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.Button("-", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.TextField(ref mPitch, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Head: ");
                    RTUtil.Button("+", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.Button("-", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.TextField(ref mHeading, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Roll: ");
                    RTUtil.Button("+", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.Button("-", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.TextField(ref mRoll, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Throttle: ");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(mThrottleState.ToString("F1") + "%");
                }
                GUILayout.EndHorizontal();
                RTUtil.HorizontalSlider(ref mThrottleState, 0, 100);
                RTUtil.TextField(ref mDuration);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("UPD", OnClickUpdate);
                    GUILayout.FlexibleSpace();
                    RTUtil.Button("Q", mOnClickQueue);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        void OnClickUpdate() {

        }
    }
}