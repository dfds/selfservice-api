using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class UserStatusCheck
{
    private string authToken;
    public void getAuthToken(){
    /*
        makes an MS-Graph request to get the temporary creds
        to be able to view users
    */
         // Get the values from environment variables
        string tenant_id = Environment.GetEnvironmentVariable("SS_MSGRAPH_TENANT_ID");
        string client_id = Environment.GetEnvironmentVariable("SS_MSGRAPH_CLIENT_ID");
        string client_secret = Environment.GetEnvironmentVariable("SS_MSGRAPH_CLIENT_SECRET");

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

        Console.WriteLine("URL: " + request.RequestUri);


        // Send the request and get the response
        try
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string responseText = reader.ReadToEnd();

            Console.WriteLine(responseText);
            JsonDocument jsonDocument = JsonDocument.Parse(responseText);
            if (jsonDocument.RootElement.TryGetProperty("access_token", out JsonElement tokenElement))
            {
                string tokenValue = tokenElement.GetString();
                Console.WriteLine($"Value of 'access_token': {tokenValue}");
                authToken = tokenValue;
            }
            else
            {
                Console.WriteLine("Attribute 'access_token' not found in the JSON, or malformed json given in");
            }
        }
        catch (WebException ex)
        {
            // Handle any exceptions or error responses here
            Console.WriteLine(ex.Message);
        }

        return;
    }
}

