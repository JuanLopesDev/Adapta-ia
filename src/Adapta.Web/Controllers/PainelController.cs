using System.Security.Claims;
using Adapta.Web.Services;
using Adapta.Web.Models;
using Adapta.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adapta.Web.Controllers;

[Authorize]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class PainelController(PortalDocenteService portal, AdaptacaoService adaptacoes) : Controller
{
    private string ProfessorId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await portal.PainelAsync(ProfessorId, ct));
    public async Task<IActionResult> Avaliacoes(int pagina = 1, CancellationToken ct = default) => View(await portal.ListarAsync(ProfessorId, pagina, ct));
    public async Task<IActionResult> Historico(int pagina = 1, CancellationToken ct = default) => View(await portal.HistoricoAsync(ProfessorId, pagina, ct));

    [HttpGet]
    public async Task<IActionResult> NovaAdaptacao(Guid? documentoId, string? perfil, CancellationToken ct)
    {
        var form = new NovaAdaptacaoViewModel { DocumentoId = documentoId.GetValueOrDefault(), Perfil = PerfisAvaliacao.Disponivel(perfil) ? perfil : null };
        await adaptacoes.PreencherAsync(ProfessorId, form, ct);
        return View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Gerar([Bind("Perfil,DocumentoId,SolicitacaoId,NivelSuporte,LeituraAmpla,GuiaInstrucoes,EspacoResposta,ComandosDiretos")] NovaAdaptacaoViewModel form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await adaptacoes.GerarAsync(ProfessorId, form, ct);
                return id == null ? NotFound() : RedirectToAction(nameof(Adaptacao), new { id });
            }
            catch (InvalidDataException ex) { ModelState.AddModelError("", ex.Message); }
        }
        await adaptacoes.PreencherAsync(ProfessorId, form, ct);
        return View(nameof(NovaAdaptacao), form);
    }

    [HttpPost]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
    {
        return await adaptacoes.AprovarAsync(ProfessorId, id, ct)
            ? RedirectToAction(nameof(Adaptacao), new { id }) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> BaixarPdf(Guid id, CancellationToken ct)
    {
        var resultado = await adaptacoes.ObterAsync(ProfessorId, id, ct);
        if (resultado == null) return NotFound();
        if (resultado.Estado != EstadoAdaptacao.Aprovada) return Conflict("Aprove a versão antes de baixar a folha.");
        return File(FolhaPdf.Gerar(resultado), "application/pdf", Path.GetFileNameWithoutExtension(resultado.Nome) + "-adaptada.pdf");
    }

    public async Task<IActionResult> Adaptacao(Guid id, CancellationToken ct)
    {
        var resultado = await adaptacoes.ObterAsync(ProfessorId, id, ct);
        return resultado == null ? NotFound() : View(resultado);
    }
}
