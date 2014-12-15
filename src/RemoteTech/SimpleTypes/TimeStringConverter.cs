using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RemoteTech
{
    /// <summary>
    /// This class converts time strings like "1d 2m 2s" into a
    /// double value as seconds and also vice versa, based on
    /// earth time.
    /// </summary>
    class EarthTimeStringConverter : TimeStringConverter
    {
        /// <summary>
        /// Define the base seconds for days, hours and minutes
        /// </summary>
        public EarthTimeStringConverter()
        {
            this.SecondsPerYear = 31536000; // = 365d
            this.SecondsPerDay = 86400;     // = 24h
            this.SecondsPerHour = 3600;     // = 60m
            this.SecondsPerMinute = 60;     // = 60s
        }
    }

    /// <summary>
    /// This class converts time strings like "1d 2m 2s" into a
    /// double value as seconds and also vice versa, based on
    /// kerbin time.
    /// </summary>
    class KerbinTimeStringConverter : TimeStringConverter
    {
        /// <summary>
        /// Define the base seconds for days, hours and minutes
        /// </summary>
        public KerbinTimeStringConverter()
        {
            this.SecondsPerYear = 9201600;  // = 426d
            this.SecondsPerDay = 21600;     // = 6h
            this.SecondsPerHour = 3600;     // = 60m
            this.SecondsPerMinute = 60;     // = 60s
        }
    }

    /// <summary>
    /// This class converts time strings like "1d 2m 2s" into a
    /// double value as seconds and also vice versa.
    /// </summary>
    abstract class TimeStringConverter
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
            String.Format("{0}?{1}?{2}?{3}?{4}?",
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
        public Double parseString(String duration)
        {
            Double timeInSeconds = 0;
            MatchCollection matches = TimeStringConverter.DurationRegex.Matches(duration);

            foreach (Match match in matches)
            {
                if (match.Groups["seconds"].Success)
                {
                    timeInSeconds += Double.Parse(match.Groups["seconds"].Value);
                }
                if (match.Groups["minutes"].Success)
                {
                    timeInSeconds += Double.Parse(match.Groups["minutes"].Value) * this.SecondsPerMinute;
                }
                if (match.Groups["hours"].Success)
                {
                    timeInSeconds += Double.Parse(match.Groups["hours"].Value) * this.SecondsPerHour;
                }
                if (match.Groups["days"].Success)
                {
                    timeInSeconds += Double.Parse(match.Groups["days"].Value) * this.SecondsPerDay;
                }
                if (match.Groups["years"].Success)
                {
                    timeInSeconds += Double.Parse(match.Groups["years"].Value) * this.SecondsPerYear;
                }
            }

            // if we've no matches, try parsing the string as seconds
            if (timeInSeconds == 0)
            {
                double tmpTimeinSeconds = 0.0;
                if (Double.TryParse(duration, out tmpTimeinSeconds))
                {
                    timeInSeconds = tmpTimeinSeconds;
                }
            }

            return timeInSeconds;
        }

        /// <summary>
        /// This method will parse a time as seconds and returns the time string of this.
        /// </summary>
        /// <param name="duration">Time as seconds</param>
        /// <returns>Given time as seconds converted to a time string like "1d 2m 3s"</returns>
        public String parseDouble(Double duration)
        {
            Double time = duration;
            StringBuilder s = new StringBuilder();

            // extract years
            if (time >= this.SecondsPerYear)
                time = this.calcFromSecondsToSring(time, s, this.SecondsPerYear, "y");

            // extract days
            if (time >= this.SecondsPerDay)
                time = this.calcFromSecondsToSring(time, s, this.SecondsPerDay, "d");

            // extract hours
            if (time >= this.SecondsPerHour)
                time = this.calcFromSecondsToSring(time, s, this.SecondsPerHour, "h");

            // extract minutes
            if (time >= this.SecondsPerMinute)
                time = this.calcFromSecondsToSring(time, s, this.SecondsPerMinute, "m");

            // kill the micro seconds
            s.Append(time.ToString("F2"));
            s.Append("s");

            return s.ToString();
        }

        /// <summary>
        /// This method extracts the time segments
        /// </summary>
        /// <param name="time">Seconds to convert</param>
        /// <param name="appandTo">Stringbuilder to appand to</param>
        /// <param name="baseSeconds">Base for the calculation</param>
        /// <param name="prefix">Will be appand to the string builder</param>
        /// <returns>The remaining seconds</returns>
        private Double calcFromSecondsToSring(Double time, StringBuilder appandTo, uint baseSeconds, String prefix)
        {
            appandTo.Append(Math.Floor(time / baseSeconds));
            appandTo.Append(prefix);
            return (time % baseSeconds);
        }
    }
}
