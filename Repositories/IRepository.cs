namespace StarterApp.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task<T> UpdateAsync(Guid id, Action<T> applyChanges);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
