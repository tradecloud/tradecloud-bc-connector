namespace Com.Tradecloud1.BCconnector.TC.Client;

using Com.Tradecloud1.BCconnector.TC.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System.Text;

public class SingleDeliveryOrderClient
{
    private readonly IConfiguration config;
    private readonly ILogger logger;
    private readonly AuthClient client;

    public SingleDeliveryOrderClient(IConfiguration config, ILogger<SingleDeliveryOrderClient> logger, AuthClient client)
    {
        this.config = config;
        this.logger = logger;
        this.client = client;
    }

    public async Task<bool> Post(SingleDeliveryOrder order)
    {
        var url = config["Connector:TC:BaseURL"] + "/api-connector/order/single-delivery";

        var requestJson = JsonConvert.SerializeObject(order, serializerSettings);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var response = await client.Post(url, requestContent);
        if (response != null)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("PostSingleDeliveryOrder failed, StatusCode: {StatusCode}, responseContent: {responseContent}, url: {url}, requestJson: {requestJson}",
                    response.StatusCode, responseContent, url, requestJson);
                return false;
            }

            logger.LogInformation("PostSingleDeliveryOrder: posted singleDelivery order successfully, url: {url}, PurchaseOrderNumber: {PurchaseOrderNumber}", url, order.Order.PurchaseOrderNumber);
            return true;
        }

        return false;
    }


    private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = new JsonConverter[] { new CustomDateTimeConverter() }
    };

    private class CustomDateTimeConverter : IsoDateTimeConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DateTime dateTime = (DateTime)value;
            // Check if the time is exactly midnight, and format the date without time if so
            string format = dateTime.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss";
            writer.WriteValue(dateTime.ToString(format));
        }
    }
}

