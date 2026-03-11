using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace HPScreen.Entities
{
    public class ValueColorizer
    {
        // Five-tier color scale from worst to best
        private static readonly Color VeryPoorColor = new Color(255, 60, 60);    // Bright red
        private static readonly Color PoorColor     = new Color(255, 180, 180);  // Blush
        private static readonly Color AverageColor  = Color.White;               // White
        private static readonly Color GoodColor     = new Color(144, 238, 144);  // Light green
        private static readonly Color VeryGoodColor = new Color(50, 255, 50);    // Bright green

        private enum Direction { Higher, Lower }
        private enum ParseMode { Number, First, Sum }

        private class ThresholdDef
        {
            public Direction Direction;
            public ParseMode Parse;
            public float T1, T2, T3, T4;
        }

        private Dictionary<string, ThresholdDef> _thresholds;

        private ValueColorizer()
        {
            _thresholds = new Dictionary<string, ThresholdDef>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the quality-based color for a given label and raw value string.
        /// Labels without a threshold definition return white.
        /// </summary>
        public Color GetColor(string label, string rawValue)
        {
            if (!_thresholds.TryGetValue(label, out var def))
                return AverageColor;

            float? parsed = ParseValue(rawValue, def.Parse);
            if (!parsed.HasValue)
                return AverageColor;

            float val = parsed.Value;

            // Determine which of the 5 zones the value falls into (ascending thresholds)
            int zone; // 0 = lowest zone, 4 = highest zone
            if (val <= def.T1)      zone = 0;
            else if (val <= def.T2) zone = 1;
            else if (val <= def.T3) zone = 2;
            else if (val <= def.T4) zone = 3;
            else                    zone = 4;

            // Map zone to quality based on direction
            if (def.Direction == Direction.Higher)
            {
                // Higher is better: zone 0 = very poor, zone 4 = very good
                return ZoneToColor(zone);
            }
            else
            {
                // Lower is better: zone 0 = very good, zone 4 = very poor (reverse)
                return ZoneToColor(4 - zone);
            }
        }

        private static Color ZoneToColor(int qualityZone)
        {
            switch (qualityZone)
            {
                case 0: return VeryPoorColor;
                case 1: return PoorColor;
                case 2: return AverageColor;
                case 3: return GoodColor;
                case 4: return VeryGoodColor;
                default: return AverageColor;
            }
        }

        private static float? ParseValue(string raw, ParseMode mode)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            switch (mode)
            {
                case ParseMode.Number:
                    if (float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float n))
                        return n;
                    return null;

                case ParseMode.First:
                    var firstMatch = Regex.Match(raw, @"-?\d+\.?\d*");
                    if (firstMatch.Success && float.TryParse(firstMatch.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                        return f;
                    return null;

                case ParseMode.Sum:
                    float total = 0;
                    bool found = false;
                    foreach (Match m in Regex.Matches(raw, @"-?\d+\.?\d*"))
                    {
                        if (float.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                        {
                            total += v;
                            found = true;
                        }
                    }
                    return found ? total : (float?)null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Loads threshold definitions from a text file.
        /// </summary>
        public static ValueColorizer LoadFromFile(string filePath)
        {
            var colorizer = new ValueColorizer();
            var lines = File.ReadAllLines(filePath);

            string label = null;
            Direction dir = Direction.Higher;
            ParseMode parse = ParseMode.Number;
            float t1 = 0, t2 = 0, t3 = 0, t4 = 0;
            bool inside = false;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                // Skip comments and blanks
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                if (line == "[THRESHOLD]")
                {
                    inside = true;
                    label = null;
                    dir = Direction.Higher;
                    parse = ParseMode.Number;
                    t1 = t2 = t3 = t4 = 0;
                    continue;
                }

                if (line == "[END]")
                {
                    if (inside && label != null)
                    {
                        colorizer._thresholds[label] = new ThresholdDef
                        {
                            Direction = dir,
                            Parse = parse,
                            T1 = t1, T2 = t2, T3 = t3, T4 = t4
                        };
                    }
                    inside = false;
                    continue;
                }

                if (!inside)
                    continue;

                int eqIndex = line.IndexOf('=');
                if (eqIndex < 0)
                    continue;

                string key = line.Substring(0, eqIndex).Trim();
                string value = line.Substring(eqIndex + 1).Trim();

                switch (key)
                {
                    case "Label":
                        label = value;
                        break;
                    case "Direction":
                        dir = value.Equals("lower", StringComparison.OrdinalIgnoreCase)
                            ? Direction.Lower : Direction.Higher;
                        break;
                    case "Parse":
                        if (value.Equals("first", StringComparison.OrdinalIgnoreCase))
                            parse = ParseMode.First;
                        else if (value.Equals("sum", StringComparison.OrdinalIgnoreCase))
                            parse = ParseMode.Sum;
                        else
                            parse = ParseMode.Number;
                        break;
                    case "T1":
                        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out t1);
                        break;
                    case "T2":
                        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out t2);
                        break;
                    case "T3":
                        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out t3);
                        break;
                    case "T4":
                        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out t4);
                        break;
                }
            }

            return colorizer;
        }
    }
}
