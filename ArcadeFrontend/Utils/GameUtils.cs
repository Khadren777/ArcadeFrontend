using System;
using System.Collections.Generic;
using System.Text;

namespace ArcadeFrontend.Utils
{
    internal class GameUtils
    {
        private static string CleanGameTitle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            // Replace underscores and dots
            string title = raw.Replace("_", " ").Replace(".", " ");

            // Basic spacing for numbers
            title = System.Text.RegularExpressions.Regex.Replace(title, "(\\d+)", " $1");

            // Capitalize words
            title = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                .ToTitleCase(title.ToLower());

            return title.Trim();
        }
    }
}
