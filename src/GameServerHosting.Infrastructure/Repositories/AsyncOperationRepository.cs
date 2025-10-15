using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AsyncOperation entities
/// </summary>
public class AsyncOperationRepository : Repository<AsyncOperation>, IAsyncOperationRepository
{
    public AsyncOperationRepository(GameServerDbContext context) : base(context)
    {
    }

    public async Task<AsyncOperation?> GetByCorrelationIdAsync(Guid correlationId)
    {
        return await _context.AsyncOperations
            .FirstOrDefaultAsync(o => o.CorrelationId == correlationId);
    }

    public async Task<IEnumerable<AsyncOperation>> GetByUserIdAsync(Guid userId)
    {
        return await _context.AsyncOperations
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AsyncOperation>> GetByStatusAsync(AsyncOperationStatus status)
    {
        return await _context.AsyncOperations
            .Where(o => o.Status == status)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }
}
