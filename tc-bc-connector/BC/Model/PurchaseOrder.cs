namespace Com.Tradecloud1.BCconnector.BC.Model;

using Newtonsoft.Json;

// PurchaseOrderMetadata contains a BC purchase order header in BC API v2.0 format.
// The BC webhook will provide an BC API v2.0 resource including a purchase number `Id`.
// The fetched BC API v2.0 resource contains a purchase order `Number`.
// This number will used to fetch the BC OData using a composed functional key.
public class PurchaseOrderMetadata
{
    [JsonProperty("@odata.context")]
    public string OdataContext { get; set; }
    [JsonProperty("@odata.etag")]
    public string OdataEtag { get; set; }
    public string Id { get; set; }
    public string Number { get; set; }

    // API v2.0: one of "Draft", "In Review", "Open"
    // Odata:    one of "Open", "In Review", "Released"
    public string Status { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }
}

// PurchaseOrderOData contains a BC purchase order header in OData format.
// It contains a miminal set of fields required for a Tradecloud order.
// BC OData is used as the BC API 2.0 does not provide all required fields.
public class PurchaseOrderOData
{
    [JsonProperty("@odata.context")]
    public string OdataContext { get; set; }
    [JsonProperty("@odata.etag")]
    public string OdataEtag { get; set; }

    // The composed functional key is Document_Type ("order") with No (purchase order number)
    public string Document_Type { get; set; }
    public string No { get; set; }
    public DateTime Document_Date { get; set; }
    public DateTime Posting_Date { get; set; }
    public string Buy_from_Vendor_No { get; set; }
    public string BuyFromContactEmail { get; set; }
    public string Your_Reference { get; set; }
    public string Purchaser_Code { get; set; }
    public DateTime Order_Date { get; set; }
    public string Status { get; set; } // one of "Open", "In Review"(?), "Released"
    public string Payment_Terms_Code { get; set; }
    public string Shipment_Method_Code { get; set; }
    public string Ship_to_Code { get; set; }
    public string Location_Code { get; set; }
    public string Ship_to_Name { get; set; }
    public string Ship_to_Address { get; set; }
    public string Ship_to_Address_2 { get; set; }
    public string Ship_to_City { get; set; }
    public string Ship_to_Post_Code { get; set; }
    public string Ship_to_Country_Region_Code { get; set; }

    // TODO currency code
}
