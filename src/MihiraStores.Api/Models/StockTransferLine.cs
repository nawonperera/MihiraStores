namespace MihiraStores.Api.Models;

public class StockTransferLine
{
    public int Id { get; set; }
    public int StockTransferId { get; set; }
    public StockTransfer? StockTransfer { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
}
