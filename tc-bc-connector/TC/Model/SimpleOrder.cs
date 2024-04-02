namespace Com.Tradecloud1.BCconnector.TC.Model;

using Newtonsoft.Json;

public class SingleDeliveryOrder
{
    public Order Order { get; set; }
    public List<Line> Lines { get; set; }
    public DateTime? ErpIssueDateTime { get; set; }
    public Contact? ErpIssuedBy { get; set; }
    public DateTime? ErpLastChangeDateTime { get; set; }
    public Contact? ErpLastChangedBy { get; set; }
}

public class Order
{
    //public string? CompanyId { get; set; }
    public string SupplierAccountNumber { get; set; }
    public string PurchaseOrderNumber { get; set; }
    public string? Description { get; set; }
    public Destination Destination { get; set; }
    public Terms? Terms { get; set; }
    public Indicators? Indicators { get; set; }
    public Contact? Contact { get; set; }
    public Contact? SupplierContact { get; set; }
    public string? OrderType { get; set; }
}

public class Destination
{
    public string? Code { get; set; }
    public List<string>? Names { get; set; }
    public List<string>? AddressLines { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? CountryCodeIso2 { get; set; }
}

public class Terms
{
    public string? IncotermsCode { get; set; }
    public string? Incoterms { get; set; }
    public string? PaymentTermsCode { get; set; }
    public string? PaymentTerms { get; set; }
}

public class Indicators
{
    public bool? Delivered { get; set; }
    public bool? Completed { get; set; }
    public bool? Cancelled { get; set; }
    public bool? CancelLineWhenMissing { get; set; }
}

public class Contact
{
    public string? Email { get; set; }
}

public class Line
{
    public string Position { get; set; }
    public string? Row { get; set; }
    public string? Description { get; set; }
    public Item Item { get; set; }
    public ScheduledDelivery? ScheduledDelivery { get; set; }
    public ActualDelivery? ActualDelivery { get; set; }
    public Prices Prices { get; set; }
    public Terms? Terms { get; set; }
    public string? ProjectNumber { get; set; }
    public string? ProductionNumber { get; set; }
    public string? SalesOrderNumber { get; set; }
    public Indicators? Indicators { get; set; }
    public string? Reason { get; set; }
}

public class Item
{
    public string Number { get; set; }
    public string? Revision { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string PurchaseUnitOfMeasureIso { get; set; }
    public string? SupplierItemNumber { get; set; }
}

public class ActualDelivery
{
    public DateTime Date { get; set; }
    public decimal Quantity { get; set; }
}
