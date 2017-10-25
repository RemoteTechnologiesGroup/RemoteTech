using RemoteTech.FlightComputer.Commands;
using System;
using System.Collections;
using UnityEngine;
using static RemoteTech.FlightComputer.Commands.HibernationCommand;

namespace RemoteTech.UI
{
    public class PowerFragment : IFragment
    {
        private FlightComputer.FlightComputer mFlightComputer;
        private Action mOnClickQueue;
        private PowerModes mPowerMode;

        public PowerFragment(FlightComputer.FlightComputer fc, Action queue)
        {
            mFlightComputer = fc;
            mOnClickQueue = queue;
            mPowerMode = PowerModes.Normal;
        }

        public void Draw()
        {
            float width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;

            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUIStyle guiTableRow = new GUIStyle(HighLogic.Skin.label);
                    guiTableRow.normal.textColor = Color.white;

                    RTUtil.FakeStateButton(new GUIContent("HBNT", "Ultra-low power state with activated antennas shut down."), () => RTCore.Instance.StartCoroutine(OnPowerClick(PowerModes.Hibernate)), (int)mPowerMode, (int)PowerModes.Hibernate, GUILayout.Width(width3));
                    GUILayout.Label("", new GUILayoutOption[] { GUILayout.Width(width3) });
                    RTUtil.Button(new GUIContent("WAKE", "Exit from a power-saving state."), () => RTCore.Instance.StartCoroutine(OnPowerClick(PowerModes.Wake)), GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(200);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    RTUtil.Button(new GUIContent(">>", "Toggles the queue and delay functionality."),
                        mOnClickQueue, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private IEnumerator OnPowerClick(PowerModes nextPowerMode)
        {
            yield return null;
            if (mFlightComputer.InputAllowed)
            {
                switch(nextPowerMode)
                {
                    case PowerModes.Hibernate:
                        mPowerMode = PowerModes.Hibernate;
                        mFlightComputer.Enqueue(HibernationCommand.Hibernate());
                        break;
                    case PowerModes.Wake:
                        mPowerMode = PowerModes.Wake;
                        mFlightComputer.Enqueue(HibernationCommand.WakeUp());
                        break;
                    default:
                        mPowerMode = PowerModes.Normal;
                        break;
                }
            }
        }

        public void getActivePowerMode()
        {
            var activeHibCommand = HibernationCommand.findActiveHibernationCmd(mFlightComputer);
            if(activeHibCommand == null)
            {
                mPowerMode = PowerModes.Normal;
            }
            else
            {
                mPowerMode = PowerModes.Hibernate;
            }
        }
    }
}
