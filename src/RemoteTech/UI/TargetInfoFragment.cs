using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    class TargetInfoFragment : IFragment, IDisposable
    {

        public class Target
        {
            public AntennaFragment.Entry targetEntry;
            public IAntenna antenna;
            public KeyValuePair<string, Color> targetInfos
            {
                get
                {
                    if (targetEntry == null || antenna == null)
                        return new KeyValuePair<string, Color>("",UnityEngine.Color.white);
                    
                    return NetworkFeedback.tryConnection(antenna, targetEntry.Guid);
                }
            }
        }

        /// <summary>Current target infos</summary>
        private Target target;
        /// <summary>Styleset for each row on the target popup</summary>
        GUIStyle guiTableRow;
        /// <summary>Styleset for the headline of the target popup</summary>
        GUIStyle guiHeadline;

        /// <summary>
        /// Initialize the targetinfoFragment without a target
        /// </summary>
        public TargetInfoFragment()
        {
            initalGuiStyles();
        }

        /// <summary>
        /// Initialize the targetinfoFragment with a targetEntry and an antenna
        /// </summary>
        /// <param name="targetEntry">Target from the antenna fragment</param>
        /// <param name="antenna">current antenna</param>
        public TargetInfoFragment(AntennaFragment.Entry targetEntry, IAntenna antenna)
            : this()
        {
            setTarget(targetEntry, antenna);
        }

        /// <summary>
        /// Initialize the style sets for this fragment
        /// </summary>
        private void initalGuiStyles()
        {
            // initial styles
            guiTableRow = new GUIStyle(HighLogic.Skin.label);
            guiTableRow.fontSize = 12;
            guiTableRow.normal.textColor = UnityEngine.Color.white;

            guiHeadline = new GUIStyle(HighLogic.Skin.label);
            guiHeadline.fontSize = 13;
            guiHeadline.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// Set a new target to the targetfragment with a targetEntry and an antenna
        /// </summary>
        /// <param name="targetEntry">Target from the antenna fragment</param>
        /// <param name="antenna">current antenna</param>
        public void setTarget(AntennaFragment.Entry targetEntry, IAntenna antenna)
        {
            target = new Target();
            target.targetEntry = targetEntry;
            target.antenna = antenna;
        }

        public void Dispose()
        {
            target = null;
        }

        /// <summary>
        /// Draw the informationes for the target, set by the setTarget()
        /// </summary>
        public void Draw()
        {
            if(target != null)
            {
                KeyValuePair<string, Color> infos = target.targetInfos;

                GUILayout.Label(target.targetEntry.Text, guiHeadline);

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
                            if (tableString[0].ToLower() == "status")
                            {
                                guiTableRow.normal.textColor = infos.Value;
                            }
                            // print the value for this row
                            GUILayout.Label(tableString[1], guiTableRow);
                            // restore the text color
                            guiTableRow.normal.textColor = UnityEngine.Color.white;
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
                        // RTLog.Notify("Exception {0}",ex);
                        // I got one exception, thrown from Unity and i don't know how to deal with it

                        // Exception System.ArgumentException: Getting control 4's
                        // position in a group with only 4 controls when doing Repaint
                    }
                }
            }
        }
    }
}
