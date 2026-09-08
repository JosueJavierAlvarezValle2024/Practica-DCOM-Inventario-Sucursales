using System;
using System.Runtime.InteropServices;

namespace Inventory.Contracts
{
    [ComVisible(true)]
    [Guid("5ED8825A-1E4B-47C5-82AA-E3DE7273CB00")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class TransferItem
    {
        public int TransferId { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Status { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
}
