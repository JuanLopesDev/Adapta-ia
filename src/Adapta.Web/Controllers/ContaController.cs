using System.Security.Claims;
using Adapta.Web.Models;
using Adapta.Web.Services;
using Adapta.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Adapta.Web.Controllers;

[Authorize]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ContaController(IContaService contas, RecuperacaoSenhaService recuperacao) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Entrar() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Painel") : View(new LoginViewModel());

    [HttpPost, AllowAnonymous, EnableRateLimiting("conta")]
    public async Task<IActionResult> Entrar(LoginViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid)
            return View(model);
        if (await contas.EntrarAsync(model.Matricula, model.Senha))
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Painel");

        ModelState.AddModelError("", "Não foi possível entrar. Confira a matrícula e a senha ou tente novamente mais tarde.");
        return View(model);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult EsqueciSenha() => View(new EsqueciSenhaViewModel());

    [HttpPost, AllowAnonymous, EnableRateLimiting("conta")]
    public async Task<IActionResult> EsqueciSenha(EsqueciSenhaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        await recuperacao.SolicitarAsync(model.Matricula);
        return View("RecuperacaoSolicitada");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult RedefinirSenha(string id, string token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return View(new RedefinirSenhaViewModel { Id = id, Token = token });
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> RedefinirSenha(RedefinirSenhaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        if (!await recuperacao.RedefinirAsync(model.Id, model.Token, model.Nova))
        {
            ModelState.AddModelError("", "O link é inválido ou expirou. Solicite outro em Esqueci minha senha.");
            return View(model);
        }
        TempData["Mensagem"] = "Senha redefinida. Entre com a nova senha.";
        return RedirectToAction(nameof(Entrar));
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var conta = await contas.ObterAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (conta is null)
        {
            await contas.SairAsync();
            return RedirectToAction(nameof(Entrar));
        }
        return View(conta);
    }

    [HttpGet]
    public IActionResult AlterarSenha() => View(new AlterarSenhaViewModel());

    [HttpPost]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        if (!await contas.AlterarSenhaAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, model.Atual, model.Nova))
        {
            ModelState.AddModelError("", "Confira a senha atual e escolha uma senha diferente que atenda aos requisitos.");
            return View(model);
        }
        TempData["Mensagem"] = "Senha alterada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Sair()
    {
        await contas.SairAsync();
        return RedirectToAction(nameof(Entrar));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AcessoNegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
