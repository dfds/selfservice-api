using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using SelfService.Infrastructure.Persistence;

using System.Collections.Generic;

namespace SelfService.Infrastructure.BackgroundJobs;
//TODO: make nullable where needed
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

public class UserStatusChecker
{
    private readonly ILogger<RemoveInactiveMemberships> _logger; //depends on that background job
    private readonly SelfServiceDbContext _context;
    private string? authToken;

    public UserStatusChecker(SelfServiceDbContext context, ILogger<RemoveInactiveMemberships> logger)
    {
        _context = context;
        _logger = logger;
        setAuthToken();
    }
    public void setAuthToken(){
    /*
        makes an MS-Graph request to get the temporary creds
        to be able to view users
    */
         // Get the values from environment variables
        string? tenant_id = Environment.GetEnvironmentVariable("SS_MSGRAPH_TENANT_ID");
        string? client_id = Environment.GetEnvironmentVariable("SS_MSGRAPH_CLIENT_ID");
        string? client_secret = Environment.GetEnvironmentVariable("SS_MSGRAPH_CLIENT_SECRET");

        //setup request
        string url = $"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token ";
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";

        //create the form data with the values from above
        string formData = $"client_id={client_id}&grant_type=client_credentials&scope=https://graph.microsoft.com/.default&client_secret={client_secret}";
        byte[] formDataBytes = System.Text.Encoding.UTF8.GetBytes(formData);

        // Write the form data to the request stream
        using (Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(formDataBytes, 0, formDataBytes.Length);
        }

         _logger.LogDebug("URL: " + request.RequestUri);


        // Send the request and get the response
        try
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string responseText = reader.ReadToEnd();

             _logger.LogDebug(responseText);
            JsonDocument jsonDocument = JsonDocument.Parse(responseText);
            if (jsonDocument.RootElement.TryGetProperty("access_token", out JsonElement tokenElement))
            {
                string? tokenValue = tokenElement.GetString();
                if (tokenValue == null){
                   _logger.LogDebug("got null ms-graph access token. check credentials or request");
                }
                authToken = tokenValue;
            }
            else
            {
                 _logger.LogDebug("Attribute 'access_token' not found in the JSON, or malformed json given in");
            }
        }
        catch (WebException ex)
        {
            // Handle any exceptions or error responses here
             _logger.LogDebug(ex.Message);
        }
        _logger.LogDebug("ms-graph authToken has been set");
        return;
    }

    public (bool, string) MakeUserRequest(string userId){
        string url = $"https://graph.microsoft.com/v1.0/users/{userId}?%24select=displayName,accountEnabled,id,identities,mail";

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Headers["Authorization"] = "Bearer " + authToken;
        request.Method = "GET";

        try
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    string result = sr.ReadToEnd();
                    Console.WriteLine(result);
                    var usrJson = JsonSerializer.Deserialize<User>(result);
                    Console.WriteLine(usrJson.DisplayName+"\n");
                    if (!usrJson.AccountEnabled)
                    {
                        return (true, "deactivated");
                    }
                }
            }
        }
        catch (WebException e)
        {
            if (e.Status == WebExceptionStatus.ProtocolError)
            {
                HttpWebResponse httpResponse = (HttpWebResponse)e.Response;
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return (true, "404");
                }
                else if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Bad users (ms-graph) authorization token, exiting");
                    throw new Exception("Bad token");
                }
            }
        }
        return (false, "");
    }
}

