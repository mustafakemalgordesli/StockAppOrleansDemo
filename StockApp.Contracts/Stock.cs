namespace StockApp.Contracts;

[GenerateSerializer, Alias(nameof(Stock))]
public class Stock
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string Name { get; set; }
    [Id(2)]
    public int Quantity { get; set; }
}
