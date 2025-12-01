using Casino.Domain.Entities;

namespace Casino.Domain.Repositories;

public interface IBetRepository
{
    Task<Bet> CreateAsync(Bet bet, CancellationToken cancellationToken = default);
    Task<Bet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bet>> GetByTelegramIdAsync(string telegramId, int limit = 100, CancellationToken cancellationToken = default);
}

