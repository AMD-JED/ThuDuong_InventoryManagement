using System.Linq.Expressions;

namespace InventoryManagement.Core.Interfaces;

/// <summary>
/// Generic repository interface providing standard CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type managed by this repository.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Gets an entity by its primary key.</summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>Gets all entities of this type.</summary>
    Task<List<T>> GetAllAsync();

    /// <summary>Finds entities matching a predicate.</summary>
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Adds a new entity.</summary>
    Task<T> AddAsync(T entity);

    /// <summary>Updates an existing entity.</summary>
    Task UpdateAsync(T entity);

    /// <summary>Deletes an entity by ID.</summary>
    Task DeleteAsync(int id);

    /// <summary>Saves all pending changes to the database.</summary>
    Task SaveChangesAsync();
}
