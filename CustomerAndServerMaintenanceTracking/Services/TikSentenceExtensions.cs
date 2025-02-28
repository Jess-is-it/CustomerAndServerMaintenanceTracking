using System;
using System.Linq;
using tik4net;

namespace CustomerAndServerMaintenanceTracking.Services
{
    public static class TikSentenceExtensions
    {
        /// <summary>
        /// Parses the raw sentence (using ToString()) and returns the value for the specified attribute.
        /// Assumes the raw sentence is formatted as "key=value|key2=value2|..."
        /// </summary>
        public static string GetAttributeValue(this ITikReSentence sentence, string attributeName)
        {
            // Use ToString() to get the raw sentence string
            string rawSentence = sentence.ToString();
            if (string.IsNullOrEmpty(rawSentence))
                return string.Empty;

            // Split the raw sentence into parts using the '|' character as a delimiter
            var parts = rawSentence.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    if (keyValue[0].Trim().Equals(attributeName, StringComparison.OrdinalIgnoreCase))
                    {
                        return keyValue[1].Trim();
                    }
                }
            }
            return string.Empty;
        }
    }
}
