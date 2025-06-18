using System.Text;

namespace QuranBlazor.Services
{
    public static class TransliterationService
    {
        public static event Action OnScriptChanged;

        public static void NotifyScriptChanged() => OnScriptChanged?.Invoke();

        public static string GetDisplayText(string originalText) // Renamed parameter for clarity
        {
            if (string.IsNullOrEmpty(originalText)) return originalText;

            string targetScript = Preferences.Get("UzbekScript", "Cyrillic");

            // Basic heuristic: if it contains mostly Latin chars, assume it's Latin. Otherwise, assume Cyrillic.
            // This is imperfect. A more robust solution might involve more complex detection or context.
            bool isInputLikelyLatin = IsPotentiallyLatin(originalText);

            if (targetScript == "Latin")
            {
                return isInputLikelyLatin ? originalText : ToLatin(originalText);
            }
            else // Target is Cyrillic
            {
                return isInputLikelyLatin ? ToCyrillic(originalText) : originalText;
            }
        }

        // To be used by components to know if they need to use ToCyrillic for search terms, for example
        public static bool IsLatinScriptSelected => Preferences.Get("UzbekScript", "Cyrillic") == "Latin";

        public static bool IsPotentiallyLatin(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            int latinChars = 0;
            int cyrillicChars = 0;
            foreach (char c in text)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == 'ʻ' || c == '’') // Common Latin or apostrophes used in Latin Uzbek
                {
                    latinChars++;
                }
                else if (c >= 'А' && c <= 'я' || c == 'Ў' || c == 'ў' || c == 'Қ' || c == 'қ' || c == 'Ғ' || c == 'ғ' || c == 'Ҳ' || c == 'ҳ')
                {
                    cyrillicChars++;
                }
            }
            // If more Latin-specific chars than Cyrillic, or some Latin and no Cyrillic, assume Latin.
            return latinChars > cyrillicChars || (latinChars > 0 && cyrillicChars == 0);
        }

        private static readonly Dictionary<char, string> CyrillicToLatinMap = new Dictionary<char, string>
        {
            { 'А', "A" }, { 'а', "a" }, { 'Б', "B" }, { 'б', "b" }, { 'Д', "D" }, { 'д', "d" },
            { 'Э', "E" }, { 'э', "e" }, { 'Ф', "F" }, { 'ф', "f" }, { 'Г', "G" }, { 'г', "g" },
            { 'Ҳ', "H" }, { 'ҳ', "h" },  { 'И', "I" }, { 'и', "i" }, { 'Ж', "J" }, { 'ж', "j" },
            { 'К', "K" }, { 'к', "k" }, { 'Л', "L" }, { 'л', "l" }, { 'М', "M" }, { 'м', "m" },
            { 'Н', "N" }, { 'н', "n" }, { 'О', "O" }, { 'о', "o" }, { 'П', "P" }, { 'п', "p" },
            { 'Қ', "Q" }, { 'қ', "q" }, { 'Р', "R" }, { 'р', "r" }, { 'С', "S" }, { 'с', "s" },
            { 'Т', "T" }, { 'т', "t" }, { 'У', "U" }, { 'у', "u" }, { 'В', "V" }, { 'в', "v" },
            { 'Х', "X" }, { 'х', "x" }, { 'Й', "Y" }, { 'й', "y" }, { 'З', "Z" }, { 'з', "z" },
            { 'Ў', "Oʻ" }, { 'ў', "oʻ" }, { 'Ғ', "Gʻ" }, { 'ғ', "gʻ" }, { 'Ш', "Sh" }, { 'ш', "sh" },
            { 'Ч', "Ch" }, { 'ч', "ch" }, { 'Ъ', "ʼ" }, { 'ъ', "ʼ" }, { 'Ё', "Yo" }, { 'ё', "yo" },
            { 'Ю', "Yu" }, { 'ю', "yu" }, { 'Я', "Ya" }, { 'я', "ya" }, { 'Ц', "Ts" }, { 'ц', "ts" },
        };
        // Very basic ToCyrillic - needs significant improvement for accuracy
        private static readonly Dictionary<string, string> LatinToCyrillicMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "A", "А" }, { "B", "Б" }, { "V", "В" }, { "G", "Г" },
            { "D", "Д" }, { "Ye", "Е" }, { "E", "Е" }, // Simplified
            { "Yo", "Ё" }, { "J", "Ж" }, { "Z", "З" }, { "I", "И" },
            { "Y", "Й" }, { "K", "К" }, { "L", "Л" }, { "M", "М" },
            { "N", "Н" }, { "O", "О" }, { "P", "П" }, { "R", "Р" },
            { "S", "С" }, { "T", "Т" }, { "U", "У" }, { "F", "Ф" },
            { "X", "Х" }, { "Ts", "Ц" }, { "Ch", "Ч" }, { "Sh", "Ш" },
            { "Yu", "Ю" }, { "Ya", "Я" }, { "Oʻ", "Ў" }, { "Q", "Қ" },
            { "Gʻ", "Ғ" }, { "H", "Ҳ" }
            // This needs to handle digraphs (sh, ch, gʻ, oʻ) carefully during conversion
        };

        public static string ToLatin(string cyrillicText)
        {
            if (string.IsNullOrEmpty(cyrillicText)) return cyrillicText;

            var latinText = new StringBuilder(cyrillicText.Length);
            foreach (char c in cyrillicText)
            {
                if (CyrillicToLatinMap.TryGetValue(c, out var latinChar))
                {
                    latinText.Append(latinChar);
                }
                else
                {
                    latinText.Append(c); // Append original if no mapping (e.g., punctuation)
                }
            }
            return latinText.ToString();
        }

        // Add ToCyrillic if needed for search keyword conversion
        public static string ToCyrillic(string latinText)
        {
            if (string.IsNullOrEmpty(latinText)) return latinText;
            var cyrillicText = new StringBuilder(latinText.Length);
            int i = 0;
            while (i < latinText.Length)
            {
                bool matched = false;
                // Check for 2-character sequences first (e.g., "Oʻ", "Gʻ", "Sh", "Ch")
                if (i + 1 < latinText.Length)
                {
                    string twoCharSeq = latinText.Substring(i, 2);
                    if (LatinToCyrillicMap.TryGetValue(twoCharSeq, out var cyrTwoChar))
                    {
                        cyrillicText.Append(cyrTwoChar);
                        i += 2;
                        matched = true;
                    }
                }
                // If no 2-character match, check for 1-character
                if (!matched)
                {
                    string oneCharSeq = latinText.Substring(i, 1);
                    if (LatinToCyrillicMap.TryGetValue(oneCharSeq, out var cyrOneChar))
                    {
                        cyrillicText.Append(cyrOneChar);
                    }
                    else
                    {
                        cyrillicText.Append(oneCharSeq); // Append original if no mapping
                    }
                    i += 1;
                }
            }
            return cyrillicText.ToString();
        }
    }
}