using InventoryManagement.Core.Models;

namespace InventoryManagement.Core.Interfaces;

/// <summary>
/// Service interface for automated reorder calculations and alert management.
/// </summary>
public interface IReorderService
{
    /// <summary>Scans all inventory and generates alerts for items below reorder point.</summary>
    Task<List<ReorderAlert>> CheckAndGenerateAlertsAsync();

    /// <summary>Gets all pending (unresolved) reorder alerts.</summary>
    Task<List<ReorderAlert>> GetPendingAlertsAsync();

    /// <summary>Marks an alert as resolved.</summary>
    Task ResolveAlertAsync(int alertId);

    /// <summary>Gets the count of pending alerts.</summary>
    Task<int> GetPendingAlertCountAsync();
}
