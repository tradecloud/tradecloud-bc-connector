namespace Com.Tradecloud1.BCconnector.TC.Model;

public class ScheduledDelivery
{
    public DateTime? Date { get; set; }
    public decimal Quantity { get; set; }
}

public class Prices
{
    public Price? GrossPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public Price? NetPrice { get; set; }
    public string PriceUnitOfMeasureIso { get; set; }
    public decimal PriceUnitQuantity { get; set; }
}

public class Price
{
    public PriceInTransactionCurrency PriceInTransactionCurrency { get; set; }
}

public class PriceInTransactionCurrency
{
    public decimal Value { get; set; }
    public string CurrencyIso { get; set; }
}