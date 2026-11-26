using System.Globalization;
using Adapta.Web.Models;

namespace Adapta.Web.ViewModels;

public record Indicador(string Nome, int Valor, string Icone, string Cor);
public record EstadoVazio(string Icone, string Titulo, string Descricao);
public record ItemAvaliacao(string Titulo, DateTimeOffset Data, FormatoArquivo? Formato, int Adaptacoes,
    Guid? DocumentoId, EstadoProcessamento? Processamento, EstadoRevisao? Revisao);
public enum TipoAtividade { Avaliacao, Adaptacao, MudancaEstado }
public record Atividade(string Titulo, DateTimeOffset Data, TipoAtividade Tipo, EstadoAdaptacao? Estado)
{
    public string Descricao => Tipo switch
    {
        TipoAtividade.Avaliacao => "Avaliação cadastrada",
        TipoAtividade.Adaptacao => "Adaptação cadastrada",
        _ => Estado switch
        {
            EstadoAdaptacao.Aprovada => "Adaptação aprovada",
            EstadoAdaptacao.EmRevisao => "Adaptação em revisão",
            EstadoAdaptacao.Falhou => "Falha na adaptação",
            _ => "Adaptação pendente"
        }
    };
}
public record Paginacao(int Atual, bool TemProxima);
public record ListaPortal<T>(IReadOnlyList<T> Itens, Paginacao Pagina);
public record PainelViewModel(IReadOnlyList<Indicador> Indicadores, IReadOnlyList<Atividade> Recentes);
public static class DataPortal
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    public static string Mostrar(DateTimeOffset data, string formato = "dd/MM/yyyy") =>
        TimeZoneInfo.ConvertTime(data, Fuso).ToString(formato, CultureInfo.GetCultureInfo("pt-BR"));
}
