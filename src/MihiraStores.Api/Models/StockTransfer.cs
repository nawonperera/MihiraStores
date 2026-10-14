namespace MihiraStores.Api.Models;

public class StockTransfer
{
    public int Id { get; set; }
    public int FromBranchId { get; set; }
    public Branch? FromBranch { get; set; }
    public int ToBranchId { get; set; }
    public Branch? ToBranch { get; set; }
    public TransferStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<StockTransferLine> Lines { get; set; } = new List<StockTransferLine>();
}
