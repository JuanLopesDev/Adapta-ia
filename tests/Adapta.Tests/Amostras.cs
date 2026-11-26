using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Adapta.Tests;

internal static class Amostras
{
    public static byte[] Docx(int paragrafosExtras = 0)
    {
        using var bytes = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(bytes, WordprocessingDocumentType.Document))
        {
            var part = doc.AddMainDocumentPart();
            var corpo = new W.Body();
            part.Document = new W.Document(corpo);
            foreach (var linha in new[] { "Instruções: leia com atenção.", "Texto de apoio: A água muda de estado.",
                "Questão 01: Marque uma opção.", "A) Líquido", "B) Sólido", "2. Explique o processo." })
                corpo.Append(new W.Paragraph(new W.Run(new W.Text(linha))));
            corpo.Append(new W.Table(new W.TableRow(new W.TableCell(new W.Paragraph(new W.Run(new W.Text("Temperatura")))))));
            corpo.Append(new W.Paragraph(new DocumentFormat.OpenXml.Math.OfficeMath(new DocumentFormat.OpenXml.Math.Run(new DocumentFormat.OpenXml.Math.Text("x²")))));
            corpo.Append(new W.Paragraph(new W.Run(new W.Text("________"))));
            for (int i = 0; i < paragrafosExtras; i++)
                corpo.Append(new W.Paragraph(new W.Run(new W.Text("Linha de leitura."))));
        }
        return bytes.ToArray();
    }

    public static byte[] Pdf(params string[] linhas)
    {
        string operacoes = "BT /F1 12 Tf 50 750 Td 18 TL\n" +
            string.Join("\n", linhas.Select(s => "(" + s.Replace("(", "\\(").Replace(")", "\\)") + ") Tj T*")) + "\nET\n";
        string[] objetos = ["<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", $"<< /Length {operacoes.Length} >>\nstream\n{operacoes}endstream"];
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        foreach (var obj in objetos)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{offsets.Count} 0 obj\n{obj}\nendobj\n");
        }
        var xref = pdf.Length;
        pdf.Append($"xref\n0 {objetos.Length+1}\n0000000000 65535 f \n");
        foreach (var off in offsets) pdf.Append($"{off:D10} 00000 n \n");
        pdf.Append($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
