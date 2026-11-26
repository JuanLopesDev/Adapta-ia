using System.Data;
using System.Text.Json;
using Adapta.Web.Data;
using Adapta.Web.Models;
using Adapta.Web.Services.Documentos;
using Adapta.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Adapta.Web.Services;

public sealed class AdaptacaoService(AdaptaDbContext db)
{
    private const string Metodo = "Apresentação local v1";
    public async Task PreencherAsync(string professor, NovaAdaptacaoViewModel form, CancellationToken ct)
    {
        form.Documentos = await db.Documentos.Where(d => d.Avaliacao.ProfessorId == professor &&
                !d.ExclusaoSolicitada && d.Revisao == EstadoRevisao.Revisado)
            .OrderByDescending(d => d.RevisadoEm).Select(d => new DocumentoElegivel(d.Id, d.Avaliacao.Titulo)).ToListAsync(ct);
        form.Versoes = await (from a in db.Adaptacoes join v in db.Avaliacoes on a.AvaliacaoId equals v.Id
            where v.ProfessorId == professor && a.ModeloIa == Metodo
            orderby a.CriadaEm descending select new AdaptacaoSalva(a.Id, v.Titulo, a.CriadaEm)).ToListAsync(ct);
    }

    public async Task<Guid?> GerarAsync(string professor, NovaAdaptacaoViewModel form, CancellationToken ct)
    {
        if (!PerfisAvaliacao.Disponivel(form.Perfil))
            throw new InvalidDataException("Selecione o perfil de adaptação.");
        if (form.Perfil == PerfisAvaliacao.Dislexia && form.NivelSuporte.HasValue)
            throw new InvalidDataException("O nível de suporte se aplica ao perfil TEA.");
        if (form.SolicitacaoId == Guid.Empty || form.NivelSuporte is < 1 or > 3 ||
            !(form.LeituraAmpla || form.GuiaInstrucoes || form.EspacoResposta || form.ComandosDiretos))
            throw new InvalidDataException("Selecione pelo menos um apoio e confira as opções.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var doc = await db.Documentos.FromSqlInterpolated($"SELECT * FROM [Documentos] WITH (UPDLOCK) WHERE [Id] = {form.DocumentoId}").AsNoTracking().SingleOrDefaultAsync(d => d.Id == form.DocumentoId &&
            d.Avaliacao.ProfessorId == professor && !d.ExclusaoSolicitada, ct);
        if (doc == null) return null;
        if (doc.Revisao != EstadoRevisao.Revisado || doc.ExtracaoJson == null)
            throw new InvalidDataException("Confirme a revisão da extração antes de gerar a adaptação.");
        var anterior = await db.Adaptacoes.FindAsync([form.SolicitacaoId], ct);
        if (anterior != null) return anterior.AvaliacaoId == doc.AvaliacaoId && anterior.ModeloIa == Metodo ? anterior.Id : null;
        var conteudo = RevisaoDocumento.Ler(doc);
        var ajustes = form.ComandosDiretos ? ComandosDeProva.Aplicar(conteudo) : [];
        if (!form.LeituraAmpla && !form.EspacoResposta && ajustes.Count == 0 &&
            !(form.GuiaInstrucoes && conteudo.Instrucoes.Count > 0))
            throw new InvalidDataException("Os apoios selecionados não alteram esta avaliação. Escolha outro apoio.");
        var folha = new FolhaAdaptada(doc.Id, form.NivelSuporte, form.LeituraAmpla, form.GuiaInstrucoes,
            form.EspacoResposta, conteudo) { Perfil = form.Perfil!, ComandosDiretos = form.ComandosDiretos, AjustesDeComando = ajustes };
        var versao = new AdaptacaoAvaliacao { Id = form.SolicitacaoId, AvaliacaoId = doc.AvaliacaoId,
            ModeloIa = Metodo, VersaoPrompt = "apresentacao-2", RevisaoBase = "2026.11.4",
            Estado = EstadoAdaptacao.EmRevisao, Resultado = JsonSerializer.Serialize(folha, RevisaoDocumento.Json) };
        db.Adaptacoes.Add(versao);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return versao.Id;
    }

    public async Task<bool> AprovarAsync(string professor, Guid id, CancellationToken ct)
    {
        var versoes = db.Adaptacoes.Where(a => a.Id == id && a.ModeloIa == Metodo &&
            db.Avaliacoes.Any(v => v.Id == a.AvaliacaoId && v.ProfessorId == professor));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var alteradas = await versoes.Where(a => a.Estado == EstadoAdaptacao.EmRevisao)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Estado, EstadoAdaptacao.Aprovada), ct);
        if (alteradas == 0) return await versoes.AnyAsync(a => a.Estado == EstadoAdaptacao.Aprovada, ct);
        db.HistoricoAdaptacoes.Add(new HistoricoAdaptacao { AdaptacaoId = id, Estado = EstadoAdaptacao.Aprovada });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<ResultadoAdaptacao?> ObterAsync(string professor, Guid id, CancellationToken ct)
    {
        var linha = await (from a in db.Adaptacoes join v in db.Avaliacoes on a.AvaliacaoId equals v.Id
            where a.Id == id && v.ProfessorId == professor && a.ModeloIa == Metodo
            join d in db.Documentos on v.Id equals d.AvaliacaoId
            select new { v.Titulo, a.CriadaEm, a.Estado, a.Resultado, d.ExtracaoJson, d.Formato }).SingleOrDefaultAsync(ct);
        if (linha?.Resultado == null || linha.ExtracaoJson == null) return null;
        var folha = JsonSerializer.Deserialize<FolhaAdaptada>(linha.Resultado, RevisaoDocumento.Json)!;
        var original = JsonSerializer.Deserialize<Adapta.Web.DTOs.Documentos.ExtractedAssessment>(linha.ExtracaoJson, RevisaoDocumento.Json)!;
        return new(id, linha.Titulo, linha.CriadaEm, linha.Estado, folha, original, linha.Formato);
    }
}
