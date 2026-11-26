using Adapta.Web.DTOs.Documentos;
using Adapta.Web.Models;

namespace Adapta.Web.ViewModels;

public record DocumentoViewModel(Documento Documento, ExtractedAssessment? Conteudo, bool Adaptar = false, string? Perfil = null)
{
    public static string Situacao(EstadoProcessamento estado, EstadoRevisao? revisao) => revisao switch {
        EstadoRevisao.Revisado => "Revisado",
        EstadoRevisao.AguardandoRevisao when estado == EstadoProcessamento.ConcluidoComAvisos => "Aguardando revisão · com avisos",
        EstadoRevisao.AguardandoRevisao => "Aguardando revisão",
        _ => estado switch {
            EstadoProcessamento.Processando => "Processando", EstadoProcessamento.Falhou => "Falhou",
            EstadoProcessamento.Concluido => "Concluído", EstadoProcessamento.ConcluidoComAvisos => "Concluído com avisos", _ => "Pendente" }
    };
}
