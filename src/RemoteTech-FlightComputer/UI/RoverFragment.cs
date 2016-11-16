using System;
using System.Globalization;
using RemoteTech.Common.Extensions;
using RemoteTech.Common.UI;
using RemoteTech.Common.Utils;
using RemoteTech.FlightComputer.Commands;
using UnityEngine;

namespace RemoteTech.FlightComputer.UI
{
    public class RoverFragment : IFragment
    {
        private readonly FlightComputer _flightComputer;
        private readonly Action _onClickQueue;

        private bool _distDefault = true;

        private float _steering;
        private float _steerClamp = 1;
        private float _prevTurn;
        private float _prevDist;

        private string _turnString = "";
        private string _speedString = "5";
        private string _distanceString = "";
        private string _latitudeString = "";
        private string _longitudeString = "";
        private string _headingString = "";

        int _selected;
        private bool _mouseClick;
        private readonly string[] _tabs = { "Tgt.", "Hdt.", "Fine" };

        public RoverFragment(FlightComputer fc, Action queue)
        {
            _flightComputer = fc;
            _onClickQueue = queue;
        }

        private float Turn
        {
            get
            {
                float turn;
                if (!float.TryParse(_turnString, out turn)) {
                    turn = 0;
                }
                return turn;
            }
            set { _turnString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private float Heading
        {
            get
            {
                float heading;
                if (!float.TryParse(_headingString, out heading)) {
                    heading = 0;
                }
                return heading;
            }
            set { _turnString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private float Speed
        {
            get
            {
                float speed;
                if (!float.TryParse(_speedString, out speed)) {
                    speed = 0;
                }
                return speed;
            }
            set { _speedString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private float Dist
        {
            get
            {
                float dist;
                if (!float.TryParse(_distanceString, out dist)) {
                    dist = 0;
                }
                return dist;
            }
            set { _distanceString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private float Latitude
        {
            get
            {
                float latitude;
                if (!float.TryParse(_latitudeString, out latitude)) {
                    latitude = 0;
                }
                return latitude;
            }
            set { _latitudeString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private float Longitude
        {
            get
            {
                float longitude;
                if (!float.TryParse(_longitudeString, out longitude)) {
                    longitude = 0;
                }
                return longitude;
            }
            set { _longitudeString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private void EnqueueTurn()
        {
            _flightComputer.Enqueue(DriveCommand.Turn(_steering, Turn, Speed));
        }
        private void EnqueueDist()
        {
            _flightComputer.Enqueue(DriveCommand.Distance(Dist, 0, Speed));
        }

        public void Draw()
        {
            float width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;

            GUILayout.BeginVertical(GUILayout.Width(156), GUILayout.Height(300));
            {
                _selected = GUILayout.Toolbar(_selected, _tabs, GUILayout.Width(160 - GUI.skin.button.margin.right * 2.0f));

                switch (_selected) {
                    case 0:
                        Target();
                        break;
                    case 1:
                        HDG();
                        break;
                    case 2:
                        Fine();
                        break;
                }

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GuiUtil.Button(new GUIContent(">>", "Toggles the queue and delay functionality."),
                        _onClickQueue, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void Fine()
        {

            if (Event.current.Equals(Event.KeyboardEvent("return")) && Speed != 0) {
                if (GUI.GetNameOfFocusedControl() == "RC1" && _steering != 0 && Turn != 0)
                    EnqueueTurn();
                else if (GUI.GetNameOfFocusedControl() == "RC2" && Dist != 0)
                    EnqueueDist();
                else if (GUI.GetNameOfFocusedControl() == "RC3") {
                    if (!_distDefault && _steering != 0 && Turn != 0)
                        EnqueueTurn();
                    else if (_distDefault && Dist != 0)
                        EnqueueDist();
                }
            }


            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Wheel: ", "how much to turn"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(Math.Abs(_steering).ToString("P"), "How much to turn"));
                if (_steering != 0) {
                    if (_steering < 0)
                        GUILayout.Label(new GUIContent("right", "how much to turn"), GUILayout.Width(40));
                    else
                        GUILayout.Label(new GUIContent("left", "how much to turn"), GUILayout.Width(40));
                } else
                    GUILayout.Label(new GUIContent("", "how much to turn"), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GuiUtil.HorizontalSlider(ref _steering, 1, -1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Turn", "How many degrees to turn"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                GuiUtil.TextField(ref _turnString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "How many degrees to turn"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Dist.", "Distance to drive"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                GuiUtil.TextField(ref _distanceString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m)", "Distance to drive"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep, negative for reverse"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                GuiUtil.TextField(ref _speedString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep, negative for reverse"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            _turnString = FormatUtil.ConstrictNum(_turnString, 90);
            _distanceString = FormatUtil.ConstrictNum(_distanceString, false);
            _speedString = FormatUtil.ConstrictNum(_speedString);


            if (_prevTurn != Turn)
                _distDefault = false;
            else if (_prevDist != Dist)
                _distDefault = true;

            _prevTurn = Turn;
            _prevDist = Dist;

        }

        private void HDG()
        {

            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl().StartsWith("RC"))
                _flightComputer.Enqueue(DriveCommand.DistanceHeading(Dist, Heading, _steerClamp, Speed));

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Wheel: ", "How sharp to turn at max"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(_steerClamp.ToString("P"), "How sharp to turn at max"));
                GUILayout.Label(new GUIContent("max", "How sharp to turn at max"), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GuiUtil.HorizontalSlider(ref _steerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Hdg.", "Heading to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                GuiUtil.TextField(ref _headingString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Dist.", "Distance to drive"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                GuiUtil.TextField(ref _distanceString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                GuiUtil.TextField(ref _speedString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            _headingString = FormatUtil.ConstrictNum(_headingString, 360);
            _distanceString = FormatUtil.ConstrictNum(_distanceString, false);
            _speedString = FormatUtil.ConstrictNum(_speedString, false);
        }

        private void Target()
        {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl().StartsWith("RC"))
                _flightComputer.Enqueue(DriveCommand.Coord(_steerClamp, Latitude, Longitude, Speed));
            else if (GameSettings.MODIFIER_KEY.GetKey() && ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) != _mouseClick)) {
                _mouseClick = Input.GetMouseButton(0) || Input.GetMouseButton(1);
                Vector2 latlon;
                if (_mouseClick && CelestialBodyExtension.CBhit(_flightComputer.Vessel.mainBody, out latlon)) {
                    Latitude = latlon.x;
                    Longitude = latlon.y;

                    if (Input.GetMouseButton(1))
                        _flightComputer.Enqueue(DriveCommand.Coord(_steerClamp, Latitude, Longitude, Speed));
                }
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Wheel: ", "How sharp to turn at max"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(_steerClamp.ToString("P"), "How sharp to turn at max"));
                GUILayout.Label(new GUIContent("max", "How sharp to turn at max"), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GuiUtil.HorizontalSlider(ref _steerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("LAT.", "Latitude to drive to"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                GuiUtil.TextField(ref _latitudeString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "Hold " + GameSettings.MODIFIER_KEY.name + " and click on ground to input coordinates"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("LON.", "Longitude to drive to"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                GuiUtil.TextField(ref _longitudeString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "Hold " + GameSettings.MODIFIER_KEY.name + " and click on ground to input coordinates"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                GuiUtil.TextField(ref _speedString, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();


            _latitudeString = FormatUtil.ConstrictNum(_latitudeString);
            _longitudeString = FormatUtil.ConstrictNum(_longitudeString);
            _speedString = FormatUtil.ConstrictNum(_speedString, false);
        }

    }
}