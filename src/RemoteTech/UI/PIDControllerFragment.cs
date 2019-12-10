using RemoteTech.FlightComputer;
using RemoteTech.FlightComputer.Commands;
using System;
using System.Collections;
using UnityEngine;
using KSP.Localization;

namespace RemoteTech.UI
{
    public class PIDControllerFragment : IFragment
    {
        private FlightComputer.FlightComputer mFlightComputer;
        private Action mOnClickQueue;

        private string kp = "0",
                       ki = "0",
                       kd = "0";

        public PIDControllerFragment(FlightComputer.FlightComputer fc, Action queue)
        {
            mFlightComputer = fc;
            mOnClickQueue = queue;

            LoadFlightPIDValues();
        }

        public void Draw()
        {
            float width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;

            GUILayout.BeginVertical();
            {
                Vector3 Torque = SteeringHelper.TorqueAvailable;
                var MoI = mFlightComputer.Vessel.MOI;

                ////////////////
                //PITCH INFO
                ////////////////
                GUILayout.Label(new GUIContent("<b>" + Localizer.Format("#RT_PIDControllerFragment_Pitch") + "</b>"));//"Pitch"
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_TorqueMoI"), Localizer.Format("#RT_PIDControllerFragment_TorqueMoI_desc")));//"Torque-MoI Rate: ", "Current rate of torque to mass of inertia"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent((Torque[0] / MoI[0]).ToString("F3")));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_DeviationError"), Localizer.Format("#RT_PIDControllerFragment_DeviationError_desc")));//"Deviation Error: ", "Deviation from the target point"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent(mFlightComputer.PIDController.getDeviationErrors()[0].ToString("F2")));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_Output"), Localizer.Format("#RT_PIDControllerFragment_Output_desc")));//"Output: ", "Output of Flight Control State"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent(mFlightComputer.Vessel.ctrlState.pitch.ToString("F2")));
                }
                GUILayout.EndHorizontal();

                ////////////////
                //ROLL INFO
                ////////////////
                GUILayout.Label(new GUIContent("<b>" + Localizer.Format("#RT_PIDControllerFragment_Roll") + "</b>"));//"Roll"
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_TorqueMoI"), Localizer.Format("#RT_PIDControllerFragment_TorqueMoI_desc")));//"Torque-MoI Rate: ", "Current rate of torque to mass of inertia"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent((Torque[1] / MoI[1]).ToString("F3")));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_DeviationError"), Localizer.Format("#RT_PIDControllerFragment_DeviationError_desc")));//"Deviation Error: ", "Deviation from the target point"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent(mFlightComputer.PIDController.getDeviationErrors()[1].ToString("F2")));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_Output"), Localizer.Format("#RT_PIDControllerFragment_Output_desc")));//"Output: ", "Output of Flight Control State"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent(mFlightComputer.Vessel.ctrlState.roll.ToString("F2")));
                }
                GUILayout.EndHorizontal();

                ////////////////
                //YAW INFO
                ////////////////
                GUILayout.Label(new GUIContent("<b>" + Localizer.Format("#RT_PIDControllerFragment_Yaw") + "</b>"));//"Yaw"
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_TorqueMoI"), Localizer.Format("#RT_PIDControllerFragment_TorqueMoI_desc")));//"Torque-MoI Rate: ", "Current rate of torque to mass of inertia"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent((Torque[2] / MoI[2]).ToString("F3")));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_DeviationError"), Localizer.Format("#RT_PIDControllerFragment_DeviationError_desc")));//"Deviation Error: ", "Deviation from the target point"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent(mFlightComputer.PIDController.getDeviationErrors()[2].ToString("F2")));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_Output"), Localizer.Format("#RT_PIDControllerFragment_Output_desc")));//"Output: ", "Output of Flight Control State"
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(new GUIContent(mFlightComputer.Vessel.ctrlState.yaw.ToString("F2")));
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_PIDHelp")));//"See ni.com/white-paper/3782/en"

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_Kp"), Localizer.Format("#RT_PIDControllerFragment_Kp_desc")));//"Proportional gain", "(1) With I and D terms set to 0, increase until the output of the loop oscillates."
                    RTUtil.TextField(ref kp, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_Ki"), Localizer.Format("#RT_PIDControllerFragment_Ki_desc")));//"Integral", "(2) Increase to stop the oscillations."
                    RTUtil.TextField(ref ki, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_Kd"), Localizer.Format("#RT_PIDControllerFragment_Kd_desc")));//"Derivative", "(3) Increase until the loop is acceptably quick to its target point."
                    RTUtil.TextField(ref kd, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_button1"), Localizer.Format("#RT_PIDControllerFragment_button1_desc")),//"SAVE", "Save all values persistently."
                        OnSaveClick, GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent(Localizer.Format("#RT_PIDControllerFragment_button2"), Localizer.Format("#RT_PIDControllerFragment_button2_desc")),//"APLY", "Interface all values to Flight PID Controller."
                        () => RTCore.Instance.StartCoroutine(OnApplyClick()), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent(">>", Localizer.Format("#RT_PIDControllerFragment_Queue_desc")),//"Toggles the queue and delay functionality."
                        mOnClickQueue, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private IEnumerator OnApplyClick()
        {
            yield return null;
            if (mFlightComputer.InputAllowed)
            {
                kp = RTUtil.ConstrictNum(kp, false);
                ki = RTUtil.ConstrictNum(ki, false);
                kd = RTUtil.ConstrictNum(kd, false);

                mFlightComputer.Enqueue(PIDCommand.WithNewChanges(Double.Parse(kp), Double.Parse(ki), Double.Parse(kd)));
            }
        }

        private void OnSaveClick()
        {
            if (RTSettings.Instance != null)
            {
                RTSettings.Instance.FlightTermP = Double.Parse(kp);
                RTSettings.Instance.FlightTermI = Double.Parse(ki);
                RTSettings.Instance.FlightTermD = Double.Parse(kd);
            }

            FlightComputer.FlightComputer.PIDKp = Double.Parse(kp);
            FlightComputer.FlightComputer.PIDKi = Double.Parse(ki);
            FlightComputer.FlightComputer.PIDKd = Double.Parse(kd);
        }

        private void LoadFlightPIDValues()
        {
            if (RTSettings.Instance != null)
            {
                FlightComputer.FlightComputer.PIDKp = RTSettings.Instance.FlightTermP;
                FlightComputer.FlightComputer.PIDKi = RTSettings.Instance.FlightTermI;
                FlightComputer.FlightComputer.PIDKd = RTSettings.Instance.FlightTermD;
            }

            kp = FlightComputer.FlightComputer.PIDKp.ToString();
            ki = FlightComputer.FlightComputer.PIDKi.ToString();
            kd = FlightComputer.FlightComputer.PIDKd.ToString();
        }
    }
}
