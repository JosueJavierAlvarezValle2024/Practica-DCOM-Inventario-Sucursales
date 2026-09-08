using System;
using System.Runtime.InteropServices;

namespace Inventory.Contracts
{
    [ComVisible(true)]
    [Guid("0F1C48FC-A244-4B7B-B53F-7D916353E95E")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class InventoryItem
    {
        public int ProductId { get; set; }
        public string Code { get; set; }
        public string ProductName { get; set; }
        public int Stock { get; set; }
        public int MinimumStock { get; set; }
        public bool LowStock { get; set; }
    }
}
