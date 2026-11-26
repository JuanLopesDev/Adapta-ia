using Adapta.Web.Data;
using Adapta.Web.Models;
using Adapta.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Adapta.Web.Services;

public sealed class PortalDocenteService(AdaptaDbContext db)
{
    private IQueryable<Avaliacao> Avaliacoes(string professorId) =>
        db.Avaliacoes.AsNoTracking().Where(a => a.ProfessorId == professorId);

    public async Task<PainelViewModel> PainelAsync(string professorId, CancellationToken ct)
    {
        var avaliacoes = Avaliacoes(professorId);
        var adaptacoes = db.Adaptacoes.Where(a => avaliacoes.Any(v => v.Id == a.AvaliacaoId));
        var total = await avaliacoes.CountAsync(ct);
        var realizadas = await adaptacoes.CountAsync(a => a.Estado == EstadoAdaptacao.EmRevisao || a.Estado == EstadoAdaptacao.Aprovada, ct);
        var emRevisao = await adaptacoes.CountAsync(a => a.Estado == EstadoAdaptacao.EmRevisao, ct);
        var aprovadas = await adaptacoes.Where(a => a.Estado == EstadoAdaptacao.Aprovada)
            .Select(a => a.AvaliacaoId).Distinct().CountAsync(ct);

        Indicador[] indicadores = [
            new("Total de avaliações", total, "Files", "azul"),
            new("Adaptações realizadas", realizadas, "WandSparkles", "petroleo"),
            new("Aguardando revisão", emRevisao, "Clock", "neutro"),
            new("Avaliações aprovadas", aprovadas, "CircleCheck", "salvia")
        ];
        return new(indicadores, await Atividades(professorId, incluirHistorico: false).Take(5).ToListAsync(ct));
    }

    public Task<ListaPortal<ItemAvaliacao>> ListarAsync(string professorId, int pagina, CancellationToken ct)
    {
        var itens = from a in Avaliacoes(professorId)
                    join doc in db.Documentos on a.Id equals doc.AvaliacaoId into documentos
                    from d in documentos.DefaultIfEmpty()
                    orderby a.CriadaEm descending, a.Id
                    select new ItemAvaliacao(a.Titulo, a.CriadaEm, (FormatoArquivo?)d.Formato,
                        db.Adaptacoes.Count(t => t.AvaliacaoId == a.Id), (Guid?)d.Id,
                        (EstadoProcessamento?)d.Processamento, d.Revisao);
        return PaginarAsync(itens, pagina, ct);
    }

    public Task<ListaPortal<Atividade>> HistoricoAsync(string professorId, int pagina, CancellationToken ct) =>
        PaginarAsync(Atividades(professorId, incluirHistorico: true), pagina, ct);

    private IQueryable<Atividade> Atividades(string professorId, bool incluirHistorico)
    {
        var avaliacoes = Avaliacoes(professorId);
        var adaptacoes = from a in avaliacoes join t in db.Adaptacoes on a.Id equals t.AvaliacaoId
                         select new { a.Titulo, Adaptacao = t };
        var registros = avaliacoes.Select(a => new { a.Titulo, Data = a.CriadaEm, a.Id, Tipo = TipoAtividade.Avaliacao, Estado = (EstadoAdaptacao?)null })
            .Concat(adaptacoes.Select(t => new { t.Titulo, Data = t.Adaptacao.CriadaEm, t.Adaptacao.Id, Tipo = TipoAtividade.Adaptacao, Estado = (EstadoAdaptacao?)null }));
        if (incluirHistorico)
        {
            var eventos = from t in adaptacoes join h in db.HistoricoAdaptacoes on t.Adaptacao.Id equals h.AdaptacaoId
                          select new { t.Titulo, Data = h.RegistradoEm, h.Id, Tipo = TipoAtividade.MudancaEstado, Estado = (EstadoAdaptacao?)h.Estado };
            registros = registros.Concat(eventos);
        }
        return registros.OrderByDescending(t => t.Data).ThenBy(t => t.Tipo).ThenBy(t => t.Id)
            .Select(t => new Atividade(t.Titulo, t.Data, t.Tipo, t.Estado));
    }

    private static async Task<ListaPortal<T>> PaginarAsync<T>(IQueryable<T> consulta, int pagina, CancellationToken ct)
    {
        const int tamanho = 20;
        pagina = Math.Clamp(pagina, 1, int.MaxValue / tamanho);
        var itens = await consulta.Skip((pagina - 1) * tamanho).Take(tamanho + 1).ToListAsync(ct);
        var temProxima = itens.Count > tamanho;
        if (temProxima) itens.RemoveAt(tamanho);
        return new(itens, new(pagina, temProxima));
    }
}
