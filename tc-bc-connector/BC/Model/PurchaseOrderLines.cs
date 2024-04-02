namespace Com.Tradecloud1.BCconnector.BC.Model;

using Newtonsoft.Json;

// PurchaseOrderLinesOData contains BC purchase order lines in OData format
public class PurchaseOrderLinesOData
{
    [JsonProperty("@odata.context")]
    public string OdataContext { get; set; }
    public List<PurchaseOrderLineOData> Value { get; set; }
}

// PurchaseOrderLineOData contains a BC purchase order line in OData format
// It contains a miminal set of fields required for a Tradecloud order line.
// BC OData is used as the BC API 2.0 does not provide all required fields.
public class PurchaseOrderLineOData
{
    [JsonProperty("@odata.etag")]
    public string OdataEtag { get; set; }
    // part of the composed functional key, needed to patch the line
    public string Document_Type { get; set; }
    // part of the composed functional key, needed to patch the line
    public string Document_No { get; set; }
    public string Line_No { get; set; }
    // Only accept `Item`?
    // may be relevant later, for a `NoDeliveryRequired` indicator or charge lines
    // One of: Comment, G/L Account, Item, Resource, Fixed Asset, Charge (Item), Allocation Account
    // public string Type { get; set; } 
    // item number
    public string No { get; set; }
    // item name
    public string Description { get; set; }
    // item description
    public string Description_2 { get; set; }
    // requested & promised quantity (there is only one field in BC)
    public decimal Quantity { get; set; }
    // purchase unit
    public string Unit_of_Measure_Code { get; set; }
    // gross price per unit (so we are using PUQ 1.0)
    public decimal Direct_Unit_Cost { get; set; }

    // discount percentage
    public decimal Line_Discount_Percent { get; set; }

    public DateTime Requested_Receipt_Date { get; set; }

    // Confirmed delivery date
    public DateTime Promised_Receipt_Date { get; set; }
    
    // Production order number
    public string Prod_Order_No { get; set; }
    // not relevant?
    // public bool Finished { get; set; }
}
