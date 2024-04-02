namespace Com.Tradecloud1.BCconnector.BC.Client;

using Com.Tradecloud1.BCconnector.MS;
using Com.Tradecloud1.BCconnector.BC.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

public class PurchaseOrderResponseClient
{
    private readonly Config config;
    private readonly ILogger logger;
    private readonly OAuthClient client;

    public PurchaseOrderResponseClient(Config config, ILogger<PurchaseOrderResponseClient> logger, OAuthClient client)
    {
        this.config = config;
        this.logger = logger;
        this.client = client;
    }

    /// <summary>
    /// Send order response in BC:
    ///   1a. Get the current order status and etag
    ///   1b. Set the order status to `Open` when `Released`, using the order etag
    ///   2. Get the current lines with their positions and etags
    ///   3. Per updated line, patch the order response line, using the current line etag
    ///   4a. Per new line, get the current order with their etag
    ///   4b. Calculate the new line position
    ///   4c. Per new line, post the order response line, using the current order etag
    ///   5a. Get the current order status and etag
    ///   5b. Set the order status to `Released` when `Open` and orginally `Released`, using the order etag
    /// </summary>
    /// <param name="orderResponse">The order response to patch</param>
    /// <returns></returns>
    public async Task<bool> SendOrderResponse(PurchaseOrderResponseOData orderResponse)
    {
        logger.LogDebug("SendOrderResponse: {orderResponse}", JsonConvert.SerializeObject(orderResponse, Formatting.Indented));

        // 1. Set the order status to `Open` when `Released`, using the order etag
        var reopened = await ReopenOrder(orderResponse.Document_No);

        // 2. Get the current lines with their positions and etags
        var linesUrl = config.ODataPurchaseOrderLinesURL(orderResponse.Document_No);
        var currentLines = await GetOrderLinesOData(linesUrl);
        if (currentLines == null)
        {
            logger.LogError("SendOrderResponse: Could not find current order lines, Document_No: {Document_No}", orderResponse.Document_No);
            return false;
        }
        logger.LogDebug("SendOrderResponse: Got current purchase order lines succesfully, currentLines: {currentLines}", JsonConvert.SerializeObject(currentLines, Formatting.Indented));

        // 3. Per updated line, patch the order response line, using the current line etag
        foreach (var updatedLine in orderResponse.UpdatedLines)
        {
            var currentLine = currentLines.Value.FirstOrDefault(l => l.Line_No == updatedLine.Line_No);
            if (currentLine == null)
            {
                logger.LogWarning("SendOrderResponse: Could not find current order line, Line_No: {Line_No}", updatedLine.Line_No);
                continue;
            }

            var lineUrl = config.ODataPurchaseOrderLineURL(orderResponse.Document_No, updatedLine.Line_No);
            await PatchOrderLine(lineUrl, updatedLine, currentLine.OdataEtag);
        }

        var orderUrl = config.ODataPurchaseOrderURL(orderResponse.Document_No);
        var newLineUrl = config.ODataPurchaseOrderLinesURL(orderResponse.Document_No);
        foreach (var newLine in orderResponse.NewLines)
        {
            // 4a. Per new line, get the current order with their etag
            var currentOrder = await GetOrderOData(orderUrl);
            if (currentOrder == null)
            {
                logger.LogError("SendOrderResponse: Could not find current order, Document_No: {Document_No}", orderResponse.Document_No);
                return false;
            }
            logger.LogDebug("SendOrderResponse: Got current purchase order succesfully, currentOrder: {currentOrder}", JsonConvert.SerializeObject(currentOrder, Formatting.Indented));

            currentLines = await GetOrderLinesOData(linesUrl);
            if (currentLines == null)
            {
                logger.LogError("SendOrderResponse: Could not find current lines, Document_No: {Document_No}", orderResponse.Document_No);
                return false;
            }
            logger.LogDebug("SendOrderResponse: Got current purchase order lines succesfully, currentLines: {currentLines}", JsonConvert.SerializeObject(currentLines, Formatting.Indented));

            // 4b. calculate the new line position
            var highestLineNoForItemNo = currentLines.Value
                     .Where(line => line.No == newLine.No)
                     .OrderByDescending(line => line.Line_No)
                     .FirstOrDefault();
            if (highestLineNoForItemNo == null)
            {
                logger.LogWarning("SendOrderResponse: Could not find original line for new line, Document_No: {Document_No}, Item No: {No}", orderResponse.Document_No, newLine.No);
                continue;
            }

            var newLineNo = int.Parse(highestLineNoForItemNo.Line_No) + 10;
            newLine.Line_No = newLineNo.ToString();

            // 4c. Per new line, post the order response line, using the current order etag
            await PostNewOrderLine(newLineUrl, newLine, currentOrder.OdataEtag);
        }

        if (reopened)
        {
            // 5. Set the order status to `Released` when `Open` and orginally `Released`, using the order etag
            await ReleaseOrder(orderResponse.Document_No);
        }

        return true;
    }

    private async Task<bool> ReopenOrder(string Document_No)
    {
        // 1a. Get the current order status and etag
        var tcOrderUrl = config.TCPurchaseOrderURL(Document_No);
        var orderMeta = await GetTCOrderOData(tcOrderUrl);
        if (orderMeta == null)
        {
            // Reopen failed
            return false;
        }
        logger.LogDebug("Got TC meta data, Document_No: {Document_No}, Status: {Status}, OdataEtag: {OdataEtag}", Document_No, orderMeta.Status, orderMeta.OdataEtag);

        // API v2.0 Status "Open" == OData/UI Status "Released"
        if (orderMeta.Status == "Open")
        {
            // 1b. Set the order status to `Open` when `Released`, using the order etag
            var reopenUrl = tcOrderUrl + "/Microsoft.NAV.reopen";
            logger.LogDebug("ReopenOrder, reopenUrl: {reopenUrl}", reopenUrl);

            var response = await client.Post(reopenUrl, null, orderMeta.OdataEtag);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                // Reopen failed
                logger.LogError("ReopenOrder failed: StatusCode: {StatusCode}, responseContent: {responseContent}, reopenUrl: {reopenUrl}", response.StatusCode, responseContent, reopenUrl);
                return false;
            }

            // Reopen succesful
            logger.LogInformation("ReopenOrder: reopened order succesfully, reopenUrl: {reopenUrl}", reopenUrl);
            return true;
        }

        // Not reopened
        return false;
    }

    private async Task<bool> ReleaseOrder(string Document_No)
    {
        // 5a. Get the current order status and etag
        var tcOrderUrl = config.TCPurchaseOrderURL(Document_No);
        var orderMeta = await GetTCOrderOData(tcOrderUrl);
        if (orderMeta == null)
        {
            // Release failed
            return false;
        }
        logger.LogDebug("Got TC meta data, Document_No: {Document_No}, Status: {Status}, OdataEtag: {OdataEtag}", Document_No, orderMeta.Status, orderMeta.OdataEtag);

        // API v2.0 Status "Draft" == OData/UI Status "Open"
        if (orderMeta.Status == "Draft")
        {
            // 5b. Set the order status to `Released` when `Open` and orginally `Released`, using the order etag
            var releaseUrl = tcOrderUrl + "/Microsoft.NAV.release";
            logger.LogDebug("ReleaseOrder, releaseUrl: {releaseUrl}", releaseUrl);

            var response = await client.Post(releaseUrl, null, orderMeta.OdataEtag);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                // Release failed
                logger.LogError("ReleaseOrder failed: StatusCode: {StatusCode}, responseContent: {responseContent}, releaseUrl: {releaseUrl}", response.StatusCode, responseContent, releaseUrl);
                return false;
            }

            // Release succcesful
            logger.LogInformation("ReleaseOrder: released order succesfully, releaseUrl: {releaseUrl}", releaseUrl);
            return true;
        }

        // Not released
        return false;
    }

    private async Task<PurchaseOrderOData> GetTCOrderOData(string url)
    {
        logger.LogDebug("GetTCOrderOData, url: {url}", url);

        var response = await client.Get(url);
        var responseContent = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode == 404)
        {
            logger.LogWarning("GetTCOrderOData not found, is the Tradecloud connector app installed?: StatusCode: {StatusCode}, responseContent: {responseContent}, url: {url}", response.StatusCode, responseContent, url);
            return null;
        }
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("GetTCOrderOData failed: StatusCode: {StatusCode}, responseContent: {responseContent}, url: {url}", response.StatusCode, responseContent, url);
            return null;
        }

        return JsonConvert.DeserializeObject<PurchaseOrderOData>(responseContent);
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
        logger.LogDebug("GetOrderLinesOData, url: {url}", url);

        var response = await client.Get(url);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("GetOrderLinesODataa failed: StatusCode: {StatusCode}, responseContent: {responseContent}, url: {url}", response.StatusCode, responseContent, url);
            return null;
        }

        return JsonConvert.DeserializeObject<PurchaseOrderLinesOData>(responseContent);
    }

    private async Task<bool> PatchOrderLine(string url, UpdatedLineOData line, string etag)
    {
        var requestJson = JsonConvert.SerializeObject(line, serializerSettings);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        logger.LogDebug("PatchOrderLine, url: {url}, requestJson: {requestJson}", url, requestJson);

        var response = await client.Patch(url, requestContent, etag);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("PatchOrderLine failed: StatusCode: {StatusCode}, responseContent: {responseContent}, url: {url}, requestJson: {requestJson}", response.StatusCode, responseContent, url, requestJson);
            return false;
        }

        logger.LogInformation("PatchOrderLine: patched order line successfully, url: {url}", url);
        return true;
    }

    private async Task<bool> PostNewOrderLine(string url, NewLineOData line, string etag)
    {
        var requestJson = JsonConvert.SerializeObject(line, serializerSettings);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        logger.LogDebug("PostNewOrderLine, url: {url}, requestJson: {requestJson}", url, requestJson);

        var response = await client.Post(url, requestContent, etag);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("PostNewOrderLine failed: StatusCode: {StatusCode}, responseContent: {responseContent}, url: {url}, requestJson: {requestJson}", response.StatusCode, responseContent, url, requestJson);
            return false;
        }

        logger.LogInformation("PostNewOrderLine: posted new line successfully, url: {url}", url);
        return true;
    }
    private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        Converters = new JsonConverter[] { new CustomDateTimeConverter() }
    };

    private class CustomDateTimeConverter : IsoDateTimeConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DateTime dateTime = (DateTime)value;
            writer.WriteValue(dateTime.ToString("yyyy-MM-dd"));
        }
    }
}
