using Adapta.Web.Services.Documentos;

namespace Adapta.Tests;

public class ExtracaoTests
{
    [Fact]
    public void DocxPreservaOrdemApoioAlternativasTabelaEFormula()
    {
        var p = new DocxProcessor();
        var bytes = Amostras.Docx();
        p.Validar(new MemoryStream(bytes));
        var a = p.Extrair(new MemoryStream(bytes));
        Assert.Equal(["01", "2"], a.Questoes.Select(q => q.Numero));
        Assert.Equal(["Líquido", "Sólido"], a.Questoes[0].Alternativas.Select(o => o.Texto));
        Assert.Equal("Múltipla escolha", a.Questoes[0].Tipo);
        Assert.Contains("água", Assert.Single(a.Apoios).Texto);
        Assert.Single(a.Instrucoes);
        Assert.Contains(a.Blocos, b => b.Tipo == "Espaço de resposta");
        Assert.Equal("Temperatura", Assert.Single(a.Blocos, b => b.Tipo == "Tabela").Celulas![0][0]);
        Assert.Contains(a.Avisos, w => w.Mensagem.Contains("Fórmula"));
        Assert.Contains(a.Blocos, b => b.Texto.Contains("____"));
        Assert.Equal(Enumerable.Range(1, a.Blocos.Count), a.Blocos.Select(b => b.Ordem));
    }

    [Fact]
    public void PdfPreservaLinhasNumeracaoEAlternativas()
    {
        var bytes = Amostras.Pdf("1. Selecione uma cor.", "a) Azul", "b) Verde", "2. Explique sua escolha.");
        var p = new PdfProcessor();
        p.Validar(new MemoryStream(bytes));
        var a = p.Extrair(new MemoryStream(bytes));
        Assert.Equal(2, a.Questoes.Count);
        Assert.Equal(["Azul", "Verde"], a.Questoes[0].Alternativas.Select(o => o.Texto));
        Assert.Equal("Discursiva", a.Questoes[1].Tipo);
    }

    [Fact]
    public void PdfSemQuestoesPedeRevisao()
    {
        var a = new PdfProcessor().Extrair(new MemoryStream(Amostras.Pdf()));
        Assert.Empty(a.Questoes);
        Assert.Contains(a.Avisos, w => w.Mensagem.Contains("sem texto"));
    }

    [Theory]
    [InlineData("1) Relacione as colunas", "Associação de colunas")]
    [InlineData("1) Indique verdadeiro ou falso", "Verdadeiro ou falso")]
    [InlineData("1) Complete a lacuna", "Preenchimento de lacunas")]
    [InlineData("1) Observe a figura", "Questão visual")]
    public void ReconheceEstruturas(string linha, string tipo)
    {
        var e = new Estruturador();
        e.Adicionar("Bloco 1", "Texto", linha);
        Assert.Equal(tipo, Assert.Single(e.Concluir().Questoes).Tipo);
    }

    [Theory]
    [InlineData("../arquivo.pdf")]
    [InlineData("C:\\a.docx")]
    [InlineData("prova\u202Efdp.pdf")]
    public void RecusaNomeInvalido(string nome) => Assert.Throws<InvalidDataException>(() => DocumentStorage.NomeSeguro(nome));
}
