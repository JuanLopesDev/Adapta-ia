using System.IO.Compression;
using System.Text;
using Adapta.Web.DTOs.Documentos;
using Adapta.Web.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Adapta.Web.Services.Documentos;

public sealed class DocxProcessor : IDocumentProcessingService
{
    public FormatoArquivo Formato => FormatoArquivo.Docx;
    private const long LimiteExpandido = 40 * 1024 * 1024;
    private static WordprocessingDocument Abrir(Stream s) => WordprocessingDocument.Open(s, false,
        new OpenSettings { MaxCharactersInPart = 8_000_000 });

    public void Validar(Stream arquivo)
    {
        using (var zip = new ZipArchive(arquivo, ZipArchiveMode.Read, true))
        {
            if (zip.Entries.Count > 1500 || zip.GetEntry("[Content_Types].xml") is null) throw new InvalidDataException();
            long total = 0;
            var buffer = new byte[65536];
            foreach (var parte in zip.Entries)
            {
                if (parte.Length > LimiteExpandido - total || parte.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException();
                using var stream = parte.Open();
                long bytes = 0;
                int n;
                while ((n = stream.Read(buffer)) > 0)
                {
                    bytes += n;
                    total += n;
                    if (total > LimiteExpandido) throw new InvalidDataException();
                }
                if (bytes != parte.Length) throw new InvalidDataException();
            }
        }
        arquivo.Position = 0;
        using var doc = Abrir(arquivo);
        if (doc.DocumentType != WordprocessingDocumentType.Document || doc.MainDocumentPart?.Document?.Body is null)
            throw new InvalidDataException();
    }

    public ExtractedAssessment Extrair(Stream arquivo)
    {
        using var doc = Abrir(arquivo);
        var parte = doc.MainDocumentPart!;
        var numeracao = new NumeracaoWord(parte);
        var dados = new Estruturador();
        var bloco = 0;
        foreach (var elemento in parte.Document!.Body!.ChildElements)
        {
            var origem = $"Bloco {++bloco}";
            if (elemento is W.SectionProperties) continue;
            if (elemento is W.Table tabela)
            {
                var linhas = tabela.Elements<W.TableRow>().Select(r => r.Elements<W.TableCell>()
                    .Select(c => string.Join("\n", c.Elements<W.Paragraph>().Select(p => p.InnerText))).ToArray()).ToArray();
                dados.Adicionar(origem, "Tabela", string.Join("\n", linhas.Select(c => string.Join(" | ", c))), celulas: linhas);
                dados.Avisar(origem, "Confira células mescladas, listas, fórmulas e elementos visuais da tabela no original.");
                foreach (var visual in tabela.Descendants().Where(Especial)) RegistrarEspecial(visual, origem, dados);
            }
            else if (elemento is W.Paragraph paragrafo)
            {
                var linha = new StringBuilder();
                if (paragrafo.ParagraphProperties?.NumberingProperties is { } lista)
                {
                    var prefixo = numeracao.Prefixo(lista);
                    if (prefixo == null) dados.Avisar(origem, "Lista automática complexa. Confira a numeração exibida no original.");
                    else linha.Append(prefixo);
                }
                void Emitir()
                {
                    if (linha.Length > 0) { dados.Adicionar(origem, "Texto", linha.ToString()); linha.Clear(); }
                }
                void Ler(OpenXmlElement item)
                {
                    if (Especial(item)) { Emitir(); RegistrarEspecial(item, origem, dados); return; }
                    if (item is W.Text t) { linha.Append(t.Text); return; }
                    if (item is W.TabChar) { linha.Append('\t'); return; }
                    if (item is W.Break or W.CarriageReturn) { Emitir(); return; }
                    foreach (var filho in item.ChildElements) Ler(filho);
                }
                Ler(paragrafo);
                Emitir();
                if (string.IsNullOrWhiteSpace(paragrafo.InnerText)) dados.Adicionar(origem, "Espaço", "");
            }
            else
            {
                dados.Adicionar(origem, "Elemento do Word", elemento.InnerText, elemento.LocalName);
                dados.Avisar(origem, "Elemento fora do corpo textual convencional. Confira sua apresentação no original.");
            }
        }
        if (parte.HeaderParts.Any() || parte.FooterParts.Any() || parte.FootnotesPart != null || parte.EndnotesPart != null || parte.NumberingDefinitionsPart != null)
            dados.Avisar("Word", "O arquivo inclui cabeçalhos, rodapés, notas ou definições de listas. Confira esses elementos no original.");
        return dados.Concluir();
    }

    private static bool Especial(OpenXmlElement e) => e.LocalName is "drawing" or "pict" or "object" or "oMath" or "txbxContent" or "fldSimple" or "instrText" or "del";
    private static void RegistrarEspecial(OpenXmlElement e, string origem, Estruturador dados)
    {
        var tipo = e.LocalName switch { "oMath" => "Fórmula", "drawing" or "pict" => "Imagem", _ => "Elemento do Word" };
        var atributos = e.Descendants().Prepend(e).SelectMany(n => n.GetAttributes()).ToList();
        var descricoes = atributos.Where(a => a.LocalName is "descr" or "title").Select(a => a.Value);
        var refs = atributos.Where(a => a.NamespaceUri.EndsWith("/relationships", StringComparison.Ordinal)).Select(a => a.Value);
        var texto = string.Join(" ", descricoes.Append(e.InnerText).Where(t => !string.IsNullOrWhiteSpace(t)));
        dados.Adicionar(origem, tipo, texto, string.Join(", ", refs.Distinct()));
        dados.Avisar(origem, $"{tipo}: confira a representação e a posição no original.");
    }
}
