namespace FindIFBot.Domain
{
    public class UserInfo
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsBot { get; set; }
        public bool IsPremium { get; set; }
        public string LanguageCode { get; set; }
    }
}
