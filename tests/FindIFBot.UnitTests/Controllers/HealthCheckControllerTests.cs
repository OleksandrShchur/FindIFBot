using FindIFBot.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace FindIFBot.UnitTests.Controllers
{
    public class HealthCheckControllerTests
    {
        [Fact]
        public void Given_Request_When_Get_Then_Returns200WithHealthyPayload()
        {
            var sut = new HealthCheckController();

            var result = sut.Get();

            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be("FindIFBot is healthy!");
        }
    }
}
