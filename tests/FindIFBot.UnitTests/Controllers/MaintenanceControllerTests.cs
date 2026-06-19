using FindIFBot.Configuration;
using FindIFBot.Controllers;
using FindIFBot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindIFBot.UnitTests.Controllers
{
    public class MaintenanceControllerTests
    {
        private const string ValidKey = "super-secret-key";

        private readonly IMaintenanceService _service = Substitute.For<IMaintenanceService>();
        private readonly MaintenanceController _sut;

        public MaintenanceControllerTests()
        {
            var options = Options.Create(new MaintenanceOptions { SecretKey = ValidKey });
            _sut = new MaintenanceController(_service, options, Substitute.For<ILogger<MaintenanceController>>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("wrong-key")]
        public async Task Given_MissingOrInvalidKey_When_ProcessYesterdayLogs_Then_Returns401(string? key)
        {
            var result = await _sut.ProcessYesterdayLogs(key!);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            await _service.DidNotReceiveWithAnyArgs().ProcessYesterdayLogsAsync();
        }

        [Fact]
        public async Task Given_ValidKey_When_ProcessYesterdayLogs_Then_CallsServiceAndReturns200()
        {
            var result = await _sut.ProcessYesterdayLogs(ValidKey);

            result.Should().BeOfType<OkObjectResult>();
            await _service.Received(1).ProcessYesterdayLogsAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Given_ValidKey_When_GenerateDailyStatistics_Then_CallsServiceAndReturns200()
        {
            var result = await _sut.GenerateDailyStatistics(ValidKey);

            result.Should().BeOfType<OkObjectResult>();
            await _service.Received(1).SendDailyStatisticsAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Given_ServiceThrows_When_ProcessYesterdayLogs_Then_Returns500()
        {
            _service.ProcessYesterdayLogsAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("boom")));

            var result = await _sut.ProcessYesterdayLogs(ValidKey);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Given_ServiceThrows_When_GenerateDailyStatistics_Then_Returns500()
        {
            _service.SendDailyStatisticsAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("boom")));

            var result = await _sut.GenerateDailyStatistics(ValidKey);

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }
    }
}
