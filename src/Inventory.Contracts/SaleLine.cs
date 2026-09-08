using System; using System.Runtime.InteropServices;
namespace Inventory.Contracts { [ComVisible(true)] [Guid("A8E1B2C3-D4E5-46F7-A809-B1C2D3E4F506")] [ClassInterface(ClassInterfaceType.AutoDual)] public class SaleLine { public int ProductId { get; set; } public int Quantity { get; set; } } }
