using UnityEngine;

namespace RemoteTech {
    public class RoverFragment : IFragment {
        public void Draw() {
            GUILayout.BeginVertical(GUILayout.Width(156), GUILayout.Height(300));
            {
                GUILayout.Label("");
            }
            GUILayout.EndVertical();
        }
    }
}