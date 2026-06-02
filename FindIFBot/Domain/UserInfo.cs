namespace FindIFBot.Domain
{
    public class UserInfo
    {
        public long Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsBot { get; set; }
        public bool IsPremium { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
    }
}
