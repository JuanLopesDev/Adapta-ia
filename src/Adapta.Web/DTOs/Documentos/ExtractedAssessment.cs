namespace Adapta.Web.DTOs.Documentos;

public sealed class ExtractedAssessment
{
    public int VersaoEsquema { get; init; } = 1;
    public List<BlocoOriginal> Blocos { get; init; } = [];
    public List<AssessmentInstruction> Instrucoes { get; init; } = [];
    public List<SupportingText> Apoios { get; init; } = [];
    public List<ExtractedQuestion> Questoes { get; set; } = [];
    public List<ExtractionWarning> Avisos { get; init; } = [];

    public List<ExtractionWarning> AvisosComplementares()
    {
        var paginasSinalizadas = Blocos.Where(b => b.VisualDePagina).Select(b => b.Origem).ToHashSet();
        return Avisos.Where(a => a.Mensagem != ExtractionWarning.MaterialVisualPdf || !paginasSinalizadas.Contains(a.Origem)).ToList();
    }
}

public record BlocoOriginal(int Ordem, string Origem, string Tipo, string Texto,
    string? Referencia = null, string[][]? Celulas = null)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool VisualDePagina => Tipo == "Visual de página";
}
public record AssessmentInstruction(int Bloco, string Texto);
public record SupportingText(int Bloco, string Texto);
public record ExtractionWarning(string Origem, string Mensagem)
{
    public const string MaterialVisualPdf = "Há imagens ou desenhos vetoriais. Confira gráficos, tabelas, fórmulas e sua associação às questões no original.";
}
public record ExtractedAlternative(string Rotulo, string Texto);
public sealed class ExtractedQuestion
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int Ordem { get; set; }
    public string Numero { get; set; } = "";
    public string Tipo { get; set; } = "Indeterminada";
    public string Enunciado { get; set; } = "";
    public List<ExtractedAlternative> Alternativas { get; set; } = [];
    public List<int> Blocos { get; init; } = [];
    public List<SupportingText> Apoios { get; init; } = [];
    public List<int> ElementosVisuais { get; init; } = [];
    public string Confianca { get; set; } = "Conferir";
}

public enum NivelSuporteTea { Nivel1 = 1, Nivel2, Nivel3 }
public record EntradaFuturaAdaptacao(ExtractedAssessment Avaliacao, NivelSuporteTea NivelSuporte);
