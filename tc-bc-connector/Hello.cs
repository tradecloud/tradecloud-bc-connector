namespace Com.Tradecloud1.BCconnector;

using Microsoft.AspNetCore.Mvc;

[Route("hello")]
public class HelloController : ControllerBase
{
    private readonly ILogger<HelloController> logger;
    private readonly IConfiguration config;

    public HelloController(ILogger<HelloController> logger, IConfiguration config)
    {
        this.logger = logger;
        this.config = config;
    }

    [HttpGet]
    public string Get()
    {
        var baseUrl = config["Connector:BaseURL"];
        logger.LogInformation("Hello from {baseUrl}", baseUrl);
        return "Tradecloud One Business Central Connector";
    }
}
