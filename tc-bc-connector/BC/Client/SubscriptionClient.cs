namespace Com.Tradecloud1.BCconnector.BC.Client;

using Com.Tradecloud1.BCconnector.MS;
using Com.Tradecloud1.BCconnector.BC.Model;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Text;


public class SubscriptionClient
{
    private readonly Config config;
    private readonly ILogger logger;
    private readonly OAuthClient client;
    private Subscription? subscription;

    /// <summary>
    /// Initializes a Business Centrals webhook subscription client 
    /// TODO renew subscription within 3 days
    /// </summary>
    public SubscriptionClient(Config config, ILogger<SubscriptionClient> logger, OAuthClient client)
    {
        this.config = config;
        this.logger = logger;
        this.client = client;
    }

    /// <summary>
    /// Ensures there is a BC purchase order subscription while starting app
    /// </summary>
    public async Task EnsureSubscription()
    {
        var url = config.SubscriptionURL;
        logger.LogDebug("EnsureSubscription, url: {url}", url);

        var subscriptions = await GetSubscriptions(url);
        if (subscriptions != null)
        {
            subscription = subscriptions.Value.FirstOrDefault(s => s.NotificationUrl.Equals(config.NotificationURL) && s.Resource.Equals(config.SubscriptionResource));
        }

        if (subscription == null)
        {
            await Subscribe(url, config.NotificationURL, config.SubscriptionResource);
        }
    }

    /// <summary>
    /// Gets current subscriptions
    /// </summary>
    private async Task<Subscriptions?> GetSubscriptions(string url)
    {
        logger.LogDebug("Getting BC subscriptions, url: {url}", url);

        var response = await client.Get(url);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Getting BC subscriptions failed, status: {statusCode}, response: {response}, url: {url}",
                response.StatusCode.ToString(), responseContent, url);
            return null;
        }

        var subscriptions = JsonConvert.DeserializeObject<Subscriptions>(responseContent);
        logger.LogInformation("Got BC subscriptions successfully, subscriptions: {subscriptions}", JsonConvert.SerializeObject(subscriptions, Formatting.Indented));

        return subscriptions;
    }

    /// <summary>
    /// Subscribes to purchase order notifications
    /// </summary>
    private async Task Subscribe(string url, string notificationUrl, string resource)
    {
        logger.LogDebug("Subscribing to BC, url: {url}, notificationUrl: {notificationUrl}, resource: {resource}", url, notificationUrl, resource);

        var body = new
        {
            notificationUrl,
            resource,
            clientState = config.SharedSecret
        };
        var jsonBody = JsonConvert.SerializeObject(body);
        var requestContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.Post(url, requestContent, null);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Subscribing BC failed, status: {statusCode}, response: {response}, url: {url}, resource: {resource}",
                response.StatusCode.ToString(), responseContent, url, body.resource);
            return;
        }

        subscription = JsonConvert.DeserializeObject<Subscription>(responseContent);
        logger.LogInformation("Subscribed to BC successfully, Subscription: {Subscription}", JsonConvert.SerializeObject(subscription, Formatting.Indented));
    }

    /// <summary>
    /// Unsubscribes from purchase order notifications
    /// </summary>
    public async Task Unsubscribe()
    {
        if (subscription != null)
        {
            var url = config.SubscriptionURL + $"('{subscription.SubscriptionId}')";
            logger.LogDebug("Unsubscribing from BC, url: {url}", url);

            var response = await client.Delete(url, subscription.ODataEtag);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Unsubscribing BC failed, status: {statusCode}, response: {response}, url: {url}",
                    response.StatusCode.ToString(), responseContent, url);
                return;
            }

            subscription = null;
            logger.LogInformation("Unsubscribed from BC successfully, url: {url}", url);
        }
    }
}
