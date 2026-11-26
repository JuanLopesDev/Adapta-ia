using System.Text;
using Adapta.Web.DTOs.Documentos;
using Adapta.Web.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Adapta.Web.Services.Documentos;

public sealed class PdfProcessor : IDocumentProcessingService
{
    public FormatoArquivo Formato => FormatoArquivo.Pdf;

    public void Validar(Stream arquivo)
    {
        Span<byte> inicio = stackalloc byte[5];
        arquivo.ReadExactly(inicio);
        if (!inicio.SequenceEqual("%PDF-"u8)) throw new InvalidDataException();
        arquivo.Position = Math.Max(0, arquivo.Length - 1024);
        var fim = new byte[arquivo.Length - arquivo.Position];
        arquivo.ReadExactly(fim);
        if (!Encoding.ASCII.GetString(fim).Contains("%%EOF")) throw new InvalidDataException();
        arquivo.Position = 0;
        using var pdf = PdfDocument.Open(arquivo, ParsingOptions.LenientParsingOff);
        if (pdf.IsEncrypted || pdf.NumberOfPages is < 1 or > 150) throw new InvalidDataException();
    }

    public ExtractedAssessment Extrair(Stream arquivo)
    {
        using var pdf = PdfDocument.Open(arquivo);
        var dados = new Estruturador();
        foreach (var pagina in pdf.GetPages())
        {
            var origem = $"Página {pagina.Number}";
            if (pagina.Letters.Count > 60000) throw new InvalidDataException();
            var texto = ContentOrderTextExtractor.GetText(pagina);
            foreach (var linha in texto.Replace("\r\n", "\n").Split('\n'))
                dados.Adicionar(origem, "Texto", linha);
            if (string.IsNullOrWhiteSpace(texto)) dados.Avisar(origem, "Página sem texto selecionável. Consulte a imagem original e transcreva as questões.");
            int imagemNumero = 0;
            foreach (var imagem in pagina.GetImages())
                dados.Adicionar(origem, "Visual de página", $"Imagem no PDF · limites {imagem.BoundingBox}",
                    $"pdf:pagina/{pagina.Number}/imagem/{++imagemNumero}");
            if (pagina.Paths.Count != 0)
                dados.Adicionar(origem, "Visual de página", "Elementos vetoriais", $"pdf:pagina/{pagina.Number}/desenhos");
            if (pagina.NumberOfImages != 0 || pagina.Paths.Count != 0)
                dados.Avisar(origem, ExtractionWarning.MaterialVisualPdf);
        }
        dados.Avisar("PDF", "Confira colunas, fórmulas e alinhamentos no original. A ordem de leitura do texto no PDF é estimada.");
        return dados.Concluir();
    }
}
