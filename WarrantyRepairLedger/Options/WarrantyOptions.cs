namespace WarrantyRepairLedger.Options;

public class WarrantyOptions
{
    public const string SectionName = "Warranty";

    public int DefaultMonths { get; set; } = 24;

    public int RepairExtensionMonths { get; set; } = 12;
}
