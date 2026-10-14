namespace MihiraStores.Api.Models;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsWarehouse { get; set; }

    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
    public ICollection<StockTransfer> OutgoingTransfers { get; set; } = new List<StockTransfer>();
    public ICollection<StockTransfer> IncomingTransfers { get; set; } = new List<StockTransfer>();
}
