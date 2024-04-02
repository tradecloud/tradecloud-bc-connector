namespace Com.Tradecloud1.BCconnector.BC.Client;

public class Config
{
    private readonly IConfiguration config;
    private readonly ILogger<HelloController> logger;

    public Config(IConfiguration config, ILogger<HelloController> logger)
    {
        this.config = config;
        this.logger = logger;
        Verify();
    }

    public string SubscriptionURL
    {
        get { return PrefixURL + "/api/v2.0/subscriptions"; }
    }

    public string SubscriptionResource
    {
        get { return $"api/v2.0/companies({CompanyId})/purchaseOrders"; }
    }

    public string ResourceURL(string resource)
    {
        return PrefixURL + "/" + resource;
    }

    public string NotificationURL
    {
        get { return config["Connector:BaseURL"] + "/bc/purchase-order"; }
    }

    public string TCPurchaseOrderURL(string No)
    {
        return PrefixURL + "/api/tradecloud/connector/v2.0/companies(" + CompanyId + ")/purchaseOrders('" + No + "')";
    }

    private string ODataBaseURL
    {
        get { return PrefixURL + "/ODataV4/company('" + CompanyName + "')"; }
    }

    public string ODataPurchaseOrderURL(string No)
    {
        return ODataBaseURL + "/purchaseOrders(Document_Type='Order',No='" + No + "')";
    }

    public string ODataPurchaseOrderLinesURL(string No)
    {
        return ODataBaseURL + "/purchaseOrders(Document_Type='Order',No='" + No + "')/purchaseOrdersPurchLines";
    }

    public string ODataPurchaseOrderLineURL(string Document_No, string Line_No)
    {
        return ODataBaseURL + "/purchaseOrdersPurchLines(Document_Type='Order',Document_No='" + Document_No + "',Line_No=" + Line_No + ")";
    }

    private string BaseURL
    {
        get { return config["Connector:BC:BaseURL"]; }
    }

    private string PrefixURL
    {
        get { return BaseURL + "/" + TenantId + "/" + Env; }
    }

    public string TenantId
    {
        get { return config["Connector:BC:TenantId"]; }
    }

    public string CompanyId
    {
        get { return config["Connector:BC:CompanyId"]; }
    }

    private string CompanyName
    {
        get { return config["Connector:BC:CompanyName"]; }
    }

    private string Env
    {
        get { return config["Connector:BC:Environment"]; }
    }

    public string SharedSecret
    {
        get { return config["Connector:BC:SharedSecret"]; }
    }

    private void Verify()    
    {
        if (string.IsNullOrEmpty(BaseURL))
        {
            logger.LogError("BC Config: BaseURL is empty");
        }
        else
        {
            logger.LogInformation("BC Config: BaseURL:     {BaseURL}", BaseURL);
        }

        if (string.IsNullOrEmpty(TenantId)) 
        {
            logger.LogError("BC Config: TenantId is empty");
        }
        else
        {
            logger.LogInformation("BC Config: TenantId:    {TenantId}", TenantId);
        }

        if (string.IsNullOrEmpty(Env))
        {
            logger.LogError("BC Config: Enviroment is empty");
        }
        else
        {
            logger.LogInformation("BC Config: Environment: {Environment}", Env);
        }

        if (String.IsNullOrEmpty(TenantId)) 
        {
            logger.LogError("BC Config: CompanyId is empty");
        }
        else
        {
            logger.LogInformation("BC Config: CompanyId:   {CompanyId}", CompanyId);
        }

        if (String.IsNullOrEmpty(TenantId)) 
        {
            logger.LogError("BC Config: TenantId is empty");
        }
        else
        {
            logger.LogInformation("BC Config: CompanyName: {CompanyName}", CompanyName);
        }
    }
}
