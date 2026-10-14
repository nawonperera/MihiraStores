namespace MihiraStores.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;

    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
    public ICollection<StockTransferLine> TransferLines { get; set; } = new List<StockTransferLine>();
}
