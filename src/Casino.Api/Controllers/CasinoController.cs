using Casino.Api.Models;
using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;
using CreateWalletResponse = Casino.Application.Services.CreateWalletResponse;
using GetBalanceResponse = Casino.Application.Services.GetBalanceResponse;
using ApplicationPlaceBetRequest = Casino.Application.Services.PlaceBetRequest;
using PlaceBetResponse = Casino.Application.Services.PlaceBetResponse;
using DepositResponse = Casino.Application.Services.DepositResponse;

namespace Casino.Api.Controllers;

[ApiController]
[Route("casino")]
public class CasinoController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IBetService _betService;
    private readonly ILogger<CasinoController> _logger;

    public CasinoController(
        IWalletService walletService,
        IBetService betService,
        ILogger<CasinoController> logger)
    {
        _walletService = walletService;
        _betService = betService;
        _logger = logger;
    }

    [HttpPost("wallet/create")]
    [ProducesResponseType(typeof(CreateWalletResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CreateWalletResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateWalletResponse>> CreateWallet(
        [FromBody] CreateWalletRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TelegramId))
        {
            return BadRequest(new CreateWalletResponse(
                Success: false,
                WalletAddress: null,
                Balance: 0m,
                Message: "Telegram ID is required"
            ));
        }

        var result = await _walletService.CreateWalletAsync(request.TelegramId, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("wallet/balance")]
    [ProducesResponseType(typeof(GetBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GetBalanceResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetBalanceResponse>> GetBalance(
        [FromQuery(Name = "telegram_id")] string telegramId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(telegramId))
        {
            return BadRequest(new GetBalanceResponse(
                Balance: 0m,
                WalletAddress: null
            ));
        }

        var result = await _walletService.GetBalanceAsync(telegramId, cancellationToken);

        if (result.WalletAddress == null)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("bet")]
    [ProducesResponseType(typeof(PlaceBetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PlaceBetResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PlaceBetResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceBetResponse>> PlaceBet(
        [FromBody] Casino.Api.Models.PlaceBetRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TelegramId))
        {
            return BadRequest(new PlaceBetResponse(
                Success: false,
                TransactionHash: null,
                WinAmount: 0m,
                Balance: 0m,
                Message: "Telegram ID is required",
                WinningCombination: new List<string>()
            ));
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new PlaceBetResponse(
                Success: false,
                TransactionHash: null,
                WinAmount: 0m,
                Balance: 0m,
                Message: "Bet amount must be greater than 0",
                WinningCombination: new List<string>()
            ));
        }

        var betRequest = new ApplicationPlaceBetRequest(
            request.TelegramId,
            request.Amount,
            request.GameType,
            request.BetData
        );

        var result = await _betService.PlaceBetAsync(betRequest, cancellationToken);

        if (!result.Success)
        {
            if (result.Message?.Contains("not found") == true)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("wallet/deposit")]
    [ProducesResponseType(typeof(DepositResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DepositResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DepositResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepositResponse>> Deposit(
        [FromBody] Casino.Api.Models.DepositRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TelegramId))
        {
            return BadRequest(new DepositResponse(
                Success: false,
                TransactionHash: null,
                NewBalance: 0m,
                Message: "Telegram ID is required"
            ));
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new DepositResponse(
                Success: false,
                TransactionHash: null,
                NewBalance: 0m,
                Message: "Deposit amount must be greater than 0"
            ));
        }

        var result = await _walletService.DepositAsync(request.TelegramId, request.Amount, cancellationToken);

        if (!result.Success)
        {
            if (result.Message?.Contains("not found") == true)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}

