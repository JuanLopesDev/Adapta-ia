using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Adapta.Web.Services.Documentos;

// Numerações simples com início e máscara explícitos. Listas multinível e estilos herdados exigem conferência.
internal sealed class NumeracaoWord(MainDocumentPart parte)
{
    private readonly Dictionary<int,int> contadores = [];
    public string? Prefixo(W.NumberingProperties propriedades)
    {
        var id = propriedades.NumberingId?.Val?.Value;
        if (id is null || propriedades.NumberingLevelReference?.Val?.Value is not (0 or null)) return null;
        var definicoes = parte.NumberingDefinitionsPart?.Numbering;
        var instancia = definicoes?.Elements<W.NumberingInstance>().FirstOrDefault(n => n.NumberID?.Value == id);
        var abstrata = definicoes?.Elements<W.AbstractNum>().FirstOrDefault(n => n.AbstractNumberId?.Value == instancia?.AbstractNumId?.Val?.Value);
        var ajuste = instancia?.Elements<W.LevelOverride>().FirstOrDefault(l => l.LevelIndex?.Value == 0);
        var nivel = ajuste?.GetFirstChild<W.Level>() ?? abstrata?.Elements<W.Level>().FirstOrDefault(l => l.LevelIndex?.Value == 0);
        var inicio = ajuste?.StartOverrideNumberingValue?.Val?.Value ?? nivel?.StartNumberingValue?.Val?.Value;
        var mascara = nivel?.LevelText?.Val?.Value;
        if (inicio is null || mascara == null || !mascara.Contains("%1") || mascara.Any(c => c is >= '2' and <= '9')) return null;
        var numero = contadores.TryGetValue(id.Value, out var anterior) ? anterior + 1 : inicio.Value;
        contadores[id.Value] = numero;
        var texto = nivel?.NumberingFormat?.Val?.InnerText switch
        {
            "decimal" => numero.ToString(CultureInfo.InvariantCulture),
            "upperLetter" when numero is >= 1 and <= 26 => ((char)('A' + numero - 1)).ToString(),
            "lowerLetter" when numero is >= 1 and <= 26 => ((char)('a' + numero - 1)).ToString(),
            _ => null
        };
        return texto == null ? null : mascara.Replace("%1", texto) + " ";
    }
}
