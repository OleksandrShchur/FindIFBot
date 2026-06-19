using System.Net;
using System.Net.Http.Json;
using System.Text;
using FindIFBot.Services;
using Telegram.Bot.Types;

namespace FindIFBot.IntegrationTests.Http
{
    public class HttpEndpointsTests
    {
        private const string ValidUpdateJson =
            """
            {
              "update_id": 7,
              "message": {
                "message_id": 11,
                "date": 1700000000,
                "chat": { "id": 42, "type": "private" },
                "from": { "id": 42, "is_bot": false, "first_name": "Tester" },
                "text": "hello"
              }
            }
            """;

        private static StringContent UpdateContent() =>
            new(ValidUpdateJson, Encoding.UTF8, "application/json");

        [Fact]
        public async Task Given_HealthEndpoint_When_Get_Then_Returns200()
        {
            using var factory = new TestWebAppFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/api/healthcheck");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("healthy");
        }

        [Fact]
        public async Task Given_ValidUpdate_When_PostWebhook_Then_Returns200AndDispatches()
        {
            using var factory = new TestWebAppFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync("/api/telegram/webhook", UpdateContent());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await factory.Dispatcher.Received(1).DispatchAsync(Arg.Is<Update>(u => u.Id == 7));
        }

        [Fact]
        public async Task Given_DispatcherThrows_When_PostWebhook_Then_Still200()
        {
            using var factory = new TestWebAppFactory();
            factory.Dispatcher.DispatchAsync(Arg.Any<Update>())
                .Returns(Task.FromException(new InvalidOperationException("downstream failure")));
            var client = factory.CreateClient();

            var response = await client.PostAsync("/api/telegram/webhook", UpdateContent());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Given_NoKeyHeader_When_PostMaintenance_Then_Returns400AndServiceNotCalled()
        {
            using var factory = new TestWebAppFactory();
            var client = factory.CreateClient();

            var response = await client.PostAsync("/api/maintenance/process-yesterday-logs", content: null);

            // [ApiController] rejects the absent required [FromHeader] string via automatic model
            // validation (400) before the action runs — the service is still never invoked.
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await factory.Maintenance.DidNotReceiveWithAnyArgs().ProcessYesterdayLogsAsync();
        }

        [Fact]
        public async Task Given_WrongKey_When_PostMaintenance_Then_Returns401()
        {
            using var factory = new TestWebAppFactory();
            var client = factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/maintenance/process-yesterday-logs");
            request.Headers.Add("X-Maintenance-Key", "wrong-key");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await factory.Maintenance.DidNotReceiveWithAnyArgs().ProcessYesterdayLogsAsync();
        }

        [Fact]
        public async Task Given_CorrectKeyAndServiceSucceeds_When_PostMaintenance_Then_Returns200()
        {
            using var factory = new TestWebAppFactory();
            var client = factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/maintenance/process-yesterday-logs");
            request.Headers.Add("X-Maintenance-Key", TestWebAppFactory.MaintenanceKey);

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await factory.Maintenance.Received(1).ProcessYesterdayLogsAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Given_CorrectKeyButServiceThrows_When_PostMaintenance_Then_Returns500()
        {
            using var factory = new TestWebAppFactory();
            factory.Maintenance.ProcessYesterdayLogsAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("processing failed")));
            var client = factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/maintenance/process-yesterday-logs");
            request.Headers.Add("X-Maintenance-Key", TestWebAppFactory.MaintenanceKey);

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }
}
