namespace Com.Tradecloud1.BCconnector.TC.Webhook;

using Com.Tradecloud1.BCconnector.Mapping;
using Com.Tradecloud1.BCconnector.BC.Client;
using Com.Tradecloud1.BCconnector.TC.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[Route("tc/single-delivery-order-event")]
public class OrderReponseController : ControllerBase
{
    private readonly IConfiguration config;
    private readonly ILogger<OrderReponseController> logger;
    private readonly PurchaseOrderResponseClient porc;

    public OrderReponseController(IConfiguration config, ILogger<OrderReponseController> logger, PurchaseOrderResponseClient porc)
    {
        this.config = config;
        this.logger = logger;
        this.porc = porc;
    }

    [HttpGet]
    public string Get()
    {
        return "Tradecloud single delivery order event webhook";
    }

    [HttpPost]
    public async Task<IActionResult> Webhook()
    {
        var request = HttpContext.Request;

        // Extract the bearer token from the Authorization header
        string authorizationHeader = request.Headers["Authorization"];
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            logger.LogWarning("OrderReponseController.Webhook: TC authorization header missing.");
            return Unauthorized();
        }

        // Verify the format of the bearer token
        if (!authorizationHeader.StartsWith("Bearer "))
        {
            logger.LogWarning("OrderReponseController.Webhook: TC authorization bearer token missing.");
            return Unauthorized();
        }

        // Extract the actual token value
        string bearerToken = authorizationHeader.Substring("Bearer ".Length).Trim();

        // TODO Apply SecureStringComparer class for constant-time string comparison to prevent timing attacks
        if (bearerToken != config["Connector:TC:WebhookBearerToken"])
        {
            logger.LogWarning("OrderReponseController.Webhook: TC authorization bearer token invalid.");
            return Unauthorized();
        }

        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var webhookBody = JsonConvert.DeserializeObject<WebhookBody>(body);

        logger.LogDebug("OrderReponseController.Webhook received body: {webhookBody}", JsonConvert.SerializeObject(webhookBody, Formatting.Indented));

        // Map and patch order response
        var response = TCtoBC.Map(webhookBody.SingleDeliveryOrderEvent);
        if (!await porc.SendOrderResponse(response))
        {
            return StatusCode(500);
        }

        logger.LogInformation("OrderReponseController.Webhook: processed TC single delivery order event succesfully, PurchaseOrderNumber: {PurchaseOrderNumber}", webhookBody.SingleDeliveryOrderEvent.BuyerOrder.PurchaseOrderNumber);
        return Ok();
    }
}
