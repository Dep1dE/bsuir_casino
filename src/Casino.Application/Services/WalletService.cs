using Casino.Domain.Entities;
using Casino.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Casino.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISolanaService _solanaService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        IUnitOfWork unitOfWork,
        ISolanaService solanaService,
        IConfiguration configuration,
        ILogger<WalletService> logger)
    {
        _unitOfWork = unitOfWork;
        _solanaService = solanaService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CreateWalletResponse> CreateWalletAsync(string telegramId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingWallet = await _unitOfWork.Wallets.GetByTelegramIdAsync(telegramId, cancellationToken);
            if (existingWallet != null)
            {
                return new CreateWalletResponse(
                    Success: false,
                    WalletAddress: existingWallet.WalletAddress,
                    Balance: existingWallet.Balance,
                    Message: "Wallet already exists for this Telegram ID"
                );
            }

            var (publicKeyBytes, privateKey) = await _solanaService.CreateWalletAsync(cancellationToken);
            var encryptedPrivateKey = EncryptPrivateKey(privateKey);
            var walletAddress = Convert.ToBase64String(publicKeyBytes);
            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                TelegramId = telegramId,
                WalletAddress = walletAddress,
                EncryptedPrivateKey = encryptedPrivateKey,
                Balance = 0m
            };

            await _unitOfWork.Wallets.CreateAsync(wallet, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created wallet for Telegram ID: {TelegramId}, Address: {Address}", 
                telegramId, walletAddress);

            const decimal initialBalance = 100m;
            string? airdropTransactionHash = null;
            wallet.Balance = initialBalance;
            wallet.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            try
            {
                airdropTransactionHash = await _solanaService.RequestAirdropAsync(
                    walletAddress: walletAddress,
                    amount: initialBalance,
                    cancellationToken: cancellationToken
                );
                
                if (airdropTransactionHash != null)
                {
                    _logger.LogInformation("Initial airdrop successful for wallet {WalletAddress}, Amount: {Amount}, Transaction: {TxHash}", 
                        walletAddress, initialBalance, airdropTransactionHash);
                    
                    await Task.Delay(1000, cancellationToken);
                    decimal blockchainBalance = 0m;
                    for (int i = 0; i < 3; i++)
                    {
                        blockchainBalance = await _solanaService.GetBalanceAsync(walletAddress, cancellationToken);
                        if (blockchainBalance >= initialBalance)
                        {
                            break;
                        }
                        await Task.Delay(500, cancellationToken);
                    }
                    
                    wallet.Balance = Math.Max(initialBalance, blockchainBalance);
                    wallet.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);
                    
                    var initialTransaction = new Casino.Domain.Entities.Transaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = wallet.Id,
                        TransactionHash = airdropTransactionHash,
                        Amount = initialBalance,
                        TransactionType = "initial_deposit",
                        Status = "confirmed",
                        CreatedAt = DateTime.UtcNow,
                        ConfirmedAt = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.Transactions.CreateAsync(initialTransaction, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Initial airdrop failed for wallet {WalletAddress}, but balance was set locally", walletAddress);
                    var initialTransaction = new Casino.Domain.Entities.Transaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = wallet.Id,
                        TransactionHash = $"initial-deposit-{Guid.NewGuid()}",
                        Amount = initialBalance,
                        TransactionType = "initial_deposit",
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow,
                        ConfirmedAt = null
                    };
                    
                    await _unitOfWork.Transactions.CreateAsync(initialTransaction, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during initial airdrop for wallet {WalletAddress}, but balance was set locally", walletAddress);
                wallet.Balance = initialBalance;
                wallet.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);
                
                var initialTransaction = new Casino.Domain.Entities.Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    TransactionHash = $"initial-deposit-{Guid.NewGuid()}",
                    Amount = initialBalance,
                    TransactionType = "initial_deposit",
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    ConfirmedAt = null
                };
                
                await _unitOfWork.Transactions.CreateAsync(initialTransaction, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new CreateWalletResponse(
                Success: true,
                WalletAddress: walletAddress,
                Balance: wallet.Balance,
                Message: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet for Telegram ID: {TelegramId}", telegramId);
            return new CreateWalletResponse(
                Success: false,
                WalletAddress: null,
                Balance: 0m,
                Message: $"Error creating wallet: {ex.Message}"
            );
        }
    }

    public async Task<GetBalanceResponse> GetBalanceAsync(string telegramId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await _unitOfWork.Wallets.GetByTelegramIdAsync(telegramId, cancellationToken);
            
            if (wallet == null)
            {
                return new GetBalanceResponse(
                    Balance: 0m,
                    WalletAddress: null
                );
            }

            var blockchainBalance = await _solanaService.GetBalanceAsync(wallet.WalletAddress, cancellationToken);
            var finalBalance = Math.Max(wallet.Balance, blockchainBalance);
            if (blockchainBalance > wallet.Balance)
            {
                wallet.Balance = blockchainBalance;
                wallet.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new GetBalanceResponse(
                Balance: finalBalance,
                WalletAddress: wallet.WalletAddress
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for Telegram ID: {TelegramId}", telegramId);
            return new GetBalanceResponse(
                Balance: 0m,
                WalletAddress: null
            );
        }
    }

    private string EncryptPrivateKey(string privateKey)
    {
        var encryptionKey = _configuration["Encryption:Key"] ?? "DefaultEncryptionKey12345678901234567890";
        var keyBytes = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 32));
        
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(privateKey);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string DecryptPrivateKey(string encryptedPrivateKey)
    {
        var encryptionKey = _configuration["Encryption:Key"] ?? "DefaultEncryptionKey12345678901234567890";
        var keyBytes = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 32));

        var fullCipher = Convert.FromBase64String(encryptedPrivateKey);
        var iv = new byte[16];
        var cipher = new byte[fullCipher.Length - 16];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    public async Task<DepositResponse> DepositAsync(string telegramId, decimal amount, CancellationToken cancellationToken = default)
    {
        try
        {
            if (amount <= 0)
            {
                return new DepositResponse(
                    Success: false,
                    TransactionHash: null,
                    NewBalance: 0m,
                    Message: "Deposit amount must be greater than 0"
                );
            }

            var wallet = await _unitOfWork.Wallets.GetByTelegramIdAsync(telegramId, cancellationToken);
            if (wallet == null)
            {
                return new DepositResponse(
                    Success: false,
                    TransactionHash: null,
                    NewBalance: 0m,
                    Message: "Wallet not found. Please create a wallet first."
                );
            }

            string? transactionHash = null;
            try
            {
                transactionHash = await _solanaService.RequestAirdropAsync(
                    walletAddress: wallet.WalletAddress,
                    amount: amount,
                    cancellationToken: cancellationToken
                );
                
                if (transactionHash != null)
                {
                    _logger.LogInformation("Airdrop successful for wallet {WalletAddress}, Amount: {Amount}, Transaction: {TxHash}", 
                        wallet.WalletAddress, amount, transactionHash);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Airdrop failed, trying system wallet. Error: {Error}", ex.Message);
            }

            if (transactionHash == null)
            {
                var systemWalletPrivateKey = _configuration["Solana:SystemWalletPrivateKey"];
                
                if (!string.IsNullOrEmpty(systemWalletPrivateKey))
                {
                    try
                    {
                        transactionHash = await _solanaService.SendTransactionAsync(
                            fromPrivateKey: systemWalletPrivateKey,
                            toPublicKey: wallet.WalletAddress,
                            amount: amount,
                            cancellationToken: cancellationToken
                        );
                        
                        _logger.LogInformation("Sent {Amount} SOL from system wallet to {WalletAddress}, Transaction: {TxHash}", 
                            amount, wallet.WalletAddress, transactionHash);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send SOL from system wallet. Continuing with local balance update.");
                    }
                }
                else
                {
                    _logger.LogWarning("System wallet private key not configured and airdrop failed. Updating local balance only.");
                }
            }

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);

            var transaction = new Casino.Domain.Entities.Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                TransactionHash = transactionHash ?? $"local-deposit-{Guid.NewGuid()}",
                Amount = amount,
                TransactionType = "deposit",
                Status = transactionHash != null ? "confirmed" : "pending",
                CreatedAt = DateTime.UtcNow,
                ConfirmedAt = transactionHash != null ? DateTime.UtcNow : null
            };

            await _unitOfWork.Transactions.CreateAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (transactionHash != null)
            {
                await Task.Delay(500, cancellationToken);
                var blockchainBalance = await _solanaService.GetBalanceAsync(wallet.WalletAddress, cancellationToken);
                if (blockchainBalance > wallet.Balance)
                {
                    wallet.Balance = blockchainBalance;
                    await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                var blockchainBalance = await _solanaService.GetBalanceAsync(wallet.WalletAddress, cancellationToken);
                if (blockchainBalance > wallet.Balance)
                {
                    wallet.Balance = blockchainBalance;
                    await _unitOfWork.Wallets.UpdateAsync(wallet, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            _logger.LogInformation("Deposit completed for Telegram ID: {TelegramId}, Amount: {Amount}, New Balance: {Balance}", 
                telegramId, amount, wallet.Balance);

            return new DepositResponse(
                Success: true,
                TransactionHash: transactionHash,
                NewBalance: wallet.Balance,
                Message: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error depositing to wallet for Telegram ID: {TelegramId}", telegramId);
            return new DepositResponse(
                Success: false,
                TransactionHash: null,
                NewBalance: 0m,
                Message: $"Error depositing: {ex.Message}"
            );
        }
    }
}

