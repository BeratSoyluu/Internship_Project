using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Staj_Proje_1.Models.OpenBanking;
using Staj_Proje_1.Services;
using System.Security.Claims;

namespace Staj_Proje_1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // JWT zorunlu
public class OpenBankingController : ControllerBase
{
    private readonly IOpenBankingService _svc;

    public OpenBankingController(IOpenBankingService svc)
    {
        _svc = svc;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "demo-user";

    [HttpGet("linked-banks")]
    public async Task<ActionResult<IEnumerable<BankDto>>> GetLinkedBanks(CancellationToken ct)
    {
        var data = await _svc.GetLinkedBanksAsync(UserId, ct);
        return Ok(data);
    }

    [HttpPost("link")]
    public async Task<ActionResult<BankDto>> LinkBank([FromBody] LinkBankRequest req, CancellationToken ct)
    {
        var data = await _svc.LinkBankAsync(UserId, req, ct);
        return Ok(data);
    }

    [HttpGet("accounts")]
    public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccounts([FromQuery] BankCode bank, CancellationToken ct)
    {
        var data = await _svc.GetAccountsAsync(UserId, bank, ct);
        return Ok(data);
    }

    [HttpGet("recent-transactions")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetRecent([FromQuery] BankCode bank, [FromQuery] int take = 5, CancellationToken ct = default)
    {
        var data = await _svc.GetRecentTransactionsAsync(UserId, bank, take, ct);
        return Ok(data);
    }
}
