using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.BackgroundJobs
{
    public class UserStatusCheckerStub : IUserStatusChecker
    {
        private readonly ILogger<RemoveDeactivatedMemberships> _logger; //depends on that background job
        private readonly SelfServiceDbContext _context;
        private string? _authToken;

        public UserStatusCheckerStub(SelfServiceDbContext context, ILogger<RemoveDeactivatedMemberships> logger)
        {
            _context = context;
            _logger = logger;
            SetAuthToken();
        }

        private async void SetAuthToken()
        {
            return; //so we can await it
        }

        public async Task<(bool, string)> MakeUserRequest(string userId)
        {
            if (userId == "bonga")
            {
                return (true, "deactivated");
            }
            else if (userId == "bongus")
            {
                return (false, "");
            }
            else{
                return (false, "undefined for this test");
            }
            // The rest of the implementation remains the same
            // ...
        }
    }
}
