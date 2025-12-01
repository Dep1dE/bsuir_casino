using Casino.Domain.Data;
using Casino.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Casino.Domain.Repositories;

public class BetRepository : IBetRepository
{
    private readonly CasinoDbContext _context;

    public BetRepository(CasinoDbContext context)
    {
        _context = context;
    }

    public async Task<Bet> CreateAsync(Bet bet, CancellationToken cancellationToken = default)
    {
        bet.CreatedAt = DateTime.UtcNow;
        _context.Bets.Add(bet);
        await _context.SaveChangesAsync(cancellationToken);
        return bet;
    }

    public async Task<Bet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Bets
            .Include(b => b.Wallet)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Bet>> GetByTelegramIdAsync(string telegramId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Bets
            .Where(b => b.TelegramId == telegramId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

