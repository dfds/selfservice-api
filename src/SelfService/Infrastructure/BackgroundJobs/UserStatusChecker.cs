using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.BackgroundJobs;

public class UserStatusChecker : IUserStatusChecker
{
    private readonly ILogger<RemoveDeactivatedMemberships> _logger;
    private string? _authToken;
    private readonly IConfiguration _configuration;
    private HttpClient _httpClient;
    private static string _baseUrl = "https://graph.microsoft.com/v1.0";
    private string? _domainSuffix;

    public UserStatusChecker(ILogger<RemoveDeactivatedMemberships> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
        _domainSuffix = _configuration["SS_MSGRAPH_SUFFIX"];
    }

    private async Task SetAuthToken()
    {
        /*
            makes an MS-Graph request to get the temporary creds
            to be able to view users
        */
        // Get the values from environment variables
        string? tenantId = _configuration["SS_MSGRAPH_TENANT_ID"];
        string? clientId = _configuration["SS_MSGRAPH_CLIENT_ID"];
        string? clientSecret = _configuration["SS_MSGRAPH_CLIENT_SECRET"];
        if (clientSecret == null)
        {
            _logger.LogError("[UserStatusChecker] `clientSecret` not found in environment");
            return;
        }

        if (clientId == null)
        {
            _logger.LogError("[UserStatusChecker] `clientId` not found in environment");
            return;
        }

        if (tenantId == null)
        {
            _logger.LogError("[UserStatusChecker] `tenantId` not found in environment");
            return;
        }

        //setup request
        string url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token ";
        string formData =
            $"client_id={clientId}&grant_type=client_credentials&scope=https://graph.microsoft.com/.default&client_secret={clientSecret}";

        HttpContent content = new StringContent(
            formData,
            System.Text.Encoding.UTF8,
            "application/x-www-form-urlencoded"
        );

        // Make request and handle response
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"[UserStatusChecker] Unable to request AccessToken, failed with {response.StatusCode}"
                );
                return;
            }

            string responseText = await response.Content.ReadAsStringAsync();
            AuthTokenResponse? authTokenResponse = JsonSerializer.Deserialize<AuthTokenResponse>(responseText);

            if (authTokenResponse == null)
            {
                _logger.LogError($"[UserStatusChecker] Unable to deserialize AuthTokenReponse. Got: {responseText}");
                return;
            }

            _authToken = authTokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"[UserStatusChecker] Got exception when trying to request accessToken: {ex.Message} with {ex.InnerException}"
            );
            return;
        }

        _logger.LogDebug("[UserStatusChecker] ms-graph authToken has been set");
    }

    /// <returns> true if AuthToken is set or was set sucessfully</returns>
    public async Task<bool> TrySetAuthToken()
    {
        if (_authToken == null)
        {
            await SetAuthToken();
        }
        return _authToken != null;
    }

    public bool IsUserExternal(string val)
    {
        if (_domainSuffix == null)
        {
            return false;
        }
        return !val.ToLower().EndsWith(_domainSuffix);
    }

    /// <summary>
    ///  Returns the status of a member, by querying ms-graph/AzureAD
    /// </summary>
    public async Task<UserStatusCheckerStatus> CheckUserStatus(UserId userId)
    {
        if (_authToken == null)
        {
            _logger.LogError(
                "[UserStatusChecker] cannot make user request, `authToken` is not set, attempting to set `authToken`"
            );
            if (!(await TrySetAuthToken()))
            {
                _logger.LogError("[UserStatusChecker] Failed setting authToken: cancelling CheckUserStatus");
                return UserStatusCheckerStatus.NoAuthToken;
            }
        }
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        try
        {
            HttpResponseMessage response;
            User? user;

            if (IsUserExternal(userId.ToString()))
            {
                (user, response) = await GetUserViaEmail(userId.ToString());
            }
            else
            {
                (user, response) = await GetUserViaUpn(userId.ToString());
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                {
                    if (user != null)
                    {
                        if (!user.AccountEnabled)
                        {
                            return UserStatusCheckerStatus.Deactivated;
                        }
                    }
                    else
                    {
                        return UserStatusCheckerStatus.NotFound;
                    }

                    break;
                }
                case HttpStatusCode.NotFound:
                    return UserStatusCheckerStatus.NotFound;
                case HttpStatusCode.Unauthorized:
                    _logger.LogError("Bad users (ms-graph) authorization token. Setting to null");
                    _authToken = null;
                    return UserStatusCheckerStatus.BadAuthToken;
                default:
                    _logger.LogError($"Unhandled HttpStatusCode: {response.StatusCode}");
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return UserStatusCheckerStatus.Unknown;
    }

    public async Task<(User?, HttpResponseMessage)> GetUserViaUpn(string upn)
    {
        string url = $"{_baseUrl}/users/{upn}";
        var queryParams = new Dictionary<string, string?>
        {
            { "$select", "displayName,accountEnabled,id,identities,mail,userPrincipalName" },
        };
        url = QueryHelpers.AddQueryString(url, queryParams);

        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string result = await response.Content.ReadAsStringAsync();
            User? user = JsonSerializer.Deserialize<User>(result);
            if (user == null)
            {
                throw new JsonException("Failed to parse user response");
            }
            return (user, response);
        }

        return (null, response);
    }

    public async Task<(User?, HttpResponseMessage)> GetUserViaEmail(string email)
    {
        string url = $"{_baseUrl}/users";
        var queryParams = new Dictionary<string, string?>
        {
            { "$select", "displayName,accountEnabled,id,identities,mail,userPrincipalName" },
            { "$filter", $"mail eq '{email.ToLower()}'" },
        };
        url = QueryHelpers.AddQueryString(url, queryParams);

        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string result = await response.Content.ReadAsStringAsync();
            UsersResponse? usersResponse = JsonSerializer.Deserialize<UsersResponse>(result);

            if (usersResponse != null && usersResponse.Value!.Count > 0)
            {
                return (usersResponse.Value[0], response);
            }
        }

        return (null, response);
    }
}
