using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RemoteTech.Common.Utils
{
    /// <summary>
    /// This class converts time strings like "1d 2m 2s" into a
    /// double value as seconds and also vice versa, based on
    /// earth time.
    /// </summary>
    internal class EarthTimeStringConverter : TimeStringConverter
    {
        /// <summary>
        /// Define the base seconds for days, hours and minutes
        /// </summary>
        public EarthTimeStringConverter()
        {
            SecondsPerYear = 31536000; // = 365d
            SecondsPerDay = 86400;     // = 24h
            SecondsPerHour = 3600;     // = 60m
            SecondsPerMinute = 60;     // = 60s
        }
    }

    /// <summary>
    /// This class converts time strings like "1d 2m 2s" into a
    /// double value as seconds and also vice versa, based on
    /// Kerbin time.
    /// </summary>
    internal class KerbinTimeStringConverter : TimeStringConverter
    {
        /// <summary>
        /// Define the base seconds for days, hours and minutes
        /// </summary>
        public KerbinTimeStringConverter()
        {
            SecondsPerYear = 9201600;  // = 426d
            SecondsPerDay = 21600;     // = 6h
            SecondsPerHour = 3600;     // = 60m
            SecondsPerMinute = 60;     // = 60s
        }
    }

    /// <summary>
    /// This class converts time strings like "1d 2m 2s" into a
    /// double value as seconds and also vice versa.
    /// </summary>
    internal abstract class TimeStringConverter
    {
        /// <summary>
        /// Get the seconds for one year
        /// </summary>
        protected uint SecondsPerYear;
        /// <summary>
        /// Get the seconds for one day
        /// </summary>
        protected uint SecondsPerDay;
        /// <summary>
        /// Get the seconds for one hour
        /// </summary>
        protected uint SecondsPerHour;
        /// <summary>
        /// Get the seconds for one minute
        /// </summary>
        protected uint SecondsPerMinute;
        /// <summary>
        /// Expression for parsing the time string
        /// </summary>
        private static readonly Regex DurationRegex = new Regex(
            string.Format("{0}?{1}?{2}?{3}?{4}?",
                @"(?:(?<seconds>\d*\.?\d+)\s*s[a-z]*[,\s]*)",
                @"(?:(?<minutes>\d*\.?\d+)\s*m[a-z]*[,\s]*)",
                @"(?:(?<hours>\d*\.?\d+)\s*h[a-z]*[,\s]*)",
                @"(?:(?<days>\d*\.?\d+)\s*d[a-z]*[,\s]*)",
                @"(?:(?<years>\d*\.?\d+)\s*y[a-z]*[,\s]*)"));

        /// <summary>
        /// This method will parse a time string like "1d 2m 3s" and returns the
        /// seconds for this string. If no matching string was found with the
        /// "DurationRegex" we'll try to parse the given duration string as seconds.
        /// </summary>
        /// <param name="duration">time string like "1d 2m 3s" or "500" (as seconds).
        ///                        Possible suffixes: y,d,h,m and s</param>
        /// <returns>Given time string converted in seconds</returns>
        public double ParseString(string duration)
        {
            double timeInSeconds = 0;
            var matches = DurationRegex.Matches(duration);

            foreach (Match match in matches)
            {
                if (match.Groups["seconds"].Success)
                {
                    timeInSeconds += double.Parse(match.Groups["seconds"].Value);
                }
                if (match.Groups["minutes"].Success)
                {
                    timeInSeconds += double.Parse(match.Groups["minutes"].Value) * SecondsPerMinute;
                }
                if (match.Groups["hours"].Success)
                {
                    timeInSeconds += double.Parse(match.Groups["hours"].Value) * SecondsPerHour;
                }
                if (match.Groups["days"].Success)
                {
                    timeInSeconds += double.Parse(match.Groups["days"].Value) * SecondsPerDay;
                }
                if (match.Groups["years"].Success)
                {
                    timeInSeconds += double.Parse(match.Groups["years"].Value) * SecondsPerYear;
                }
            }

            if (Math.Abs(timeInSeconds) > float.Epsilon)
                return timeInSeconds;

            // if we've no matches, try parsing the string as seconds
            double tmpTimeinSeconds;
            if (double.TryParse(duration, out tmpTimeinSeconds))
            {
                timeInSeconds = tmpTimeinSeconds;
            }

            return timeInSeconds;
        }

        /// <summary>
        /// This method will parse a time as seconds and returns the time string of this.
        /// </summary>
        /// <param name="duration">Time as seconds</param>
        /// <param name="withMicroSecs">[optional] Add the microseconds to the time string, default true</param>
        /// <returns>Given time as seconds converted to a time string like "1d 2m 3s"</returns>
        public string ParseDouble(double duration, bool withMicroSecs = true)
        {
            var time = duration;
            var s = new StringBuilder();

            // extract years
            if (time >= SecondsPerYear)
                time = CalcFromSecondsToSring(time, s, SecondsPerYear, "y");

            // extract days
            if (time >= SecondsPerDay)
                time = CalcFromSecondsToSring(time, s, SecondsPerDay, "d");

            // extract hours
            if (time >= SecondsPerHour)
                time = CalcFromSecondsToSring(time, s, SecondsPerHour, "h");

            // extract minutes
            if (time >= SecondsPerMinute)
                time = CalcFromSecondsToSring(time, s, SecondsPerMinute, "m");


            s.Append(withMicroSecs ? time.ToString("F2") : time.ToString("F0"));
            s.Append("s");

            return s.ToString();
        }

        /// <summary>
        /// This method extracts the time segments
        /// </summary>
        /// <param name="time">Seconds to convert</param>
        /// <param name="appendTo"><see cref="StringBuilder"/> to append to</param>
        /// <param name="baseSeconds">Base for the calculation</param>
        /// <param name="prefix">Will be append to the string builder</param>
        /// <returns>The remaining seconds</returns>
        private static double CalcFromSecondsToSring(double time, StringBuilder appendTo, uint baseSeconds, string prefix)
        {
            appendTo.Append(Math.Floor(time / baseSeconds));
            appendTo.Append(prefix);
            return (time % baseSeconds);
        }
    }
}
