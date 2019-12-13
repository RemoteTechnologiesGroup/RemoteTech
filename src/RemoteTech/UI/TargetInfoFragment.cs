using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace RemoteTech.UI
{
    public class TargetInfoFragment : IFragment, IDisposable
    {

        public class Target
        {
            public AntennaFragment.Entry TargetEntry { get; set; }
            public IAntenna Antenna { get; set; }
            public KeyValuePair<string, Color> TargetInfos
            {
                get
                {
                    if (TargetEntry == null || Antenna == null)
                        return new KeyValuePair<string, Color>("",Color.white);
                    
                    return NetworkFeedback.tryConnection(Antenna, TargetEntry.Guid);
                }
            }
        }

        /// <summary>Current target infos</summary>
        private Target target;
        /// <summary>Style set for each row on the target pop-up</summary>
        private GUIStyle guiTableRow;
        /// <summary>Style set for the headline of the target pop-up</summary>
        private GUIStyle guiHeadline;

        /// <summary>
        /// Initialize the targetinfoFragment without a target
        /// </summary>
        public TargetInfoFragment()
        {
            InitalGuiStyles();
        }

        /// <summary>
        /// Initialize the targetinfoFragment with a targetEntry and an antenna
        /// </summary>
        /// <param name="targetEntry">Target from the antenna fragment</param>
        /// <param name="antenna">current antenna</param>
        public TargetInfoFragment(AntennaFragment.Entry targetEntry, IAntenna antenna)
            : this()
        {
            SetTarget(targetEntry, antenna);
        }

        /// <summary>
        /// Initialize the style sets for this fragment
        /// </summary>
        private void InitalGuiStyles()
        {
            // initial styles
            guiTableRow = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12,
                normal = {textColor = Color.white}
            };

            guiHeadline = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 13, 
                fontStyle = FontStyle.Bold
            };
        }

        /// <summary>
        /// Set a new target to the targetfragment with a targetEntry and an antenna
        /// </summary>
        /// <param name="targetEntry">Target from the antenna fragment</param>
        /// <param name="antenna">current antenna</param>
        public void SetTarget(AntennaFragment.Entry targetEntry, IAntenna antenna)
        {
            target = new Target {TargetEntry = targetEntry, Antenna = antenna};
        }

        public void Dispose()
        {
            target = null;
        }

        /// <summary>
        /// Draw the information for the target, set by the setTarget()
        /// </summary>
        public void Draw()
        {
            if(target != null)
            {
                KeyValuePair<string, Color> infos = target.TargetInfos;

                GUILayout.Label(target.TargetEntry.Text, guiHeadline);

                // Split the given informations from the target.targetInfos. Each ; is one row
                var diagnostic = infos.Key.Split(';');
                // Loop the rows
                foreach (var diagnosticTextLines in diagnostic)
                {
                    try
                    {
                        GUILayout.BeginHorizontal();
                        // If the text contains a 'label' so we also split this text into to
                        // seperated text parts
                        if (diagnosticTextLines.Trim().Contains(':'))
                        {
                            var tableString = diagnosticTextLines.Trim().Split(':');
                            // draw the label
                            GUILayout.Label(tableString[0] + ':', guiTableRow, GUILayout.Width(110));
                            // if the label is 'status' so change the textcolor to the color
                            // given by the NetworkFeedback class.
                            if (tableString[0].ToLower() == Localizer.Format("#RT_ModuleUI_Status_tolower"))//"status"
                            {
                                guiTableRow.normal.textColor = infos.Value;
                            }
                            // print the value for this row
                            GUILayout.Label(tableString[1], guiTableRow);
                            // restore the text color
                            guiTableRow.normal.textColor = Color.white;
                        }
                        else
                        {
                            // if we do not have a table style information so print the complete text
                            GUILayout.Label(diagnosticTextLines.Trim(), guiTableRow);
                        }
                        GUILayout.EndHorizontal();
                    }
                    catch (Exception ex)
                    {
                        RTLog.Notify("Exception {0}",ex);
                        // I got one exception, thrown from Unity and i don't know how to deal with it

                        // Exception System.ArgumentException: Getting control 4's
                        // position in a group with only 4 controls when doing Repaint
                    }
                }
            }
        }
    }
}
