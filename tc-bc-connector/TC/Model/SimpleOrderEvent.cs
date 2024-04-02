namespace Com.Tradecloud1.BCconnector.TC.Model;

public class WebhookBody
{
    /* Common buyer events are, depending on the webhook event configuration in the portal:
     * - OrderChangesProposalApprovedByBuyer
     * - OrderLinesAcceptedBySupplier
     * - OrderLinesConfirmedBySupplier
     * - OrderLinesRejectedBySupplier
     * - OrderLinesReopenRequestApprovedByBuyer
     * - OrderLinesReopenRequestApprovedBySupplier
     * - OrderResentByBuyer
     */
    public string EventName { get; set; }
    public SingleDeliveryOrderEvent SingleDeliveryOrderEvent { get; set; }
}

public class SingleDeliveryOrderEvent
{
    public string OrderId { get; set; }
    public BuyerOrder BuyerOrder { get; set; }
    public SupplierOrder SupplierOrder { get; set; }
    public List<SingleDeliveryOrderLine> Lines { get; set; }
    public Status Status { get; set; }
    public string Reason { get; set; }
    public MessageMeta Meta { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string PortalUrl { get; set; }
}

public class BuyerOrder
{
    public string CompanyId { get; set; }
    public string PurchaseOrderNumber { get; set; }
    public string SupplierAccountNumber { get; set; }
}

public class SupplierOrder
{
    public string CompanyId { get; set; }
    public string Description { get; set; }
}

public class Status
{
    // One of: Issued, InProgress, Confirmed, Rejected, Completed, Cancelled
    public string ProcessStatus { get; set; }
    // One of: Open, Produced, ReadyToShip, Shipped, Delivered, Cancelled
    public string LogisticsStatus { get; set; }
}

public class MessageMeta
{
    public Guid MessageId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public MessageSource Source { get; set; }
}

public class MessageSource
{
    public Guid TraceId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Origin { get; set; }
}

public class SingleDeliveryOrderLine
{
    public string Id { get; set; }
    public SingleDeliveryBuyerLine BuyerLine { get; set; }
    public SingleDeliverySupplierLine SupplierLine { get; set; }
    public SingleDeliveryStatusLine StatusLine { get; set; }
    public Status Status { get; set; }
    public string Reason { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string PortalUrl { get; set; }
}

public class SingleDeliveryBuyerLine
{
    public string Position { get; set; }
    public Item Item { get; set; }
}

public class SingleDeliverySupplierLine
{
    public string SalesOrderNumber { get; set; }
    public string SalesOrderLinePosition { get; set; }
    public string Description { get; set; }
}

public class SingleDeliveryStatusLine
{
    public ScheduledDelivery ScheduledDelivery { get; set; }
    public Prices Prices { get; set; }
}


