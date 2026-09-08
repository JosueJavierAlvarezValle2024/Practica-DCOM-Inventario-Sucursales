using System;
using System.Runtime.InteropServices;

namespace Inventory.Contracts
{
    [ComVisible(true)]
    [Guid("6B1AB2D0-4CF8-4B8A-996B-85C8B01A2F9D")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class AuthenticationResult
    {
        public bool IsAuthenticated { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
    }
}
