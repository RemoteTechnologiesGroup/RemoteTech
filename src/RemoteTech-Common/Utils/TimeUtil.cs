namespace RemoteTech.Common.Utils
{
    public static class TimeUtil
    {
        /// <summary>Format a <see cref="double"/>duration into a string.</summary>
        /// <param name="duration">The time duration as a double.</param>
        /// <param name="withMicroSecs">Whether or not to include microseconds in the output.</param>
        /// <returns>A string corresponding to the <paramref name="duration"/> input parameter.</returns>
        public static string FormatDuration(double duration, bool withMicroSecs = true)
        {
            TimeStringConverter time;

            if (GameSettings.KERBIN_TIME)
            {
                time = new KerbinTimeStringConverter();
            }
            else
            {
                time = new EarthTimeStringConverter();
            }

            return time.ParseDouble(duration, withMicroSecs);
        }

        /// <summary>Generates a string for use in flight log entries.</summary>
        /// <returns>A string in the same format as used by stock flight log events</returns>
        /// <param name="years">The number of full years the mission has lasted</param>
        /// <param name="days">The number of additional days the mission has lasted</param>
        /// <param name="hours">The number of additional hours the mission has lasted</param>
        /// <param name="minutes">The number of additional minutes the mission has lasted</param>
        /// <param name="seconds">The number of additional seconds the mission has lasted</param>
        /// <precondition>All numerical arguments non-negative</precondition>
        /// <exceptionsafe>Does not throw exceptions</exceptionsafe>
        public static string FormatTimestamp(int years, int days, int hours, int minutes, int seconds)
        {
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        /// <summary>The simulation time, in seconds, since this save was started.</summary>
        public static double GameTime => Planetarium.GetUniversalTime();

        /// <summary>
        /// Try to parse a duration from a string to a double value.
        /// </summary>
        /// <param name="duration">A duration, as a string.</param>
        /// <returns>The <see cref="double"/> value corresponding to the <paramref name="duration"/> input string.</returns>
        public static double TryParseDuration(string duration)
        {
            TimeStringConverter time;

            if (GameSettings.KERBIN_TIME)
            {
                time = new KerbinTimeStringConverter();
            }
            else
            {
                time = new EarthTimeStringConverter();
            }

            return time.ParseString(duration);
        }
    }
}
