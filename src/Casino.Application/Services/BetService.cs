using Casino.Domain.Entities;
using Casino.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Casino.Application.Services;

public class BetService : IBetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISolanaService _solanaService;
    private readonly ILogger<BetService> _logger;
    private readonly Random _random = new();

    public BetService(
        IUnitOfWork unitOfWork,
        ISolanaService solanaService,
        ILogger<BetService> logger)
    {
        _unitOfWork = unitOfWork;
        _solanaService = solanaService;
        _logger = logger;
    }

    public async Task<PlaceBetResponse> PlaceBetAsync(PlaceBetRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await _unitOfWork.Wallets.GetByTelegramIdAsync(request.TelegramId, cancellationToken);
            if (wallet == null)
            {
            return new PlaceBetResponse(
                Success: false,
                TransactionHash: null,
                WinAmount: 0m,
                Balance: 0m,
                Message: "Wallet not found",
                WinningCombination: new List<string>()
            );
            }

            var blockchainBalance = await _solanaService.GetBalanceAsync(wallet.WalletAddress, cancellationToken);
            var currentBalance = Math.Max(wallet.Balance, blockchainBalance);
            if (blockchainBalance > wallet.Balance)
            {
                wallet.Balance = blockchainBalance;
                wallet.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            
            if (currentBalance < request.Amount)
            {
            return new PlaceBetResponse(
                Success: false,
                TransactionHash: null,
                WinAmount: 0m,
                Balance: currentBalance,
                Message: "Insufficient balance",
                WinningCombination: new List<string>()
            );
            }

            var (winningCombination, winAmount) = ExecuteSlotGameLogic(request.Amount);
            var bet = new Bet
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                TelegramId = request.TelegramId,
                Amount = request.Amount,
                WinAmount = winAmount,
                GameType = request.GameType,
                BetData = request.BetData != null ? JsonSerializer.Serialize(request.BetData) : null,
                GameResult = JsonSerializer.Serialize(new { winning_combination = winningCombination, win_amount = winAmount })
            };

            wallet.Balance = currentBalance - request.Amount + winAmount;
            wallet.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);

            var transactionHash = $"bet_{Guid.NewGuid():N}";
            bet.TransactionHash = transactionHash;
            await _unitOfWork.Bets.CreateAsync(bet, cancellationToken);

            var betTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                TransactionHash = transactionHash,
                Amount = -request.Amount,
                TransactionType = "bet",
                Status = "confirmed",
                CreatedAt = DateTime.UtcNow,
                ConfirmedAt = DateTime.UtcNow
            };
            await _unitOfWork.Transactions.CreateAsync(betTransaction, cancellationToken);

            if (winAmount > 0)
            {
                var winTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    TransactionHash = transactionHash,
                    Amount = winAmount,
                    TransactionType = "win",
                    Status = "confirmed",
                    CreatedAt = DateTime.UtcNow,
                    ConfirmedAt = DateTime.UtcNow
                };
                await _unitOfWork.Transactions.CreateAsync(winTransaction, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedBalance = wallet.Balance;

            _logger.LogInformation(
                "Bet placed: TelegramId={TelegramId}, Amount={Amount}, WinAmount={WinAmount}, Balance={Balance}",
                request.TelegramId, request.Amount, winAmount, updatedBalance);

            var message = winAmount > 0 
                ? "You won!" 
                : "Better luck next time!";

            return new PlaceBetResponse(
                Success: true,
                TransactionHash: transactionHash,
                WinAmount: winAmount,
                Balance: updatedBalance,
                Message: message,
                WinningCombination: winningCombination
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing bet for Telegram ID: {TelegramId}", request.TelegramId);
            return new PlaceBetResponse(
                Success: false,
                TransactionHash: null,
                WinAmount: 0m,
                Balance: 0m,
                Message: $"Error placing bet: {ex.Message}",
                WinningCombination: new List<string>()
            );
        }
    }

    private (List<string> winningCombination, decimal winAmount) ExecuteSlotGameLogic(decimal betAmount)
    {
        var slotElements = new[] { "bar", "bell", "cherry", "club", "diamond", "heart", 
            "lemon", "orange", "plum", "seven", "spade", "star" };

        var combination = new List<string>
        {
            slotElements[_random.Next(slotElements.Length)],
            slotElements[_random.Next(slotElements.Length)],
            slotElements[_random.Next(slotElements.Length)]
        };

        var isWin = combination[0] == combination[1] && combination[1] == combination[2];
        
        decimal winAmount = 0m;
        if (isWin)
        {
            var element = combination[0];
            var multiplier = GetSlotMultiplier(element);
            winAmount = betAmount * multiplier;
        }

        return (combination, winAmount);
    }

    private decimal GetSlotMultiplier(string element)
    {
        return element switch
        {
            "seven" => 100m,
            "diamond" => 50m,
            "star" => 30m,
            "bell" => 20m,
            "bar" => 15m,
            "heart" => 10m,
            "spade" => 10m,
            "club" => 10m,
            "cherry" => 5m,
            "lemon" => 5m,
            "orange" => 5m,
            "plum" => 5m,
            _ => 0m
        };
    }
}

