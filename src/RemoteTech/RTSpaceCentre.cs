using RemoteTech.UI;
using System;
using UnityEngine;

namespace RemoteTech
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class RTSpaceCentre : MonoBehaviour
    {
        /// <summary>Button for KSP Stock Toolbar</summary>
        public static ApplicationLauncherButton LauncherButton = null;
        /// <summary>OptionWindow</summary>
        private OptionWindow OptionWindow;
        /// <summary>Texture for the KSP Stock Toolbar-Button</summary>
        private Texture2D RtOptionBtn;

        /// <summary>
        /// Start method for RTSpaceCentre
        /// </summary>
        public void Start()
        {
            // create the option window
            this.OptionWindow = new OptionWindow();
            
            if (ApplicationLauncher.Ready)
            {
                RTUtil.LoadImage(out this.RtOptionBtn, "gitpagessat.png");
                RTSpaceCentre.LauncherButton = ApplicationLauncher.Instance.AddModApplication(this.OptionWindow.toggleWindow, null, null, null, null, null,
                                                                                        ApplicationLauncher.AppScenes.SPACECENTER,
                                                                                        (Texture)this.RtOptionBtn);
            }

            if (RTSettings.Instance.firstStart)
            {
                // open here the option dialog for the first start
                RTLog.Notify("First start of RemoteTech!");
                this.OptionWindow.Show();
            }
        }

        /// <summary>
        /// Unity onGUI Method to draw the OptionWindow
        /// </summary>
        public void OnGUI()
        {
            Action windows = delegate { };
            foreach (var window in AbstractWindow.Windows.Values)
            {
                windows += window.Draw;
            }
            windows.Invoke();
        }

        /// <summary>
        /// Unity OnDestroy Method to clean up
        /// </summary>
        public void OnDestroy()
        {
            this.OptionWindow.Hide();

            if (RTSpaceCentre.LauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(RTSpaceCentre.LauncherButton);
            }
        }
    }
}
