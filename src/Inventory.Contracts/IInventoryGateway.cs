using System;
using System.Runtime.InteropServices;

namespace Inventory.Contracts
{
    [ComVisible(true)]
    [Guid("A94EF148-30B6-4EFA-82B4-3C3B5DBE8E90")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IInventoryGateway
    {
        string Ping(string clientName);

        decimal Sum(decimal firstValue, decimal secondValue);

        AuthenticationResult Authenticate(string username, string password);

        CatalogItem[] GetCatalog(string catalogType);

        CatalogItem SaveCatalog(string catalogType, CatalogItem item);

        void DeactivateCatalog(string catalogType, int id);

        InventoryItem[] GetInventory(int branchId);

        void RegisterInventoryMovement(int userId, int branchId, int productId, string movementType, int quantity, string notes);

        int CreateTransfer(int userId, int originBranchId, int destinationBranchId, int productId, int quantity, string notes);

        void ResolveTransfer(int userId, int transferId, bool approve);

        TransferItem[] GetTransfers();

        ReportRow[] GetReport(string reportType, DateTime startDate, DateTime endDate);

        AuditRecord[] GetAuditLog(DateTime startDate, DateTime endDate);

        int RegisterSale(int userId, int branchId, SaleLine[] lines);
    }
}
