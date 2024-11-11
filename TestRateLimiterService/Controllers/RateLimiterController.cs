using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TestRateLimiterService.Services.RateLimiterService;

namespace TestRateLimiterService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateLimiterController : ControllerBase
    {
        private readonly RateLimiterService _rateLimiterService;

        public RateLimiterController(RateLimiterService rateLimiterService)
        {
            _rateLimiterService = rateLimiterService;
        }

        // Endpoint to create a new account
        [HttpPost("create-account")]
        public async Task<IActionResult> CreateAccount([FromBody] AccountRequest request)
        {
            bool success = await _rateLimiterService.CreateAccountAsync(request.AccountId);
            if (!success) return Conflict(new { message = "Account already exists." });
            return Ok(new { message = "Account created successfully." });
        }

        // Endpoint to add a phone number to an existing account
        [HttpPost("add-phone-number")]
        public async Task<IActionResult> AddPhoneNumber([FromBody] PhoneNumberRequest request)
        {
            bool success = await _rateLimiterService.AddPhoneNumberAsync(request.AccountId, request.PhoneNumber);
            if (!success) return NotFound(new { message = "Account not found." });
            return Ok(new { message = "Phone number added successfully." });
        }

        // Endpoint to check if a message can be sent based on rate limits
        [HttpPost("can-send")]
        public async Task<IActionResult> CanSend([FromBody] PhoneNumberRequest request)
        {
            if (string.IsNullOrEmpty(request.AccountId) || string.IsNullOrEmpty(request.PhoneNumber))
                return BadRequest(new { message = "AccountId and PhoneNumber are required." });

            bool accountExists = await _rateLimiterService.AccountExistsAsync(request.AccountId);
            if (!accountExists)
            {
                return NotFound(new { message = "Account does not exist." });
            }

            bool phoneNumberExists = await _rateLimiterService.PhoneNumberExistsInAccountAsync(request.AccountId, request.PhoneNumber);
            if (!phoneNumberExists)
            {
                return NotFound(new { message = "Phone number not found in the specified account." });
            }

            var (canSend, message) = await _rateLimiterService.CanSendMessageAsync(request.AccountId, request.PhoneNumber);

            if (!canSend)
            {
                return StatusCode(429, new { canSend = false, message });
            }

            return Ok(new { canSend = true, message });
        }

        // Endpoint to retrieve statistics for a specific account
        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetAccountStats([FromRoute] string accountId)
        {
            var stats = await _rateLimiterService.GetAccountStatsAsync(accountId);

            if (!stats.Success)
            {
                return NotFound(new { message = stats.Message });
            }

            return Ok(stats);
        }

        // Endpoint to retrieve statistics for a specific phone number within an account
        [HttpGet("{accountId}/{phoneNumber}")]
        public async Task<IActionResult> GetPhoneNumberStats([FromRoute] string accountId, [FromRoute] string phoneNumber)
        {
            var stats = await _rateLimiterService.GetPhoneNumberStatsAsync(accountId, phoneNumber);

            if (!stats.Success)
            {
                return NotFound(new { message = stats.Message });
            }

            return Ok(stats);
        }

        [HttpGet("accounts")]
        public IActionResult GetAllAccounts()
        {
            var accountIds = _rateLimiterService.GetAllAccountIds();
            return Ok(accountIds);
        }


    }

    // Data transfer object (DTO) for account-related requests
    public class AccountRequest
    {
        public string? AccountId { get; set; }
    }

    // Data transfer object (DTO) for phone number-related requests
    public class PhoneNumberRequest
    {
        public string? AccountId { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
