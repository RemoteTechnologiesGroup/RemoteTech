using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public interface IFragment
    {
        void Draw();
    }

    public static class FragmentExtensions
    {
        public static void Draw(this IFragment fragment, int width, int height)
        {
            GUILayout.BeginArea(GUILayoutUtility.GetRect(width, height), GUIStyle.none);
            {
                fragment.Draw();
            }
            GUILayout.EndArea();
        }
    }
}
