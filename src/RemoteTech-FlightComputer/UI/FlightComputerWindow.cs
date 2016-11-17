using System;
using RemoteTech.Common.UI;
using UnityEngine;

namespace RemoteTech.FlightComputer.UI
{
    public class FlightComputerWindow : AbstractWindow
    {
        private enum FragmentTab
        {
            Attitude = 0,
            Rover = 1,
        }

        private FragmentTab _fragmentTab = FragmentTab.Attitude;
        private readonly AttitudeFragment _attitudeFragment;
        private readonly RoverFragment _roverFragment;
        private readonly QueueFragment _queueFragment;
        private bool _queueEnabled;
        private readonly FlightComputer _flightComputer;

        private FragmentTab Tab
        {
            get
            {
                return _fragmentTab;
            }
            set
            {
                var numberOfTabs = Enum.GetNames(typeof(FragmentTab)).Length;
                if ((int)value >= numberOfTabs) {
                    _fragmentTab = 0;
                } else if (value < 0) {
                    _fragmentTab = (FragmentTab)(numberOfTabs - 1);
                } else {
                    _fragmentTab = value;
                }
            }
        }

        public FlightComputerWindow(FlightComputer fc)
            : base(Guid.NewGuid(), "FlightComputer", new Rect(100, 100, 0, 0), WindowAlign.Floating)
        {
            mSavePosition = true;
            _flightComputer = fc;
            _attitudeFragment = new AttitudeFragment(fc, OnQueue);
            _roverFragment = new RoverFragment(fc, OnQueue);
            _queueFragment = new QueueFragment(fc);
            _queueEnabled = false;
        }

        public override void Show()
        {
            base.Show();
            _flightComputer.OnActiveCommandAbort += _attitudeFragment.Reset;
            _flightComputer.OnNewCommandPop += _attitudeFragment.getActiveFlightMode;

            _attitudeFragment.getActiveFlightMode();
        }

        public override void Hide()
        {
            _flightComputer.OnActiveCommandAbort -= _attitudeFragment.Reset;
            _flightComputer.OnNewCommandPop -= _attitudeFragment.getActiveFlightMode;
            base.Hide();
        }

        public override void Window(int id)
        {
            GUI.skin = null;
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginHorizontal();
                {
                    switch (_fragmentTab) {
                        case FragmentTab.Attitude:
                            _attitudeFragment.Draw();
                            break;
                        case FragmentTab.Rover:
                            _roverFragment.Draw();
                            break;
                    }
                }
                GUILayout.EndHorizontal();

                // RoverComputer
                if (GUI.Button(new Rect(2, 2, 16, 16), "<")) {
                    Tab--;
                }
                if (GUI.Button(new Rect(16, 2, 16, 16), ">")) {
                    Tab++;
                }

                if (_queueEnabled) {
                    _queueFragment.Draw();
                } else {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginScrollView(Vector2.zero, GUILayout.ExpandHeight(true));
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndHorizontal();
            base.Window(id);
        }

        private void OnQueue()
        {
            _queueEnabled = !_queueEnabled;
            if(_queueEnabled)
            {
                Title = "Flight Computer: " + _flightComputer.Vessel.vesselName.Substring(0, Math.Min(25, _flightComputer.Vessel.vesselName.Length));
            }
            else
            {
                Title = "Flight Computer";
            }
        }
    }
}
