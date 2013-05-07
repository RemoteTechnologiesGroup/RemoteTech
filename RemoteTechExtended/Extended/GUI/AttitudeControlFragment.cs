using System;
using UnityEngine;

namespace RemoteTechExtended
{
    public class AttitudeControlFragment : IUIFragment {

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
            SurfaceSend,
            ToggleRoll,
        }

        Button<Action>[] mSimpleCommandButtons;
        Button<Action>[] mCollapsableCommandButtons;
        DelayedToggleButton<Action> mSurfaceButton;
        InputButton<Action> mPitchButton;
        InputButton<Action> mHeadingInclinationButton;
        InputToggleButton<Action> mRollButton;
        bool mFoldSurface = true;

        public AttitudeControlFragment() {
            mSimpleCommandButtons = new Button<Action>[] {
                new DelayedToggleButton<Action>(OnClick, "KillRot", Action.KillRot),
                new DelayedToggleButton<Action>(OnClick, "Prograde", Action.Prograde),
                new DelayedToggleButton<Action>(OnClick, "Retrograde", Action.Retrograde),
                new DelayedToggleButton<Action>(OnClick, "Normal+", Action.NormalPlus),
                new DelayedToggleButton<Action>(OnClick, "Normal-", Action.NormalMinus),
                new DelayedToggleButton<Action>(OnClick, "Radial+", Action.RadialPlus),
                new DelayedToggleButton<Action>(OnClick, "Radial-", Action.RadialMinus),
                new DelayedToggleButton<Action>(OnClick, "Maneuver", Action.ManeuverNode),
                mSurfaceButton = new DelayedToggleButton<Action>(OnClick, "Surface", Action.Surface),
            };
            mCollapsableCommandButtons = new Button<Action>[] {
                mPitchButton = new InputButton<Action>(null, "Pitch:", null),
                mHeadingInclinationButton = new InputButton<Action>(null, "Heading:", null),
                mRollButton = new InputToggleButton<Action>(OnClick, "Roll:", Action.ToggleRoll),
                new Button<Action>(OnClick, "Send", Action.SurfaceSend),
            }
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
                    mFoldSurface = ~mFoldSurface;
                    break;
                case Action.SurfaceSend:
                    break;
                case Action.ToggleRoll:
                    break;
                default:
                    mFoldSurface = true;

            }
        }
        
    }
}

