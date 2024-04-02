namespace Com.Tradecloud1.BCconnector.Mapping;

using Com.Tradecloud1.BCconnector.BC.Model;
using Com.Tradecloud1.BCconnector.TC.Model;

static public class TCtoBC
{
    static public PurchaseOrderResponseOData Map(SingleDeliveryOrderEvent soe)
    {
        var response = new PurchaseOrderResponseOData(
            document_No: soe.BuyerOrder.PurchaseOrderNumber,
            updatedLines: new List<UpdatedLineOData>(),
            newLines: new List<NewLineOData>()
        );

        foreach (var sol in soe.Lines)
        {
            if (sol.BuyerLine.Position == null)
            {
                response.NewLines.Add(NewLineMap(soe, sol));
            }
            else
            {
                response.UpdatedLines.Add(UpdatedLineMap(sol));
            }
        }

        return response;
    }

    static private UpdatedLineOData UpdatedLineMap(SingleDeliveryOrderLine sol)
    {
        return new UpdatedLineOData
        {
            Line_No = sol.BuyerLine.Position,
            Quantity = sol.StatusLine.ScheduledDelivery.Quantity,
            Direct_Unit_Cost = sol.StatusLine.Prices.GrossPrice.PriceInTransactionCurrency.Value,
            Line_Discount_Percent = sol.StatusLine.Prices.DiscountPercentage.Value,
            Promised_Receipt_Date = sol.StatusLine.ScheduledDelivery.Date.Value
        };
    }

    static private NewLineOData NewLineMap(SingleDeliveryOrderEvent soe, SingleDeliveryOrderLine sol)
    {
        return new NewLineOData
        {
            Document_Type = "Order",
            Document_No = soe.BuyerOrder.PurchaseOrderNumber,
            Type = "Item",
            No = sol.BuyerLine.Item.Number,
            Quantity = sol.StatusLine.ScheduledDelivery.Quantity,
            Direct_Unit_Cost = sol.StatusLine.Prices.GrossPrice.PriceInTransactionCurrency.Value,
            Line_Discount_Percent = sol.StatusLine.Prices.DiscountPercentage.Value,
            Promised_Receipt_Date = sol.StatusLine.ScheduledDelivery.Date.Value
        };
    }
}
