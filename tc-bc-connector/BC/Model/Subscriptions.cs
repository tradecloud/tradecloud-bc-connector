namespace Com.Tradecloud1.BCconnector.BC.Model;

using Newtonsoft.Json;

public class Subscriptions
{
    [JsonProperty("@odata.context")]
    public string? OdataContext { get; set; }
    public List<Subscription> Value { get; set; }
}

public class Subscription
{
    [JsonProperty("@odata.context")]
    public string? OdataContext { get; set; }

    [JsonProperty("@odata.etag")]
    public string ODataEtag { get; set; }
    public string SubscriptionId { get; set; }
    public string NotificationUrl { get; set; }
    public string Resource { get; set; }
    public long Timestamp { get; set; }
    public string UserId { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public string ClientState { get; set; }
    public DateTime ExpirationDateTime { get; set; }
    public DateTime SystemCreatedAt { get; set; }
    public string SystemCreatedBy { get; set; }
    public DateTime SystemModifiedAt { get; set; }
    public string SystemModifiedBy { get; set; }
}
