using CrudApi.DTOs;

namespace CrudApi.Services;

public interface IProductImageStorage
{
    Task<StoredProductImage> SaveAsync(string productCode, IFormFile file, CancellationToken ct);
}