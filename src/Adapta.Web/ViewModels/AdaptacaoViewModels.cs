using System.ComponentModel.DataAnnotations;
using Adapta.Web.DTOs.Documentos;

namespace Adapta.Web.ViewModels;

public sealed class NovaAdaptacaoViewModel
{
    [Required(ErrorMessage = "Selecione o perfil de adaptação.")]
    [RegularExpression("^(TEA|Dislexia)$", ErrorMessage = "Escolha um perfil disponível.")]
    public string? Perfil { get; set; }
    public Guid DocumentoId { get; set; }
    public Guid SolicitacaoId { get; set; } = Guid.NewGuid();
    [Range(1, 3, ErrorMessage = "Selecione um nível entre 1 e 3.")]
    public int? NivelSuporte { get; set; }
    public bool LeituraAmpla { get; set; } = true;
    public bool ComandosDiretos { get; set; }
    public bool GuiaInstrucoes { get; set; }
    public bool EspacoResposta { get; set; }
    public IReadOnlyList<DocumentoElegivel> Documentos { get; set; } = [];
    public IReadOnlyList<AdaptacaoSalva> Versoes { get; set; } = [];
}
public record DocumentoElegivel(Guid Id, string Nome);
public record AdaptacaoSalva(Guid Id, string Nome, DateTimeOffset Data);
public record FolhaAdaptada(Guid DocumentoId, int? NivelSuporte, bool LeituraAmpla, bool GuiaInstrucoes,
    bool EspacoResposta, ExtractedAssessment Conteudo)
{
    public string Perfil { get; init; } = Models.PerfisAvaliacao.Tea;
    public bool ComandosDiretos { get; init; }
    public List<AjusteDeComando> AjustesDeComando { get; init; } = [];
    public int ComandosAlterados => AjustesDeComando.Count;
}
public record AjusteDeComando(string Local, string Antes, string Depois);
public record ResultadoAdaptacao(Guid Id, string Nome, DateTimeOffset Data, Models.EstadoAdaptacao Estado, FolhaAdaptada Folha, ExtractedAssessment Original, Models.FormatoArquivo Formato);
