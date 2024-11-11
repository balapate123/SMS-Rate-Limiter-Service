using Microsoft.AspNetCore.Mvc;
using Moq;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TestRateLimiterService.Controllers;
using TestRateLimiterService.Services.RateLimiterService;
using Xunit;
using System.Net;

namespace TestRateLimiterService.Tests
{
    public class RateLimiterControllerTests
    {
        private readonly RateLimiterController _controller;
        private readonly RateLimiterService _rateLimiterService;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly Mock<IServer> _mockServer;

        public RateLimiterControllerTests()
        {
            _mockDatabase = new Mock<IDatabase>();
            _mockRedis = new Mock<IConnectionMultiplexer>();
            _mockServer = new Mock<IServer>();

            // Set up Redis to return the mocked database
            _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);

            // Mock Redis endpoints and server behavior
            _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new EndPoint[] { new DnsEndPoint("localhost", 6379) });
            _mockRedis.Setup(r => r.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>())).Returns(_mockServer.Object);

            // In-memory configuration
            var inMemorySettings = new Dictionary<string, string> {
                { "MaxMessagesPerNumber", "5" },
                { "MaxMessagesPerAccount", "10" },
                { "ExpirationTimeSeconds", "60" }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _rateLimiterService = new RateLimiterService(_mockRedis.Object, configuration);
            _controller = new RateLimiterController(_rateLimiterService);
        }


        [Fact]
        public async Task GetAccountStats_ShouldReturnStats_WhenAccountExists()
        {
            // Arrange
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _mockDatabase.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(5);

            // Act
            var result = await _controller.GetAccountStats("testAccount");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AccountStatsResponse>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("testAccount", response.AccountId);
            Assert.Equal(5, response.MessageCount);
            Assert.Equal(10, response.MaxMessagesAllowed);
        }

        [Fact]
        public async Task GetPhoneNumberStats_ShouldReturnStats_WhenPhoneNumberExists()
        {
            // Arrange
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _mockDatabase.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(3);

            // Act
            var result = await _controller.GetPhoneNumberStats("testAccount", "1234567890");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PhoneNumberStatsResponse>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("1234567890", response.PhoneNumber);
            Assert.Equal(3, response.MessageCount);
            Assert.Equal(5, response.MaxMessagesAllowed);
        }
    }
}
