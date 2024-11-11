using Moq;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TestRateLimiterService.Services.RateLimiterService;
using Xunit;

namespace RateLimiterTests.Services
{
    public class RateLimiterServiceTests
    {
        private readonly RateLimiterService _rateLimiterService;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly Mock<IServer> _mockServer;

        

        public RateLimiterServiceTests()
        {
            // Set up mocks for Redis components
            _mockDatabase = new Mock<IDatabase>();
            _mockRedis = new Mock<IConnectionMultiplexer>();
            _mockServer = new Mock<IServer>();

            _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
            _mockRedis.Setup(r => r.GetServer(It.IsAny<EndPoint>(), null)).Returns(_mockServer.Object);

            var inMemorySettings = new Dictionary<string, string> {
                { "MaxMessagesPerNumber", "5" },
                { "MaxMessagesPerAccount", "10" },
                { "ExpirationTimeSeconds", "60" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Instantiate the RateLimiterService with mocks and configuration
            _rateLimiterService = new RateLimiterService(_mockRedis.Object, configuration);
        }

        // Test for creating an account that doesn't already exist
        [Fact]
        public async Task CreateAccountAsync_ShouldReturnTrue_WhenAccountDoesNotExist()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);
            _mockDatabase.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, When.Always, CommandFlags.None)).ReturnsAsync(true);

            var result = await _rateLimiterService.CreateAccountAsync("testAccount");

            Assert.True(result);
        }

        // Test for trying to create an account that already exists
        [Fact]
        public async Task CreateAccountAsync_ShouldReturnFalse_WhenAccountAlreadyExists()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

            var result = await _rateLimiterService.CreateAccountAsync("testAccount");

            Assert.False(result);
        }

        // Test for adding a phone number to an existing account
        [Fact]
        public async Task AddPhoneNumberAsync_ShouldReturnTrue_WhenAccountExists()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _mockDatabase.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, When.Always, CommandFlags.None)).ReturnsAsync(true);

            var result = await _rateLimiterService.AddPhoneNumberAsync("testAccount", "1234567890");

            Assert.True(result);
        }

        // Test for trying to add a phone number to a non-existent account
        [Fact]
        public async Task AddPhoneNumberAsync_ShouldReturnFalse_WhenAccountDoesNotExist()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);

            var result = await _rateLimiterService.AddPhoneNumberAsync("testAccount", "1234567890");

            Assert.False(result);
        }

        // Test for checking message sending when the limit is not exceeded
        [Fact]
        public async Task CanSendMessageAsync_ShouldReturnTrue_WhenUnderLimit()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _mockDatabase.Setup(db => db.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>())).ReturnsAsync(1);

            var (canSend, _) = await _rateLimiterService.CanSendMessageAsync("testAccount", "1234567890");

            Assert.True(canSend);
        }

        // Test for exceeding the phone number-specific limit
        [Fact]
        public async Task CanSendMessageAsync_ShouldReturnFalse_WhenPhoneNumberOverLimit()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _mockDatabase.SetupSequence(db => db.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>())).ReturnsAsync(6);

            var (canSend, message) = await _rateLimiterService.CanSendMessageAsync("testAccount", "1234567890");

            Assert.False(canSend);
            Assert.Equal("Message limit exceeded for this phone number.", message);
        }

        // Test for exceeding the account-wide message limit
        [Fact]
        public async Task CanSendMessageAsync_ShouldReturnFalse_WhenAccountOverLimit()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _mockDatabase.Setup(db => db.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>())).ReturnsAsync(11);

            var (canSend, message) = await _rateLimiterService.CanSendMessageAsync("testAccount", "1234567890");

            Assert.False(canSend);
            Assert.Equal("Message limit exceeded for this phone number.", message);
        }

        // Test for attempting to retrieve stats for a non-existent account
        [Fact]
        public async Task GetAccountStatsAsync_ShouldReturnFailure_WhenAccountDoesNotExist()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);

            var stats = await _rateLimiterService.GetAccountStatsAsync("testAccount");

            Assert.False(stats.Success);
            Assert.Equal("Account not found.", stats.Message);
        }

        // Test for retrieving statistics of an existing phone number within an account
        [Fact]
        public async Task GetPhoneNumberStatsAsync_ShouldReturnStats_WhenPhoneNumberExists()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _mockDatabase.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(3);

            var stats = await _rateLimiterService.GetPhoneNumberStatsAsync("testAccount", "1234567890");

            Assert.True(stats.Success);
            Assert.Equal("1234567890", stats.PhoneNumber);
            Assert.Equal(3, stats.MessageCount);
            Assert.Equal(5, stats.MaxMessagesAllowed);
        }

        // Test for attempting to retrieve stats for a non-existent phone number within an account
        [Fact]
        public async Task GetPhoneNumberStatsAsync_ShouldReturnFailure_WhenPhoneNumberDoesNotExist()
        {
            _mockDatabase.Setup(db => db.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(false);

            var stats = await _rateLimiterService.GetPhoneNumberStatsAsync("testAccount", "1234567890");

            Assert.False(stats.Success);
            Assert.Equal("Account or phone number not found.", stats.Message);
        }
    }
}
