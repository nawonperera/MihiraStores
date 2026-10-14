namespace MihiraStores.Api.Models;

public class Stock
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
