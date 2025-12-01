using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Messages;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Programs;

namespace Casino.Application.Services;

public class SolanaService : ISolanaService
{
    private readonly IRpcClient _rpcClient;
    private readonly ILogger<SolanaService> _logger;

    public SolanaService(IConfiguration configuration, ILogger<SolanaService> logger)
    {
        var rpcUrl = configuration["Solana:RpcUrl"] ?? "http://localhost:8899";
        _rpcClient = ClientFactory.GetClient(rpcUrl);
        _logger = logger;
    }

    public Task<(byte[] PublicKey, string PrivateKey)> CreateWalletAsync(CancellationToken cancellationToken = default)
    {
        var account = new Solnet.Wallet.Account();
        var publicKey = account.PublicKey.KeyBytes;
        var privateKey = account.PrivateKey.KeyBytes;

        return Task.FromResult<(byte[], string)>(
            (publicKey, Convert.ToBase64String(privateKey))
        );
    }

    public async Task<decimal> GetBalanceAsync(string walletAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var publicKeyBytes = Convert.FromBase64String(walletAddress);
            var publicKey = new PublicKey(publicKeyBytes);
            var result = await _rpcClient.GetBalanceAsync(publicKey, commitment: Commitment.Confirmed);

            if (result.WasSuccessful && result.Result != null)
            {
                var balanceInSol = (decimal)result.Result.Value / 1_000_000_000m;
                return balanceInSol;
            }

            _logger.LogWarning("Failed to get balance for wallet {WalletAddress}: {Error}", 
                walletAddress, result.Reason);
            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for wallet {WalletAddress}", walletAddress);
            return 0m;
        }
    }

    public async Task<string> SendTransactionAsync(
        string fromPrivateKey, 
        string toPublicKey, 
        decimal amount, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var privateKeyBytes = Convert.FromBase64String(fromPrivateKey);
            Solnet.Wallet.Account fromAccount;
            try
            {
                byte[] seed;
                if (privateKeyBytes.Length == 64)
                {
                    seed = new byte[32];
                    Array.Copy(privateKeyBytes, 0, seed, 0, 32);
                }
                else if (privateKeyBytes.Length == 32)
                {
                    seed = privateKeyBytes;
                }
                else
                {
                    throw new ArgumentException($"Invalid private key length: {privateKeyBytes.Length}. Expected 32 or 64 bytes.");
                }
                
                var accountType = typeof(Solnet.Wallet.Account);
                var constructors = accountType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.ConstructorInfo? targetConstructor = null;
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(byte[]))
                    {
                        targetConstructor = constructor;
                        break;
                    }
                }
                
                if (targetConstructor != null)
                {
                    fromAccount = (Solnet.Wallet.Account)targetConstructor.Invoke(new object[] { seed });
                }
                else
                {
                    throw new InvalidOperationException(
                        "Solnet 5.0.0 Account class does not have a public constructor that accepts byte array. " +
                        "Please check Solnet documentation for the correct way to restore Account from private key. " +
                        "You may need to use Wallet class or a different version of Solnet.");
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Failed to create Account from private key. Length: {Length}", privateKeyBytes.Length);
                throw new InvalidOperationException(
                    "Cannot restore Account from private key. " +
                    "Please verify Solnet 5.0.0 API for Account creation.", ex);
            }
            
            var toPublicKeyBytes = Convert.FromBase64String(toPublicKey);
            var toAccount = new PublicKey(toPublicKeyBytes);
            var lamports = (ulong)(amount * 1_000_000_000m);
            var blockHashResult = await _rpcClient.GetRecentBlockHashAsync(Commitment.Confirmed);
            if (!blockHashResult.WasSuccessful || blockHashResult.Result == null)
            {
                throw new Exception($"Failed to get blockhash: {blockHashResult.Reason}");
            }

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(SystemProgram.Transfer(
                    fromAccount.PublicKey,
                    toAccount,
                    lamports))
                .Build(fromAccount);

            var sendResult = await _rpcClient.SendTransactionAsync(transaction, commitment: Commitment.Confirmed);

            if (!sendResult.WasSuccessful)
            {
                throw new Exception($"Failed to send transaction: {sendResult.Reason}");
            }

            return sendResult.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transaction from {From} to {To} amount {Amount}", 
                fromPrivateKey, toPublicKey, amount);
            throw;
        }
    }

    public async Task<string?> RequestAirdropAsync(string walletAddress, decimal amount, CancellationToken cancellationToken = default)
    {
        try
        {
            var publicKeyBytes = Convert.FromBase64String(walletAddress);
            var publicKey = new PublicKey(publicKeyBytes);
            var lamports = (ulong)(amount * 1_000_000_000m);
            var result = await _rpcClient.RequestAirdropAsync(publicKey, lamports, Commitment.Confirmed);

            if (result.WasSuccessful && result.Result != null)
            {
                _logger.LogInformation("Airdrop successful for wallet {WalletAddress}, Amount: {Amount}, Signature: {Signature}", 
                    walletAddress, amount, result.Result);
                return result.Result;
            }

            _logger.LogWarning("Failed to request airdrop for wallet {WalletAddress}: {Error}", 
                walletAddress, result.Reason);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting airdrop for wallet {WalletAddress}", walletAddress);
            return null;
        }
    }
}

