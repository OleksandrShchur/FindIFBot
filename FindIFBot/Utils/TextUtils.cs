namespace FindIFBot.Utils
{
    public static class TextUtils
    {
        public static string GetTextPreview(string text)
        {
            string input = (text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            if (input.Length <= 15)
            {
                return input;
            }

            string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string firstThreeWords = string.Join(" ", words.Take(3));

            string truncated;
            if (firstThreeWords.Length <= 15)
            {
                truncated = firstThreeWords;
            }
            else
            {
                truncated = input.Substring(0, 15);
            }

            return truncated + "...";
        }
    }
}
