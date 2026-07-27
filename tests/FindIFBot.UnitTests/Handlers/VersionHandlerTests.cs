using FindIFBot.Handlers;
using FindIFBot.Helpers;

namespace FindIFBot.UnitTests.Handlers
{
    public class VersionHandlerTests
    {
        [Fact]
        public void Given_Handler_When_Handle_Then_ReturnsUkrainianVersionText()
        {
            var sut = new VersionHandler();

            var text = sut.Handle();

            text.Should().Contain("Версія бота");
            text.Should().Contain(BotVersion.Current.ToString());
        }
    }
}
