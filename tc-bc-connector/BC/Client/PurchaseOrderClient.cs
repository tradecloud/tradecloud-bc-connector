namespace Com.Tradecloud1.BCconnector.BC.Client;

using Com.Tradecloud1.BCconnector.Mapping;
using Com.Tradecloud1.BCconnector.MS;
using Com.Tradecloud1.BCconnector.BC.Model;
using Com.Tradecloud1.BCconnector.TC.Client;
using Newtonsoft.Json;

public class PurchaseOrderClient
{
    private readonly Config config;
    private readonly ILogger logger;
    private readonly OAuthClient client;
    private readonly SingleDeliveryOrderClient sdoc;

    public PurchaseOrderClient(Config config, ILogger<PurchaseOrderClient> logger, OAuthClient client, SingleDeliveryOrderClient sdoc)
    {
        this.config = config;
        this.logger = logger;
        this.client = client;
        this.sdoc = sdoc;
    }

    public async Task<bool> SendOrder(string resource)
    {
        // Retrieve purchase order meta data to know the purchase order number
        var metaUrl = config.ResourceURL(resource);
        var orderMeta = await GetOrderMetadata(metaUrl);
        if (orderMeta == null)
        {
            return false;
        }
        logger.LogDebug("SendOrder: Got purchase order meta succesfully, orderMeta: {orderMeta}", JsonConvert.SerializeObject(orderMeta, Formatting.Indented));

        // Stop when status is "Draft" (which is equal to BC's OData and UI "Open") or "In Review"
        // This will also ignore echoes while patching the order/line by the connector, before releasing the order.
        if (orderMeta.Status == "Draft" || orderMeta.Status == "In Review")
        {
            return true;
        }

        // Retrieve purchase order OData using the the purchse order number
        var orderUrl = config.ODataPurchaseOrderURL(orderMeta.Number);
        var orderOData = await GetOrderOData(orderUrl);
        if (orderOData == null)
        {
            return false;
        }
        logger.LogDebug("SendOrder: Got purchase order fetched succesfully, orderOData: {orderOData}", JsonConvert.SerializeObject(orderOData, Formatting.Indented));

        var linesUrl = config.ODataPurchaseOrderLinesURL(orderMeta.Number);
        var linesOData = await GetOrderLinesOData(linesUrl);
        if (linesOData == null)
        {
            return false;
        }
        logger.LogDebug("SendOrder: Got purchase order line succesfully, linesOData: {linesOData}", JsonConvert.SerializeObject(linesOData, Formatting.Indented));

        var singleDeliveryOrder = BCtoTC.Map(orderOData, linesOData, logger);
        if (singleDeliveryOrder == null)
        {
            // Return succesful to prevent BC webhook retries.
            return true;
        }

        return await sdoc.Post(singleDeliveryOrder);
    }

    private async Task<PurchaseOrderMetadata> GetOrderMetadata(string url)
    {
        var response = await client.Get(url);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Get order metadata failed: StatusCode: {statusCode}, responseContent: {responseContent},url: {url}",
                response.StatusCode, responseContent, url);
            return null;
        }

        return JsonConvert.DeserializeObject<PurchaseOrderMetadata>(responseContent);
    }

    private async Task<PurchaseOrderOData> GetOrderOData(string url)
    {
        logger.LogDebug("GetOrderOData OData, url: {url}", url);

        var response = await client.Get(url);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("GetOrderOData failed: StatusCode: {StatusCode}, responseContent: {responseContent}, url: {url}", response.StatusCode, responseContent, url);
            return null;
        }

        return JsonConvert.DeserializeObject<PurchaseOrderOData>(responseContent);
    }

    private async Task<PurchaseOrderLinesOData> GetOrderLinesOData(string url)
    {
        logger.LogDebug("GetOrderLinesOData odata, url: {url}", url);

        var response = await client.Get(url);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("GetOrderLinesOData failed: StatusCode: {StatusCode}, responseContent: {responseContent}, url: {url}", response.StatusCode, responseContent, url);
            return null;
        }

        return JsonConvert.DeserializeObject<PurchaseOrderLinesOData>(responseContent);
    }
}
