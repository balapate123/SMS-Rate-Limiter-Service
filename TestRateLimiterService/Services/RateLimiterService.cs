using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TestRateLimiterService.Services.RateLimiterService
{
    // Response model for phone number statistics, containing status, message count, and message limit information
    public class PhoneNumberStatsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PhoneNumber { get; set; }
        public long MessageCount { get; set; }
        public int MaxMessagesAllowed { get; set; }
    }

    public class PhoneNumberStats
    {
        public string PhoneNumber { get; set; }
        public long MessageCount { get; set; }
    }


    // Response model for account statistics, with data on associated phone numbers and message limits
    public class AccountStatsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AccountId { get; set; }
        public long MessageCount { get; set; }
        public int MaxMessagesAllowed { get; set; }
        public List<string> PhoneNumbers { get; set; } = new List<string>();
    }

    // Core service for managing rate limits and account operations
    public class RateLimiterService
    {
        private readonly IDatabase _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly int _maxMessagesPerNumber; 
        private readonly int _maxMessagesPerAccount;
        private readonly TimeSpan _expirationTime ; 

        public RateLimiterService(IConnectionMultiplexer redis, IConfiguration configuration)
        {
            _cache = redis.GetDatabase();
            _redis = redis;

            _maxMessagesPerNumber = configuration.GetValue<int>("MaxMessagesPerNumber", 5);
            _maxMessagesPerAccount = configuration.GetValue<int>("MaxMessagesPerAccount", 10);
            _expirationTime = TimeSpan.FromSeconds(configuration.GetValue<int>("ExpirationTimeSeconds", 60));
        }

        // Creates a new account if it doesn't exist, otherwise returns false
        public virtual async Task<bool> CreateAccountAsync(string accountId)
        {
            var accountKey = $"account:{accountId}";
            if (await _cache.KeyExistsAsync(accountKey))
                return false;

            await _cache.StringSetAsync(accountKey, "active");
            return true;
        }

        // Adds a phone number to an existing account; returns false if the account doesn't exist
        public virtual async Task<bool> AddPhoneNumberAsync(string accountId, string phoneNumber)
        {
            var accountKey = $"account:{accountId}";
            if (!await _cache.KeyExistsAsync(accountKey))
                return false;

            var phoneKey = $"{accountId}:phone:{phoneNumber}";
            await _cache.StringSetAsync(phoneKey, "active");
            return true;
        }

        // Checks if an account exists in the cache
        public virtual async Task<bool> AccountExistsAsync(string accountId)
        {
            var accountKey = $"account:{accountId}";
            return await _cache.KeyExistsAsync(accountKey);
        }

        // Checks if a phone number is associated with an account
        public virtual async Task<bool> PhoneNumberExistsInAccountAsync(string accountId, string phoneNumber)
        {
            var phoneKey = $"{accountId}:phone:{phoneNumber}";
            return await _cache.KeyExistsAsync(phoneKey);
        }

        // Determines if a message can be sent based on rate limits for the phone number and account
        public virtual async Task<(bool success, string message)> CanSendMessageAsync(string accountId, string phoneNumber)
        {
            // Check account existence
            if (!await _cache.KeyExistsAsync($"account:{accountId}"))
            {
                return (false, "Account does not exist.");
            }

            // Check phone number association with account
            if (!await _cache.KeyExistsAsync($"{accountId}:phone:{phoneNumber}"))
            {
                return (false, "Phone number not found in the specified account.");
            }

            // Increment message count for the phone number
            var phoneKey = $"{accountId}:{phoneNumber}:count";
            var phoneCount = await _cache.StringIncrementAsync(phoneKey);

            // Set expiration on phone number count if it’s the first message
            if (phoneCount == 1)
            {
                await _cache.KeyExpireAsync(phoneKey, _expirationTime);
            }

            if (phoneCount > _maxMessagesPerNumber)
            {
                return (false, "Message limit exceeded for this phone number.");
            }

            // Increment message count for the account
            var accountKey = $"{accountId}:account-count";
            var accountCount = await _cache.StringIncrementAsync(accountKey);

            // Set expiration on account count if it’s the first message
            if (accountCount == 1)
            {
                await _cache.KeyExpireAsync(accountKey, _expirationTime);
            }

            if (accountCount > _maxMessagesPerAccount)
            {
                return (false, "Message limit exceeded for this account.");
            }

            return (true, "Message sent successfully.");
        }

        public List<string> GetAllAccountIds()
        {
            // Assuming Redis is storing accounts with a specific prefix like "account:{accountId}"
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var accountKeys = server.Keys(pattern: "account:*");

            List<string> accountIds = new List<string>();

            foreach (var key in accountKeys)
            {
                // Extract the account ID from the key name
                var accountId = key.ToString().Split(':').Last();
                accountIds.Add(accountId);
            }

            return accountIds;
        }



        // Retrieves account statistics, including associated phone numbers and message count
        public virtual async Task<AccountStatsResponse> GetAccountStatsAsync(string accountId)
        {
            if (!await AccountExistsAsync(accountId))
            {
                return new AccountStatsResponse
                {
                    Success = false,
                    Message = "Account not found."
                };
            }

            var accountLimitKey = $"{accountId}:account-count";
            long messageCount = (await _cache.StringGetAsync(accountLimitKey)).IsNull ? 0 : (long)await _cache.StringGetAsync(accountLimitKey);

            // Retrieve all phone numbers associated with this account
            var server = _redis.GetServer(_redis.GetEndPoints().FirstOrDefault() ?? throw new InvalidOperationException("No Redis endpoints found"));
            var phoneKeys = server.Keys(pattern: $"{accountId}:phone:*");
            var phoneNumbers = phoneKeys.Select(key => key.ToString().Split(':').Last()).ToList();

            return new AccountStatsResponse
            {
                Success = true,
                AccountId = accountId,
                MessageCount = messageCount,
                MaxMessagesAllowed = _maxMessagesPerAccount,
                PhoneNumbers = phoneNumbers
            };
        }

        // Retrieves phone number statistics, including message count and limit for the specific number
        public virtual async Task<PhoneNumberStatsResponse> GetPhoneNumberStatsAsync(string accountId, string phoneNumber)
        {
            if (!await AccountExistsAsync(accountId) || !await PhoneNumberExistsInAccountAsync(accountId, phoneNumber))
            {
                return new PhoneNumberStatsResponse
                {
                    Success = false,
                    Message = "Account or phone number not found."
                };
            }

            var phoneLimitKey = $"{accountId}:phone:{phoneNumber}:count";
            long messageCount = (await _cache.StringGetAsync(phoneLimitKey)).IsNull ? 0 : (long)await _cache.StringGetAsync(phoneLimitKey);

            return new PhoneNumberStatsResponse
            {
                Success = true,
                PhoneNumber = phoneNumber,
                MessageCount = messageCount,
                MaxMessagesAllowed = _maxMessagesPerNumber
            };
        }
    }
}
