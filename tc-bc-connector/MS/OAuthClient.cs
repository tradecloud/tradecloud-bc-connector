namespace Com.Tradecloud1.BCconnector.MS;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
public class OAuthClient
{
    private readonly IConfiguration config;
    private readonly ILogger logger;
    HttpClient httpClient;
    private Token? token;

    /// <summary>
    /// Initializes a Microsft OAuth 2.0 client 
    /// The OauthClient will apply a Service-to-Service Authentication flow
    /// </summary>
    public OAuthClient(IConfiguration config, ILogger<OAuthClient> logger)
    {
        httpClient = new HttpClient();
        this.config = config;
        this.logger = logger;
    }

    /// <summary>
    /// Send a HTTP GET request to the Business Central API v2.0 or ODATA API.
    /// The OauthClient will apply a valid access token.
    /// </summary>
    /// <param name="requestUri">The request URI</param>
    /// <returns>The HTTP response</returns>
    public async Task<HttpResponseMessage> Get(string? requestUri)
    {
        //logger.LogDebug("Get requestUri: {requestUri}", requestUri);

        await EnsureAccessToken();

        Func<Task<HttpResponseMessage>> getAsync = async () => await httpClient.GetAsync(requestUri);
        return await Request(getAsync);
    }

    /// <summary>
    /// Send a HTTP PATCH request to the Business Central API v2.0 or ODATA API.
    /// The OauthClient will apply a valid access token.
    /// </summary>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The PATCH body content</param>
    /// <param name="etag">The optional `If-Match` etag value, used for optimistic locking.</param>
    /// <returns>The HTTP response</returns>
    public async Task<HttpResponseMessage> Patch(string? requestUri, HttpContent? content, string? etag)
    {
        //logger.LogDebug("Patch requestUri: {requestUri}, etag: {etag}", requestUri, etag);

        await EnsureAccessToken();

        if (etag != null)
        {
            httpClient.DefaultRequestHeaders.Add("If-Match", etag);
        }
        Func<Task<HttpResponseMessage>> patchAsync = async () => await httpClient.PatchAsync(requestUri, content);
        var result = await Request(patchAsync);
        if (etag != null)
        {
            httpClient.DefaultRequestHeaders.Remove("If-Match");
        }

        return result;
    }

    /// <summary>
    /// Send a HTTP POST request to the Business Central API v2.0 or ODATA API.
    /// The OauthClient will apply a valid access token.
    /// </summary>
    /// <param name="requestUri">The request URI</param>
    /// <param name="content">The POST body content</param>
    /// <param name="etag">The optional `If-Match` etag value, used for optimistic locking.</param>
    /// <returns>The HTTP response</returns>
    public async Task<HttpResponseMessage> Post(string? requestUri, HttpContent? content, string? etag)
    {
        //logger.LogDebug("Post requestUri: {requestUri}, etag: {etag}", requestUri, etag);

        await EnsureAccessToken();

        if (etag != null)
        {
            httpClient.DefaultRequestHeaders.Add("If-Match", etag);
        }
        Func<Task<HttpResponseMessage>> postAsync = async () => await httpClient.PostAsync(requestUri, content);
        var result = await Request(postAsync);
        if (etag != null)
        {
            httpClient.DefaultRequestHeaders.Remove("If-Match");
        }

        return result;
    }

    /// <summary>
    /// Send a HTTP DELETE request to Business Central API v2.0 or ODATA API.
    /// The OauthClient will apply a valid access token.
    /// </summary>
    /// <param name="requestUri">The request URI</param>
    /// <param name="etag">The optional `If-Match` etag value, used for optimistic locking.</param>
    /// <returns>The HTTP response</returns>
    public async Task<HttpResponseMessage> Delete(string? requestUri, string? etag)
    {
        //logger.LogDebug("Delete requestUri: {requestUri}, etag: {etag}", requestUri, etag);

        await EnsureAccessToken();

        if (etag != null)
        {
            httpClient.DefaultRequestHeaders.Add("If-Match", etag);
        }
        Func<Task<HttpResponseMessage>> deleteAsync = async () => await httpClient.DeleteAsync(requestUri);
        var result = await Request(deleteAsync);
        if (etag != null)
        {
            httpClient.DefaultRequestHeaders.Remove("If-Match");
        }

        return result;
    }

    /// <summary>
    /// Ensure there is a valid access token, refeshing the token when expired.
    /// </summary>
    private async Task EnsureAccessToken()
    {
        if (token == null || DateTime.UtcNow >= token.expiryTime)
        {
            logger.LogDebug("EnsureAccessToken token null or expired, refreshing token");
            await RefreshAccessToken();
        }
    }

    /// <summary>
    /// Executes the HTTP request.
    /// In case of an Unauthorized status code, refreshes the access token, and tries once again.
    /// </summary>
    /// <param name="action">Request function to execute</param>
    /// <returns>The HTTP response</returns>
    private async Task<HttpResponseMessage> Request(Func<Task<HttpResponseMessage>> action)
    {
        var response = await action();

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Request unauthorized, refreshing token");
            await RefreshAccessToken();

            response = await action(); // Retry the action after refreshing the token
        }

        return response;
    }

    private async Task RefreshAccessToken()
    {
        var baseUrl = config["Connector:MS:BaseURL"];
        var tenantId = config["Connector:BC:TenantId"];
        var tokenUrl = baseUrl + "/" + tenantId + "/oauth2/v2.0/token";

        // TODO verify ClientId and ClientSecret are defined
        var clientId = config["Connector:MS:ClientId"];
        var clientSecret = config["Connector:MS:ClientSecret"];
        var scope = config["Connector:MS:Scope"];

        var form = new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"},
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"scope", scope},
            };

        var requestContent = new FormUrlEncodedContent(form);
        var response = await httpClient.PostAsync(tokenUrl, requestContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug("RefreshAccessToken successful, tokenUrl: {tokenUrl}", tokenUrl);
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
            token = new Token
            {
                accessToken = tokenResponse.access_token,
                expiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 300) // Refresh 5 minutes before expiry
            };

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization =
              new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.accessToken);
        }
        else
        {
            logger.LogError("RefreshAccessToken failed, tokenUrl: {tokenUrl}, content: {responseContent}", tokenUrl, responseContent);
        }
    }

    private class TokenResponse
    {
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public int ext_expires_in { get; set; }
        public string access_token { get; set; }
    }

    private class Token
    {
        public string accessToken { get; set; }
        public DateTime expiryTime { get; set; }
    }
}


