using System;                                   // ApplicationException için
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Staj_Proje_1.Models;
using Staj_Proje_1.Services;
using System.Text.Json.Serialization;

namespace Staj_Proje_1.Controllers
{
    /// <summary>
    /// VakıfBank entegrasyon uç noktaları.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class VakifBankController : ControllerBase
    {
        private readonly IBankService _bankService;

        public VakifBankController(IBankService bankService)
        {
            _bankService = bankService;
        }

        // -------------------------------------------------------------
        // TOKEN
        // -------------------------------------------------------------
        [HttpPost("token")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object),       StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Token()
        {
            try
            {
                var token = await _bankService.GetTokenAsync();
                return Ok(new { access_token = token });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = "VakifBank token error", detail = ex.Message });
            }
        }

        // -------------------------------------------------------------
        // HESAP LİSTESİ
        // -------------------------------------------------------------
        [HttpPost("accountList")]
        [ProducesResponseType(typeof(AccountListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AccountList()
        {
            var token = await _bankService.GetTokenAsync();
            var list  = await _bankService.GetAccountListAsync(token);
            return Ok(list);
        }

        // -------------------------------------------------------------
        // HESAP DETAYI
        // -------------------------------------------------------------
        [HttpPost("accountDetail")]
        [ProducesResponseType(typeof(AccountInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object),      StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AccountDetail([FromBody] AccountDetailRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.AccountNumber))
                return BadRequest(new { message = "AccountNumber zorunludur." });

            try
            {
                var token   = await _bankService.GetTokenAsync();
                var account = await _bankService.GetAccountInfoAsync(token, req.AccountNumber);
                return Ok(account);
            }
            catch (ApplicationException ex)
            {
                // VakıfBank 400 dönerse buraya düşer
                return BadRequest(new { message = ex.Message });
            }
        }

        // -------------------------------------------------------------
        // HESAP HAREKETLERİ
        // -------------------------------------------------------------
        [HttpPost("accountTransactions")]
        [ProducesResponseType(typeof(TransactionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object),               StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AccountTransactions([FromBody] AccountDetailRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.AccountNumber))
                return BadRequest(new { message = "AccountNumber zorunludur." });

            try
            {
                var token        = await _bankService.GetTokenAsync();
                var transactions = await _bankService.GetAccountTransactionsAsync(token, req.AccountNumber);
                return Ok(transactions);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    /// <summary>
    /// İstek gövdesinde yalnızca hesap numarası barındırır.
    /// </summary>
    public class AccountDetailRequest
    {
        [JsonPropertyName("AccountNumber")]
        public string AccountNumber { get; set; } = string.Empty;
    }
}
