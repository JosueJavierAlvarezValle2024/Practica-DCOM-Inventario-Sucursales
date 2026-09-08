using System;
using System.Runtime.InteropServices;

namespace Inventory.Contracts
{
    [ComVisible(true)]
    [Guid("F1A16A2B-47F7-460A-99BB-8F60A62AFD2B")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class CatalogItem
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public int MinimumStock { get; set; }
        public bool Active { get; set; }
    }
}
