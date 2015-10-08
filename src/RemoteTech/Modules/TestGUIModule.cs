using System;
using UnityEngine;
using RemoteTech;

namespace RemoteTech.Modules
{
	[KSPModule("TESTGUI")]
	public class TestGuiModule : PartModule
	{
		protected Rect windowPos = new Rect();

		public override void OnStart(StartState state)
		{
			if (state != StartState.Editor)
				RenderingManager.AddToPostDrawQueue (0, OnDraw);			
		}

		private void OnDraw()
		{
			if (this.vessel == FlightGlobals.ActiveVessel)
				windowPos = GUILayout.Window (99, windowPos, OnWindow, "RT TEST");
		}

		private void OnWindow(int windowId)
		{
			GUILayout.BeginHorizontal (GUILayout.Width (250f));			
			GUILayout.Label ("add basestation");
			GUILayout.EndHorizontal ();
			GUILayout.BeginVertical ();
			if (GUILayout.Button ("Add Basestation", GUILayout.ExpandWidth (true))) 
			{
				RemoteTech.API.API.AddGroundStation ("Barry's Ijspaleis", 78.608117f, 147.491623f, 2817.58f, 1);
			}
			if (GUILayout.Button ("Remove Barry's Ijspalijs", GUILayout.ExpandWidth (true))) 
			{
				RemoteTech.API.API.RemoveGroundStation ("Barry's Ijspaleis", 1);
			}
			if (GUILayout.Button ("Remove andere body", GUILayout.ExpandWidth (true))) 
			{
				RemoteTech.API.API.RemoveGroundStation ("Barry's Ijspaleis", 2);
			}
			if (GUILayout.Button ("Remove andere naam", GUILayout.ExpandWidth (true))) 
			{
				RemoteTech.API.API.RemoveGroundStation ("Barry", 1);
			}

			GUILayout.EndVertical ();

			GUI.DragWindow ();
		}



		//RTSettings.Instance.AddBaseStation ("Barry's Ijspaleis", -0.1313315f, -74.59484f, 100f, 1, new Color(0.996078f, 0, 0, 1));

	}
}

