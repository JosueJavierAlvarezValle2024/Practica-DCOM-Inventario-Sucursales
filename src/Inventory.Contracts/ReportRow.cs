using System;
using System.Runtime.InteropServices;
namespace Inventory.Contracts
{
    [ComVisible(true)] [Guid("E8E2B6D9-5BD4-4A74-A6C9-2E944DE554E4")] [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ReportRow { public string Item { get; set; } public string Detail { get; set; } public decimal Amount { get; set; } public int Quantity { get; set; } public DateTime Date { get; set; } }
}
