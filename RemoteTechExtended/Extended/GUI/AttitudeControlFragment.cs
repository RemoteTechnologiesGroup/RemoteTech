using System;
using UnityEngine;

namespace RemoteTech
{
    public class AttitudeControlFragment : AbstractFragment {

        public enum Action {
            KillRot,
            Prograde,
            Retrograde,
            NormalPlus,
            NormalMinus,
            RadialPlus,
            RadialMinus,
            ManeuverNode,
            Surface,
            Send,
            Roll,
        }

        Button<Action>[] mSimpleCommandButtons;
        Button<Action>[] mCollapsableCommandButtons;
        DelayedStateButton<Action> mSurfaceButton;
        InputButton<Action> mPitchButton;
        InputButton<Action> mHeadingButton;
        InputToggleButton<Action> mRollButton;
        bool mFoldSurface = true;

        public AttitudeControlFragment(SPUPartModule attachedTo) : base(attachedTo) {
            mSimpleCommandButtons = new Button<Action>[] {
                new DelayedStateButton<Action>(OnClick, "KillRot", Action.KillRot),
                new DelayedStateButton<Action>(OnClick, "Prograde", Action.Prograde),
                new DelayedStateButton<Action>(OnClick, "Retrograde", Action.Retrograde),
                new DelayedStateButton<Action>(OnClick, "Normal+", Action.NormalPlus),
                new DelayedStateButton<Action>(OnClick, "Normal-", Action.NormalMinus),
                new DelayedStateButton<Action>(OnClick, "Radial+", Action.RadialPlus),
                new DelayedStateButton<Action>(OnClick, "Radial-", Action.RadialMinus),
                new DelayedStateButton<Action>(OnClick, "Maneuver", Action.ManeuverNode),
                mSurfaceButton = new DelayedStateButton<Action>(OnClick, "Surface", Action.Surface),
            };
            mCollapsableCommandButtons = new Button<Action>[] {
                mPitchButton = new InputButton<Action>(null, "Pitch:", Action.KillRot, 90, -180, 180),
                mHeadingButton = new InputButton<Action>(null, "Heading:", Action.KillRot, 90, -180, 180),
                mRollButton = new InputToggleButton<Action>(OnClick, "Roll:", Action.Roll, 0, -180, 180, false),
                new Button<Action>(OnClick, "Send", Action.Send),
            };
        }

        public void Draw() {
            GUILayout.BeginVertical();
            foreach (Button<Action> b in mSimpleCommandButtons) {
                b.Draw();
            }
            if (!mFoldSurface) {
                foreach (Button<Action> b in mCollapsableCommandButtons) {
                    b.Draw();
                }
            }
            GUILayout.EndVertical();
        }

        public void OnClick(Action action) {
            switch (action) {
                case Action.Surface:
                    mFoldSurface = !mFoldSurface;
                    break;
                case Action.Send:
                    if (mRollButton.IsActive) {
                        Module.Enqueue(new AttitudeChange(Attitude.Surface, 
                                                          mPitchButton.Value,
                                                          mHeadingButton.Value,
                                                          mRollButton.Value));
                    } else {
                        Module.Enqueue(new AttitudeChange(Attitude.Surface, 
                                                          mPitchButton.Value,
                                                          mHeadingButton.Value));
                    }
                    break;
                case Action.Roll:
                    mRollButton.IsActive = false;
                    break;
                default:
                    mFoldSurface = true;
                    Module.Enqueue(new AttitudeChange((Attitude)Enum.Parse(typeof(Attitude), ((int)action).ToString())));
                    break;

            }
        }
        
    }
}

