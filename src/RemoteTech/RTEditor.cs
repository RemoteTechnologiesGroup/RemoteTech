using RemoteTech.Modules;
using RemoteTech.UI;
using System;
using UnityEngine;
using KSP.UI.Screens;

namespace RemoteTech
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class RTEditor : MonoBehaviour
    {
        /// <summary>
        /// Start method for RTEditor
        /// </summary>
        public void Start()
        {
            GameEvents.onEditorNewShipDialogDismiss.Add(OnNewShip);
            GameEvents.onEditorPartPicked.Add(OnPartPicked);
            GameEvents.onEditorLoad.Add(OnEditorLoad);
            GameEvents.onEditorScreenChange.Add(OnScreenChange);
            GameEvents.onEditorRestart.Add(OnEditorRestart);
        }

        /// <summary>
        /// Unity onGUI Method to draw
        /// </summary>
        public void OnGUI()
        {
            GUI.depth = 0; // comment: necessary to make AbstractWindow's close button clickable

            Action windows = delegate { };
            var itr = AbstractWindow.Windows.GetEnumerator();
            while (itr.MoveNext())
            {
                var windowPair = itr.Current;
                windows += windowPair.Value.Draw;
            }
            windows.Invoke();
        }

        /// <summary>
        /// Unity OnDestroy Method to clean up
        /// </summary>
        public void OnDestroy()
        {
            GameEvents.onEditorNewShipDialogDismiss.Remove(OnNewShip);
            GameEvents.onEditorPartPicked.Remove(OnPartPicked);
            GameEvents.onEditorLoad.Remove(OnEditorLoad);
            GameEvents.onEditorScreenChange.Remove(OnScreenChange);
            GameEvents.onEditorRestart.Remove(OnEditorRestart);

            HideAllWindows();
        }

        private void HideAllWindows()
        {
            Action windows = delegate { };
            var itr = AbstractWindow.Windows.GetEnumerator();
            while (itr.MoveNext())
            {
                var windowPair = itr.Current;
                windows += windowPair.Value.Hide;
            }
            windows.Invoke();
        }

        /////////////
        // Bunch of editor events below that should hide RemoteTech window
        /////////////
        private void OnScreenChange(EditorScreen screen)
        {
            HideAllWindows();
        }

        private void OnEditorLoad(ShipConstruct ship, CraftBrowserDialog.LoadType loadType)
        {
            HideAllWindows();
        }

        private void OnPartPicked(Part part)
        {
            if (part != null)
            {
                var module = part.FindModuleImplementing<ModuleRTAntenna>();
                if (module != null)
                {
                    HideAllWindows();
                }
            }
        }

        private void OnNewShip()
        {
            HideAllWindows();
        }

        private void OnEditorRestart()
        {
            HideAllWindows();
        }
    }
}
