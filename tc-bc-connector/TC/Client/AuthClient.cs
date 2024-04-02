namespace Com.Tradecloud1.BCconnector.TC.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class AuthClient
{
    private readonly IConfiguration config;
    private readonly ILogger logger;
    HttpClient httpClient;
    private Token? token;

    /// <summary>
    /// Initializes a Tradecloud One API v2 authentication client
    /// </summary>
    public AuthClient(IConfiguration config, ILogger<AuthClient> logger)
    {
        httpClient = new HttpClient();
        this.config = config;
        this.logger = logger;
    }

    /// <summary>
    /// Send a HTTP POST request to the Tradecloud One API v2
    /// The AuthClient will apply a valid access token.
    /// </summary>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The POST body content</param>
    /// <returns>The HTTP response</returns>
    public async Task<HttpResponseMessage?> Post(string? requestUri, HttpContent? content)
    {
        //logger.LogDebug("Post requestUri: {requestUri}", requestUri);
        if (await EnsureAccessToken()) 
        {
            var response = await httpClient.PostAsync(requestUri, content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                logger.LogWarning("Post: unauthorized, logging out and in again");
                await Logout();
                await Login();

                response = await httpClient.PostAsync(requestUri, content); // Retry the action after re-login
            }

            return response;
        }
        
        return null;
    } 

    /// <summary>
    /// Ensure there is a valid access token, either by logging in or refreshing the token when expired.
    /// </summary>
    private async Task<bool> EnsureAccessToken()
    {
        if (token == null)
        {
            logger.LogDebug("EnsureAccessToken: token is null, logging in");
            return await Login();
        }
        else if (DateTime.UtcNow >= token.expiryTime)
        {
            logger.LogDebug("EnsureAccessToken: token is expired, refreshing token");
            return await Refresh();
        }

        return true;
    }

    /// <summary>
    /// Log in the `AuthClient`. The access token will be refreshed after 9 minutes.
    /// </summary>
    private async Task<bool> Login()
    {
        var url = config["Connector:TC:BaseURL"] + "/authentication/login";
        var username = config["Connector:TC:IntegrationUsername"];
        var password = config["Connector:TC:IntegrationPassword"];

        logger.LogDebug("Login url: {url}, username: {username}", url, username);

        var base64EncodedUsernamePassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedUsernamePassword);

        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Login failed, StatusCode: {StatusCode}, url: {url}, username: {username}", response.StatusCode, url, username);
            return false;
        }

        token = new Token
        {
            accessToken = GetHeaderValue("Set-Authorization", response),
            refreshToken = GetHeaderValue("Set-Refresh-Token", response),
            expiryTime = DateTime.UtcNow.AddMinutes(9) // Refresh in 9 minutes
        };
        SetAccessToken();
        return true;
    }

    /// <summary>
    /// Refreshes the access token. The access token will be refreshed after 9 minutes.
    /// </summary>
    private async Task<bool> Refresh()
    {
        var url = config["Connector:TC:BaseURL"] + "/authentication/refresh";
        logger.LogDebug("Refresh url: {url}", url);

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Refresh-Token", token.refreshToken);

        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Refresh failed, StatusCode: {StatusCode}, url: {url}", response.StatusCode, url);
            return false;
        }

        token = new Token
        {
            accessToken = GetHeaderValue("Set-Authorization", response),
            refreshToken = GetHeaderValue("Set-Refresh-Token", response),
            expiryTime = DateTime.UtcNow.AddMinutes(9) // Refresh in 9 minutes
        };
        SetAccessToken();
        return true;
    }

    /// <summary>
    /// Logout the `AuthClient`. 
    /// The access token will still be valid for max. 10 minutes. The refresh token becomes invalid.
    /// </summary>
    private async Task<bool> Logout()
    {
        var url = config["Connector:TC:BaseURL"] + "/authentication/logout";
        logger.LogDebug("Logout url: {url}", url);

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Refresh-Token", token.refreshToken);

        var response = await httpClient.PostAsync(url, null);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Logout failed, StatusCode: {StatusCode}, url: {url}", response.StatusCode, url);
            return false;
        }

        httpClient.DefaultRequestHeaders.Clear();
        token = null;
        return true;
    }

    private void SetAccessToken()
    {
        if (token != null)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization =
              new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.accessToken);
        }
    }

    // Work around non-existing headers without Exception
    private string? GetHeaderValue(string headerName, HttpResponseMessage message)
    {
        IEnumerable<string>? values;
        string? value = string.Empty;
        if (message.Headers.TryGetValues(headerName, out values))
        {
            value = values.FirstOrDefault();
        }
        return value;
    }

    private class Token
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public DateTime? expiryTime { get; set; }
    }
}
