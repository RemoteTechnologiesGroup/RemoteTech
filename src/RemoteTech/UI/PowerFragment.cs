using RemoteTech.FlightComputer.Commands;
using System;
using System.Collections;
using UnityEngine;
using static RemoteTech.FlightComputer.Commands.HibernationCommand;
using KSP.Localization;

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
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_PowerFragment_HBNT"), Localizer.Format("#RT_PowerFragment_HBNT_desc")), () => RTCore.Instance.StartCoroutine(OnPowerClick(PowerModes.Hibernate)), (int)mPowerMode, (int)PowerModes.Hibernate, GUILayout.Width(width3));//"HBNT", "Ultra-low power hibernation with all active antennas shut down."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_PowerFragment_THLD"), Localizer.Format("#RT_PowerFragment_THLD_desc")), () => RTCore.Instance.StartCoroutine(OnPowerClick(PowerModes.AntennaSaver)), (int)mPowerMode, (int)PowerModes.AntennaSaver, GUILayout.Width(width3));//"THLD", "Optimally adaptive power-saving threshold control on all antennas"
                    RTUtil.Button(new GUIContent(Localizer.Format("#RT_PowerFragment_WAKE"), Localizer.Format("#RT_PowerFragment_WAKE_desc")), () => RTCore.Instance.StartCoroutine(OnPowerClick(PowerModes.Wake)), GUILayout.Width(width3));//"WAKE", "Terminate any power-saving state."
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(200);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent(">>", Localizer.Format("#RT_PowerFragment_Queue_desc")),//"Toggles the queue and delay functionality."
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
                    case PowerModes.AntennaSaver:
                        mPowerMode = PowerModes.AntennaSaver;
                        mFlightComputer.Enqueue(HibernationCommand.AntennaSaver());
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
            if(activeHibCommand != null)
            {
                mPowerMode = activeHibCommand.PowerMode;
            }
        }
    }
}
