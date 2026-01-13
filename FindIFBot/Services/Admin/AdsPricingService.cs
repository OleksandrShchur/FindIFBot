namespace FindIFBot.Services.Admin
{
    public class AdsPricingService : IAdsPricingService
    {
        public int CalculatePrice(int subscribersCount)
        {
            var price = subscribersCount / 20;

            return price < 50 ? 50 : price;
        }
    }
}
