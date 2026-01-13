namespace FindIFBot.Services.Admin
{
    public interface IAdsPricingService
    {
        int CalculatePrice(int subscribersCount);
    }
}
