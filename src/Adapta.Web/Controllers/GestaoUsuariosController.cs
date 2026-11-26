using Adapta.Web.Models;
using Adapta.Web.Services;
using Adapta.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Adapta.Web.Controllers;

[Authorize(Policy = Papeis.GerirUsuarios)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route("Conta/Administracao")]
public sealed class GestaoUsuariosController(GestaoUsuariosService gestao) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await gestao.ListarAsync(ct));

    [HttpGet("criar")]
    public IActionResult Criar() => View(new CriarUsuarioViewModel());

    [HttpPost("criar")]
    public async Task<IActionResult> Criar(CriarUsuarioViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var resultado = await gestao.CriarAsync(model.Nome, model.Matricula, model.Nova);
        if (!resultado.Succeeded) { MostrarErros(resultado); return View(model); }
        TempData["Mensagem"] = "Conta de professor criada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("editar-nome")]
    public async Task<IActionResult> EditarNome(string matricula)
    {
        var professor = await gestao.BuscarAsync(matricula ?? "");
        if (professor == null) return NotFound();
        ViewData["Matricula"] = professor.UserName;
        return View(new EditarNomeViewModel { Nome = professor.Nome, Versao = professor.ConcurrencyStamp! });
    }

    [HttpPost("editar-nome")]
    public async Task<IActionResult> EditarNome([FromQuery] string matricula, EditarNomeViewModel model)
    {
        var professor = await gestao.BuscarAsync(matricula ?? "");
        if (professor == null) return NotFound();
        ViewData["Matricula"] = professor.UserName;
        if (!ModelState.IsValid) return View(model);
        var resultado = await gestao.RenomearAsync(professor, model.Nome, model.Versao);
        if (!resultado.Succeeded) { MostrarErros(resultado); return View(model); }
        TempData["Mensagem"] = "Nome atualizado.";
        return RedirectToAction(nameof(Index));
    }

    private void MostrarErros(IdentityResult resultado)
    {
        foreach (var erro in resultado.Errors)
        {
            var mensagem = erro.Code switch
            {
                "DuplicateUserName" => "Já existe uma conta com essa matrícula.",
                "InvalidUserName" => "A matrícula contém caracteres não aceitos.",
                "ConcurrencyFailure" => "A conta foi alterada em outra sessão. Reabra a edição antes de salvar.",
                "PasswordTooShort" or "PasswordRequiresUpper" or "PasswordRequiresLower" => NovaSenhaViewModel.Requisitos,
                _ => "Não foi possível salvar a conta. Confira os dados informados."
            };
            ModelState.AddModelError("", mensagem);
        }
    }
}
