using Adapta.Web.DTOs.Documentos;
using Adapta.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Adapta.Web.Services;

public static class FolhaPdf
{
    public static byte[] Gerar(ResultadoAdaptacao resultado)
    {
        var folha = resultado.Folha;
        var dados = folha.Conteudo;
        var extras = dados.Blocos.Where(b => !b.VisualDePagina && b.Tipo != "Texto").ToDictionary(b => b.Ordem);
        var associados = dados.Questoes.SelectMany(q => q.Blocos.Concat(q.ElementosVisuais)).ToHashSet();

        return Document.Create(doc => doc.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(42);
            p.DefaultTextStyle(x => x.FontSize(folha.LeituraAmpla ? 14 : 11).LineHeight(folha.LeituraAmpla ? 1.55f : 1.25f));
            p.Content().Column(col =>
            {
                col.Spacing(folha.LeituraAmpla ? 9 : 5);
                col.Item().Text(Path.GetFileNameWithoutExtension(resultado.Nome)).FontSize(19).Bold();
                foreach (var item in dados.Instrucoes.Select(i => (i.Bloco, i.Texto, Instrucao:true))
                    .Concat(dados.Apoios.Select(a => (a.Bloco, a.Texto, Instrucao:false)))
                    .OrderBy(a => a.Bloco == 0 ? int.MaxValue : a.Bloco))
                    col.Item().Text(item.Instrucao && folha.GuiaInstrucoes ? "[  ]  " + item.Texto : item.Texto);
                foreach (var bloco in extras.Values.Where(b => !associados.Contains(b.Ordem))) Elemento(col, bloco);
                foreach (var q in dados.Questoes)
                {
                    col.Item().PaddingTop(7).Text("Questão " + q.Numero).SemiBold().FontColor("#24545A");
                    foreach (var a in q.Apoios) col.Item().Text(a.Texto);
                    col.Item().Text(q.Enunciado);
                    foreach (var ordem in q.Blocos.Concat(q.ElementosVisuais).Distinct().Order())
                        if (extras.TryGetValue(ordem, out var b)) Elemento(col, b);
                    foreach (var a in q.Alternativas) col.Item().Text(a.Rotulo + ") " + a.Texto);
                    if (folha.EspacoResposta)
                        col.Item().MinHeight(55).BorderBottom(0.6f).BorderColor("#9DAFAE").Text("Resposta").FontSize(10);
                }
            });
            p.Footer().AlignRight().Text(t => { t.Span("Página "); t.CurrentPageNumber(); });
        })).GeneratePdf();
    }

    private static void Elemento(ColumnDescriptor col, BlocoOriginal bloco)
    {
        if (bloco.Celulas is { Length: > 0 } linhas)
        {
            var largura = linhas.Max(l => l.Length);
            if (largura is > 0 and <= 10)
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { for (int i = 0; i < largura; i++) c.RelativeColumn(); });
                    foreach (var linha in linhas)
                        for (int i = 0; i < largura; i++)
                            t.Cell().Border(0.4f).BorderColor("#A9BAB7").Padding(3).Text(i < linha.Length ? linha[i] : "");
                });
            }
            else foreach (var linha in linhas) col.Item().Text(string.Join(" | ", linha));
            return;
        }
        if (bloco.Tipo != "Espaço" && bloco.Texto.Length > 0) col.Item().Text(bloco.Texto);
    }
}
