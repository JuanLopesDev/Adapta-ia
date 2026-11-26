using System.Security.Claims;
using System.Text.Json;
using Adapta.Web.Models;
using Adapta.Web.Services.Documentos;
using Adapta.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adapta.Web.Controllers;

[Authorize, ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class DocumentosController(DocumentoService documentos, IDocumentStorageService storage) : Controller
{
    private string Professor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet] public IActionResult Enviar() => View();

    [HttpPost, RequestSizeLimit(DocumentStorage.Limite + 65536), RequestFormLimits(MultipartBodyLengthLimit = DocumentStorage.Limite + 65536)]
    public async Task<IActionResult> Enviar(IFormFile? arquivo, bool adaptar, CancellationToken ct)
    {
        if (arquivo == null || arquivo.Length is <= 0 or > DocumentStorage.Limite)
            ModelState.AddModelError("", "Selecione um documento de até 10 MiB.");
        if (!ModelState.IsValid) return View();
        try
        {
            using var stream = arquivo!.OpenReadStream();
            var id = await documentos.ReceberAsync(Professor, arquivo.FileName, stream, ct);
            TempData["Mensagem"] = "Original armazenado.";
            return RedirectToAction(nameof(Revisar), new { id, adaptar = adaptar ? (bool?)true : null });
        }
        catch (InvalidDataException ex) { ModelState.AddModelError("", ex.Message); return View(); }
    }

    [HttpGet]
    public async Task<IActionResult> Revisar(Guid id, bool adaptar, string? perfil, CancellationToken ct)
    {
        var doc = await documentos.BuscarAsync(Professor, id, ct);
        return doc is null ? NotFound() : View(new DocumentoViewModel(doc,
            doc.ExtracaoJson == null || doc.ExclusaoSolicitada ? null : RevisaoDocumento.Ler(doc), adaptar, perfil));
    }

    [HttpGet]
    public async Task<IActionResult> Original(Guid id, bool visualizar, CancellationToken ct)
    {
        var doc = await documentos.BuscarAsync(Professor, id, ct);
        if (doc is null || doc.ExclusaoSolicitada) return NotFound();
        var pdf = doc.Formato == FormatoArquivo.Pdf;
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "sandbox; frame-ancestors 'self'";
        try
        {
            return File(storage.Abrir(doc.ChaveArquivo), pdf ? "application/pdf" :
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                pdf && visualizar ? null : doc.Avaliacao.Titulo, enableRangeProcessing: true);
        }
        catch (IOException) { return NotFound(); }
        catch (InvalidDataException) { return NotFound(); }
    }

    [HttpGet]
    public async Task<IActionResult> Estrutura(Guid id, CancellationToken ct)
    {
        var doc = await documentos.BuscarAsync(Professor, id, ct);
        if (doc?.ExtracaoJson == null || doc.ExclusaoSolicitada) return NotFound();
        return File(JsonSerializer.SerializeToUtf8Bytes(RevisaoDocumento.Ler(doc), RevisaoDocumento.Json), "application/json", "avaliacao.json");
    }

    [HttpPost, RequestSizeLimit(5_000_000), RequestFormLimits(ValueLengthLimit = 2_000_000)]
    public async Task<IActionResult> Salvar(Guid id, string versao, string? edicao, bool confirmar, bool adaptar, string? perfil)
    {
        var doc = await documentos.BuscarAsync(Professor, id);
        if (doc == null) return NotFound();
        try
        {
            if (adaptar && confirmar && !PerfisAvaliacao.Disponivel(perfil))
                throw new InvalidDataException("Selecione um perfil de adaptação para continuar.");
            var campos = JsonSerializer.Deserialize<EdicaoDocumento>(edicao ?? "", RevisaoDocumento.Json)
                ?? throw new InvalidDataException("Dados inválidos.");
            await documentos.SalvarAsync(doc, versao, campos, confirmar);
            if (confirmar && PerfisAvaliacao.Disponivel(perfil))
                return RedirectToAction("NovaAdaptacao", "Painel", new { documentoId = id, perfil });
            TempData["Mensagem"] = confirmar ? "Extração confirmada. Você já pode gerar uma adaptação." : "Revisão salva.";
        }
        catch (JsonException) { TempData["Erro"] = "Confira os campos e envie novamente."; }
        catch (InvalidDataException ex) { TempData["Erro"] = ex.Message; }
        catch (DbUpdateConcurrencyException) { TempData["Erro"] = "Há uma versão mais recente. Confira as alterações antes de editar novamente."; }
        return RedirectToAction(nameof(Revisar), new { id, adaptar = adaptar ? (bool?)true : null, perfil });
    }

    [HttpPost]
    public async Task<IActionResult> Reprocessar(Guid id, bool adaptar)
    {
        var doc = await documentos.BuscarAsync(Professor, id);
        if (doc == null) return NotFound();
        try { await documentos.ProcessarAsync(doc); }
        catch (InvalidDataException ex) { TempData["Erro"] = ex.Message; }
        catch (DbUpdateConcurrencyException) { TempData["Erro"] = "O estado do documento foi atualizado em outra sessão."; }
        return RedirectToAction(nameof(Revisar), new { id, adaptar = adaptar ? (bool?)true : null });
    }

    [HttpPost]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var doc = await documentos.BuscarAsync(Professor, id);
        if (doc == null) return NotFound();
        try
        {
            await documentos.ExcluirAsync(doc);
            TempData["Mensagem"] = "Documento excluído.";
            return RedirectToAction("Avaliacoes", "Painel");
        }
        catch (InvalidDataException ex) { TempData["Erro"] = ex.Message; }
        catch (IOException) { TempData["Erro"] = "Arquivo em uso ou indisponível. Repita a exclusão."; }
        catch (DbUpdateConcurrencyException) { TempData["Erro"] = "O documento foi atualizado. Confira o estado atual."; }
        return RedirectToAction(nameof(Revisar), new { id });
    }
}
