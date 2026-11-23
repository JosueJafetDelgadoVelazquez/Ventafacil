using Ventafacil.Domain.Entities;

namespace Ventafacil.Domain.Abstractions;

public interface ISaleRepository
{
    Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken ct = default);
    Task CompleteSaleAsync(Sale sale, CancellationToken ct = default);
}
