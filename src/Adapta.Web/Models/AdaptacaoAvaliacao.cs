using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.Models;

public sealed class AdaptacaoAvaliacao
{
    public Guid Id { get; set; }
    public Guid AvaliacaoId { get; set; }
    public Guid? PerfilAdaptacaoId { get; set; }
    public EstadoAdaptacao Estado { get; set; } = EstadoAdaptacao.Pendente;
    [MaxLength(160)]
    public required string ModeloIa { get; set; }
    [MaxLength(80)]
    public required string VersaoPrompt { get; set; }
    [MaxLength(80)]
    public required string RevisaoBase { get; set; }
    public string? Resultado { get; set; }
    public DateTimeOffset CriadaEm { get; set; } = DateTimeOffset.UtcNow;
}

public enum EstadoAdaptacao { Pendente, EmRevisao, Aprovada, Falhou }
