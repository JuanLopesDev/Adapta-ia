using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.Models;

public sealed class Documento
{
    public Guid Id { get; set; }
    public Guid AvaliacaoId { get; set; }
    [MaxLength(256)]
    public required string ChaveArquivo { get; set; }
    public FormatoArquivo Formato { get; set; }
    public Avaliacao Avaliacao { get; set; } = null!;
    public long Tamanho { get; set; }
    public EstadoProcessamento Processamento { get; set; }
    public EstadoRevisao? Revisao { get; set; }
    public DateTimeOffset? InicioProcessamento { get; set; }
    public DateTimeOffset? ProcessadoEm { get; set; }
    public DateTimeOffset? RevisadoEm { get; set; }
    public string? ExtracaoJson { get; set; }
    public string? RevisaoJson { get; set; }
    public bool ExclusaoSolicitada { get; set; }
    [Timestamp] public byte[] Versao { get; set; } = [];
}

public enum FormatoArquivo { Pdf, Docx }
public enum EstadoProcessamento { Pendente, Processando, Concluido, ConcluidoComAvisos, Falhou }
public enum EstadoRevisao { AguardandoRevisao, Revisado }
