namespace WarrantyRepairLedger.Models;

public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Brand { get; set; }

    public required string Serial { get; set; }

    public DateOnly PurchaseDate { get; set; }

    public int WarrantyMonths { get; set; } = 24;

    public string? Retailer { get; set; }

    public decimal? Price { get; set; }

    public ICollection<Repair> Repairs { get; set; } = new List<Repair>();
}
