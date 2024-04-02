namespace Com.Tradecloud1.BCconnector.BC.Webhook;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Com.Tradecloud1.BCconnector.BC.Client;

[Route("bc/purchase-order")]
public class PurchaseOrderController : ControllerBase
{
    private readonly IConfiguration config;
    private readonly ILogger<PurchaseOrderController> logger;
    private readonly PurchaseOrderClient poc;

    public PurchaseOrderController(IConfiguration config, ILogger<PurchaseOrderController> logger, PurchaseOrderClient poc)
    {
        this.config = config;
        this.logger = logger;
        this.poc = poc;
    }

    [HttpGet]
    public string Get()
    {
        return "Business Central Purchase Order Webhook";
    }

    [HttpPost]
    public async Task<IActionResult> Webhook([FromBody] JObject jsonData)
    {
        if (jsonData["clientState"] != null)
        {
            var initialNotification = jsonData.ToObject<ClientStateModel>();
            // Verify the deserialized model
            if (initialNotification == null)
            {
                logger.LogWarning("PurchaseOrderController.Webhook: ClientStateModel deserialization failed.");
                return BadRequest("Deserialization failed");
            }

            // Verify the echeod shared secret with the configured shared secret
            // TODO Apply SecureStringComparer class for constant-time string comparison to prevent timing attacks
            if (initialNotification.ClientState != config["Connector:BC:SharedSecret"])
            {
                logger.LogWarning("PurchaseOrderController.Webhook: initialNotification SharedSecret authorization failed.");
                return Unauthorized();
            }

            var validationToken = HttpContext.Request.Query["validationToken"].ToString();
            // Verify the validation token is not empty
            if (string.IsNullOrEmpty(validationToken))
            {
                logger.LogWarning("PurchaseOrderController.Webhook: initialNotification validationToken authorization failed.");
                return Unauthorized();
            }

            // Echo the validation token in plain text
            logger.LogInformation("PurchaseOrderController.Webhook: initialNotification validationToken authorization successful.");
            return Ok(validationToken);
        }
        else
        {
            var notifications = jsonData.ToObject<WebhookNotificationsModel>();
            if (notifications == null)
            {
                logger.LogWarning("PurchaseOrderController.Webhook: WebhookNotificationsModel deserialization failed.");
                return BadRequest("Deserialization failed");
            }

            logger.LogInformation("WebhookNotificationsModel: {notifications}", JsonConvert.SerializeObject(notifications, Formatting.Indented));

            foreach (var notification in notifications.Value)
            {
                // Verify the echeod shared secret with the configured shared secret
                // TODO Apply SecureStringComparer class for constant-time string comparison to prevent timing attacks
                if (notification.ClientState != config["Connector:BC:SharedSecret"])
                {
                    logger.LogWarning("PurchaseOrderController.Webhook: BC notification SharedSecret authorization failed.");
                    return Unauthorized();
                }

                if (notification.Resource == null)
                {
                    logger.LogWarning("PurchaseOrderController.Webhook: BC notification.Resource is empty.");
                    return BadRequest("Resource is empty");
                }

                if (!await poc.SendOrder(notification.Resource))
                {
                    return StatusCode(500);
                }

                logger.LogInformation("PurchaseOrderController.Webhook: processed BC notification succesfully, resource: {resource}", notification.Resource);
            }
            
            return Ok(); 
        }
    }
}

public class ClientStateModel
{
    public string? ClientState { get; set; }
}

public class WebhookNotificationsModel
{
    public List<WebhookNotification> Value { get; set; }
}
public class WebhookNotification
{
    public string? SubscriptionId { get; set; }
    public string? ClientState { get; set; }
    public DateTime? ExpirationDateTime { get; set; }
    public string? Resource { get; set; }
    public string? ChangeType { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }
}
