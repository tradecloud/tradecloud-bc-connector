namespace Com.Tradecloud1.BCconnector.Mapping;

using System.Data;
using Com.Tradecloud1.BCconnector.BC.Model;
using Com.Tradecloud1.BCconnector.TC.Model;

static public class BCtoTC
{
    static public SingleDeliveryOrder? Map(PurchaseOrderOData po, PurchaseOrderLinesOData pols, ILogger logger)
    {
        var destinationCode = !string.IsNullOrEmpty(po.Ship_to_Code) ? po.Ship_to_Code : po.Location_Code;
        var lines = new List<Line>();
        foreach (var pol in pols.Value)
        {
            var line = Map(po, pol, logger);
            if (line != null)
            {
                lines.Add(line);
            }
        }

        if (lines.Count == 0)
        {
            logger.LogWarning("Map SingleDeliveryOrder: order does not have lines, skipping order: No: {No}", po.No);
            return null;
        }

        return new SingleDeliveryOrder
        {
            Order = new Order
            {
                SupplierAccountNumber = po.Buy_from_Vendor_No,
                PurchaseOrderNumber = po.No,
                Description = po.Your_Reference,
                Destination = new Destination
                {
                    Code = destinationCode,
                    Names = new List<string>
                    {
                        po.Ship_to_Name
                    },
                    AddressLines = new List<string>
                    {
                        po.Ship_to_Address,
                        po.Ship_to_Address_2, // TODO check null
                    },
                    PostalCode = po.Ship_to_Post_Code,
                    City = po.Ship_to_City,
                    CountryCodeIso2 = po.Ship_to_Country_Region_Code
                },
                Terms = new Terms
                {
                    IncotermsCode = po.Shipment_Method_Code,
                    // TODO fetch incoterm text -> code pages?
                    PaymentTermsCode = po.Payment_Terms_Code
                    // TODO fetch payment terms text -> code pages?
                },
                //TODO Indicators -> preferable on line level, status ???
                Contact = new Contact
                {
                    Email = "" // TODO fetch e-mail based on Purchaser code
                },
                SupplierContact = new Contact
                {
                    Email = po.BuyFromContactEmail
                },
                OrderType = "Purchase"
            },
            Lines = lines
        };
    }

    static public Line? Map(PurchaseOrderOData po, PurchaseOrderLineOData pol, ILogger logger)
    {
        DateTime? date;
        if (pol.Promised_Receipt_Date > DateTime.MinValue)
        {
            date = pol.Promised_Receipt_Date;
        }
        else if (pol.Requested_Receipt_Date > DateTime.MinValue)
        {
            date = pol.Requested_Receipt_Date;
        }
        else
        {
            logger.LogWarning("Map Line: both `Promised_Receipt_Date` and `Requested_Receipt_Date` are empty, skipping order line: No: {No}, Line_No {Line_No}", po.No, pol.Line_No);
            return null;
        }

        // TODO filter out none-Item types

        return new Line
        {
            Position = pol.Line_No,
            // Description -> not available
            Item = new Item
            {
                Number = pol.No,
                // Revision -> not available
                Name = pol.Description,
                Description = pol.Description_2,
                PurchaseUnitOfMeasureIso = pol.Unit_of_Measure_Code,
                // TODO fetch Vendor Item No from Items
                SupplierItemNumber = ""
            },
            ScheduledDelivery = new ScheduledDelivery
            {
                Date = date,
                Quantity = pol.Quantity
            },
            // ActualDelivery TODO fetch from Purchase Receipt Lines
            Prices = new Prices
            {
                GrossPrice = new Price
                {
                    PriceInTransactionCurrency = new PriceInTransactionCurrency
                    {
                        Value = pol.Direct_Unit_Cost,
                        // TODO provided by the order header
                        CurrencyIso = "EUR"
                    }
                },
                DiscountPercentage = pol.Line_Discount_Percent,
                // There seems only one Unit_of_Measure_Code, and no price unit.
                PriceUnitOfMeasureIso = pol.Unit_of_Measure_Code,
                // Price is always one unit
                PriceUnitQuantity = 1.0m
            },
            // Terms -> not available
            // ProjectNumber -> not available
            ProductionNumber = pol.Prod_Order_No
            // SalesOrderNumber TODO
            // Indicators TODO Completed -> Finished
            // Reason -> not available
        };
    }
}
