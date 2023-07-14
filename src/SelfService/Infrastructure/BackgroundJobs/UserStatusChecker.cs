using System.Net;
using System.Net.Http.Headers;

using System.Text.Json;
using System.Text.Json.Serialization;
using SelfService.Infrastructure.Persistence;


namespace SelfService.Infrastructure.BackgroundJobs;

public class User //TODO: refactor out
{
    [JsonPropertyName("@odata.context")]
    public string? OdataContext { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("accountEnabled")]
    public bool AccountEnabled { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("mail")]
    public string? Mail { get; set; }

    [JsonPropertyName("identities")]
    public List<Identity>? Identities { get; set; }
}

public class Identity //TODO: refactor out
{
    [JsonPropertyName("signInType")]
    public string? SignInType { get; set; }

    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("issuerAssignedId")]
    public string? IssuerAssignedId { get; set; }
}

public class AuthTokenResponse
{
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("ext_expires_in")]
    public int ExternalExpiresIn { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}

public class UserStatusChecker : IUserStatusChecker
{
    private readonly ILogger<RemoveDeactivatedMemberships> _logger; //depends on that background job
    //private readonly SelfServiceDbContext _context;
    private string? _authToken;

    public UserStatusChecker(ILogger<RemoveDeactivatedMemberships> logger)
    {
        _logger = logger;
        SetAuthToken();
    }
    private async void SetAuthToken()
    {
        /*
            makes an MS-Graph request to get the temporary creds
            to be able to view users
        */
        // Get the values from environment variables
        string? tenant_id = Environment.GetEnvironmentVariable("SS_MSGRAPH_TENANT_ID");
        string? client_id = Environment.GetEnvironmentVariable("SS_MSGRAPH_CLIENT_ID");
        string? client_secret = Environment.GetEnvironmentVariable("SS_MSGRAPH_CLIENT_SECRET");
        if (client_secret == null)
        {
            _logger.LogError("[UserStatusChecker] `client_secret` not found in environment");
            return;
        }
        if (client_id == null)
        {
            _logger.LogError("[UserStatusChecker] `client_id` not found in environment");
            return;
        }
        if (tenant_id == null)
        {
            _logger.LogError("[UserStatusChecker] `tenant_id` not found in environment");
            return;
        }

        //setup request
        string url = $"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token ";
        HttpClient client = new HttpClient();
        string formData = $"client_id={client_id}&grant_type=client_credentials&scope=https://graph.microsoft.com/.default&client_secret={client_secret}";

        HttpContent content = new StringContent(formData, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        //make request and handle response
        try
        {
            HttpResponseMessage response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string responseText = await response.Content.ReadAsStringAsync();
                AuthTokenResponse? authTokenResponse = JsonSerializer.Deserialize<AuthTokenResponse>(responseText);
                if (authTokenResponse != null)
                {
                    _authToken = authTokenResponse.AccessToken;
                }
                else
                {
                    _logger.LogError($"[UserStatusChecker] Unable to deserialize AuthTokenReponse. Got: {responseText}");
                }
            }
            else
            {
                _logger.LogError($"[UserStatusChecker] Unable to request AccessToken, failed with {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[UserStatusChecker] Got exception when trying to request accessToken: {ex.Message} with {ex.InnerException}");
        }

        _logger.LogDebug("[UserStatusChecker] ms-graph authToken has been set");
    }

    public async Task<(bool, string)> MakeUserRequest(string userId)
    {
        /*
            if the authToken attribute is set, attempts an ms-graph/AzureAD
            request to determine a member's status from the org's PoV
            [!] returns True if a user is DEactivated or not found in AD

            string part of return value is left in for debugging if/when needed
        */
        if (_authToken == null)
        {
            _logger.LogError("[UserStatusChecker] cannot make user request, `authToken` is not set");
            _logger.LogDebug("[UserStatusChecker] re-attempting to set `authToken`");
            SetAuthToken();
            if (_authToken == null){ //TODO: throw the right exceptions so this can be a try/catch
                throw new Exception("No token set");
            }

        }

        string url = $"https://graph.microsoft.com/v1.0/users/{userId}?%24select=displayName,accountEnabled,id,identities,mail";

        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string result = await response.Content.ReadAsStringAsync();
                User? user = JsonSerializer.Deserialize<User>(result);
                if (user != null)
                {
                    if (!user.AccountEnabled)
                    {
                        return (true, "deactivated");
                    }
                }
                else
                {
                    _logger.LogError($"failed to deserialize response for user with id {userId}");
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (true, "404");
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Bad users (ms-graph) authorization token, exiting");
                throw new Exception("Bad token");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return (false, $"{userId}");
    }
}

