namespace Com.Tradecloud1.BCconnector.BC.Model;

using Newtonsoft.Json;

// PurchaseOrderResponse is a container for Purchase order response lines
// It cannot be updated directly in BC; each line has to be patched seperately.
// TODO patch status to "Released"???
public class PurchaseOrderResponseOData
{
    public PurchaseOrderResponseOData(string document_No, List<UpdatedLineOData> updatedLines,  List<NewLineOData> newLines)
    {
        Document_No = document_No;
        UpdatedLines = updatedLines;
        NewLines = newLines;
    }

    // purchase order number, part of composed functional key, needed to patch the line
    // eg. purchaseOrdersPurchLines(Document_Type='Order',Document_No='106001',Line_No=10000)
    public string Document_No { get; set; }

    // The updated lines to be patched
    public List<UpdatedLineOData> UpdatedLines { get; set; }

    // The new lines to be posted
    public List<NewLineOData> NewLines { get; set; }
}

// PurchaseOrderResponseLineOData contains a BC purchase order line in OData format.
// It contains a miminal set of fields required for an order response line.
// BC OData is used as the BC API 2.0 does not provide all required fields.
public class UpdatedLineOData
{
    // line number, part of composed functional key, needed to patch the line
    public string Line_No { get; set; }

    // confirmed delivery quantity (there is only one field in BC)
    public decimal Quantity { get; set; }

    // confirmed gross price per unit
    public decimal Direct_Unit_Cost { get; set; }

    // confirmed discount percentage
    public decimal Line_Discount_Percent { get; set; }

    // Confirmed delivery date
    public DateTime Promised_Receipt_Date { get; set; }
}

public class NewLineOData
{
    // part of the composed functional key, needed to post the new line
    public string Document_Type { get; set; }

    // purchase order number, part of composed functional key, needed to post the new line
    public string Document_No { get; set; }

    // line number, part of composed functional key, needed to post the new line
    public string Line_No { get; set; }

    // line type ("Item"), mandatory
    public string Type { get; set; }

    // item number, mandatory
    public string No { get; set; }

    // item reference number (always equal to `No`?), mandatory
    //public string Item_Reference_No { get; set; }

    // confirmed delivery quantity (there is only one field in BC)
    public decimal Quantity { get; set; }

    // confirmed gross price per unit
    public decimal Direct_Unit_Cost { get; set; }

    // confirmed discount percentage
    public decimal Line_Discount_Percent { get; set; }

    // confirmed delivery date
    public DateTime Promised_Receipt_Date { get; set; }

    // TODO needed? production order number
    // public string Prod_Order_No { get; set; }
}
