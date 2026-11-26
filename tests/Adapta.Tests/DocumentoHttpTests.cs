using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Adapta.Web.DTOs.Documentos;
using Adapta.Web.Models;
using Adapta.Web.Services.Documentos;
using Microsoft.EntityFrameworkCore;
using static Adapta.Tests.PortalFixture;

namespace Adapta.Tests;

public sealed class DocumentoHttpTests(PortalFixture portal) : IClassFixture<PortalFixture>
{
    private static async Task<HttpResponseMessage> Upload(HttpClient cliente, string nome, byte[] bytes)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(await TokenAsync(cliente, "/Documentos/Enviar")), "__RequestVerificationToken");
        form.Add(new ByteArrayContent(bytes), "arquivo", nome);
        return await cliente.PostAsync("/Documentos/Enviar", form);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FluxoCompletoComDonoOriginalRevisaoEConcorrencia(bool pdf)
    {
        using var dono = await portal.LoginAsync("100");
        using var outro = await portal.LoginAsync("200");
        var nome = pdf ? "Amostra.pdf" : "Amostra.docx";
        var bytes = pdf ? Amostras.Pdf("1. Escolha.", "A) Um", "B) Dois", "2. Explique.") : Amostras.Docx();
        using var envio = await Upload(dono, nome, bytes);
        Assert.Equal(HttpStatusCode.Redirect, envio.StatusCode);
        var tela = envio.Headers.Location!.OriginalString;
        var id = Guid.Parse(tela.Split('/')[^1]);
        string Rota(string acao) => "/Documentos/" + acao + "/" + id;
        Assert.Contains(nome, await dono.GetStringAsync(tela));
        Assert.Equal(bytes, await dono.GetByteArrayAsync(Rota("Original")));
        foreach (var acao in new[] {"Revisar", "Original", "Estrutura"})
            Assert.Equal(HttpStatusCode.NotFound, (await outro.GetAsync(Rota(acao))).StatusCode);
        foreach (var acao in new[] {"Salvar", "Excluir", "Reprocessar"})
            Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(outro, "/Documentos/Enviar", Rota(acao), new())).StatusCode);

        await using var db = portal.Contexto();
        var doc = await db.Documentos.AsNoTracking().Include(d => d.Avaliacao).SingleAsync(d => d.Id == id);
        Assert.Equal(nome, doc.Avaliacao.Titulo);
        Assert.Equal(bytes.Length, doc.Tamanho);
        Assert.True(Guid.TryParseExact(doc.ChaveArquivo, "N", out _));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(portal.Pasta, doc.ChaveArquivo)));
        Assert.NotEqual(HttpStatusCode.OK, (await dono.GetAsync("/" + doc.ChaveArquivo)).StatusCode);
        var conteudo = await dono.GetFromJsonAsync<ExtractedAssessment>(Rota("Estrutura"));
        Assert.Equal(2, conteudo!.Questoes.Count);
        var reversas = conteudo.Questoes.AsEnumerable().Reverse().Select((q, i) => new QuestaoEditada(q.Id, (i+1).ToString(),
            "Texto revisado " + i, [new("B", "Alternativa corrigida")])).ToList();
        var json = JsonSerializer.Serialize(new EdicaoDocumento(["Instrução revisada"], reversas));
        var campos = new Dictionary<string,string> { ["edicao"] = json, ["versao"] = Convert.ToBase64String(doc.Versao), ["confirmar"] = "false" };
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(dono, tela, Rota("Salvar"), campos)).StatusCode);
        var rascunho = await db.Documentos.AsNoTracking().SingleAsync(d => d.Id == id);
        Assert.Equal(EstadoRevisao.AguardandoRevisao, rascunho.Revisao);
        Assert.NotNull(rascunho.RevisaoJson);
        campos["confirmar"] = "true";
        await PostAsync(dono, tela, Rota("Salvar"), campos);
        Assert.Equal(EstadoRevisao.AguardandoRevisao, (await db.Documentos.AsNoTracking().SingleAsync(d => d.Id == id)).Revisao);
        campos["versao"] = Convert.ToBase64String(rascunho.Versao);
        await PostAsync(dono, tela, Rota("Salvar"), campos);
        var salvo = await db.Documentos.AsNoTracking().SingleAsync(d => d.Id == id);
        Assert.Equal(EstadoRevisao.Revisado, salvo.Revisao);
        Assert.Equal(doc.ExtracaoJson, salvo.ExtracaoJson);
        var revisada = await dono.GetFromJsonAsync<ExtractedAssessment>(Rota("Estrutura"));
        Assert.Equal(reversas[0].Id, revisada!.Questoes[0].Id);
        Assert.Equal("Alternativa corrigida", revisada.Questoes[0].Alternativas[0].Texto);
        Assert.Equal("Instrução revisada", revisada.Instrucoes[0].Texto);
        Assert.Equal(bytes, await dono.GetByteArrayAsync(Rota("Original")));
        await PostAsync(dono, tela, Rota("Excluir"), new());
        Assert.False(await db.Documentos.AnyAsync(d => d.Id == id));
        Assert.False(File.Exists(Path.Combine(portal.Pasta, doc.ChaveArquivo)));
    }

    [Theory]
    [InlineData("avaliacao.txt", 20)]
    [InlineData("corrompido.pdf", 100)]
    [InlineData("corrompido.docx", 100)]
    [InlineData("grande.pdf", DocumentStorage.Limite + 1)]
    public async Task UploadInvalidoNaoPersiste(string nome, int tamanho)
    {
        using var c = await portal.LoginAsync("100");
        using var resposta = await Upload(c, nome, new byte[tamanho]);
        Assert.True(resposta.StatusCode is HttpStatusCode.OK or HttpStatusCode.RequestEntityTooLarge);
        await using var db = portal.Contexto();
        Assert.False(await db.Documentos.AnyAsync(d => d.Avaliacao.Titulo == nome));
    }

    [Theory]
    [InlineData("TEA")]
    [InlineData("Dislexia")]
    public async Task AdaptaRevisaoDoDonoPersisteEAprovaUmaUnicaVez(string perfil)
    {
        using var dono = await portal.LoginAsync("100");
        using var outro = await portal.LoginAsync("200");
        using var envio = await Upload(dono, "Folha-local.pdf", Amostras.Pdf("1. Explique.", "A) Um", "B) Dois"));
        var tela = envio.Headers.Location!.OriginalString;
        var id = Guid.Parse(tela.Split('/')[^1]);
        var campos = new Dictionary<string, string> {
            ["Perfil"] = perfil, ["DocumentoId"] = id.ToString(), ["SolicitacaoId"] = Guid.NewGuid().ToString(),
            ["LeituraAmpla"] = "true", ["EspacoResposta"] = "true", ["ComandosDiretos"] = "true"
        };
        if (perfil == "TEA") campos["NivelSuporte"] = "3";
        const string preparacao = "/Painel/NovaAdaptacao";
        var pendente = await PostAsync(dono, preparacao, "/Painel/Gerar", campos);
        Assert.Contains("Confirme a", await pendente.Content.ReadAsStringAsync());
        await using var db = portal.Contexto();
        var doc = await db.Documentos.AsNoTracking().SingleAsync(d => d.Id == id);
        var questao = RevisaoDocumento.Ler(doc).Questoes.Single();
        var edicao = new EdicaoDocumento(["Instrução conferida"], [new(questao.Id, "7", "Leia o texto a seguir e responda: qual é sua escolha?", questao.Alternativas)]);
        await PostAsync(dono, tela, "/Documentos/Salvar/" + id, new() {
            ["versao"] = Convert.ToBase64String(doc.Versao), ["confirmar"] = "true", ["edicao"] = JsonSerializer.Serialize(edicao) });
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(outro, preparacao, "/Painel/Gerar", campos)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await dono.PostAsync("/Painel/Gerar", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        var perfilInvalido = new Dictionary<string, string>(campos) { ["Perfil"] = "Outro" };
        var invalida = await PostAsync(dono, preparacao, "/Painel/Gerar", perfilInvalido);
        Assert.Contains("Escolha um perfil disponível", WebUtility.HtmlDecode(await invalida.Content.ReadAsStringAsync()));
        Assert.False(await db.Adaptacoes.AnyAsync(a => a.Id == Guid.Parse(campos["SolicitacaoId"])));
        var escolherPerfil = await dono.GetStringAsync(preparacao + "?documentoId=" + id);
        Assert.Contains("perfil-adaptacao", escolherPerfil);
        var gerada = await PostAsync(dono, preparacao, "/Painel/Gerar", campos);
        Assert.Equal(HttpStatusCode.Redirect, gerada.StatusCode);
        var destino = gerada.Headers.Location!.OriginalString;
        Assert.Equal(destino, (await PostAsync(dono, preparacao, "/Painel/Gerar", campos)).Headers.Location!.OriginalString);
        var pagina = await dono.GetStringAsync(destino);
        Assert.Contains("Leia o texto a seguir. Responda", pagina);
        Assert.Contains("Por que a prova foi adaptada assim?", pagina);
        Assert.Contains("Trechos ajustados: 1", pagina);
        Assert.Contains("Questão 7", WebUtility.HtmlDecode(pagina));
        Assert.Contains("folha-ampla", pagina);
        Assert.Contains("espaco-folha", pagina);
        Assert.Contains("Aprovar esta versão", WebUtility.HtmlDecode(pagina));
        Assert.Contains(destino, await dono.GetStringAsync(preparacao));
        Assert.Equal(HttpStatusCode.NotFound, (await outro.GetAsync(destino)).StatusCode);
        var versaoId = Guid.Parse(campos["SolicitacaoId"]);
        var download = "/Painel/BaixarPdf/" + versaoId;
        Assert.Equal(HttpStatusCode.Conflict, (await dono.GetAsync(download)).StatusCode);
        var aprovar = "/Painel/Aprovar/" + versaoId;
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(outro, preparacao, aprovar, new())).StatusCode);
        for (int i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(dono, preparacao, aprovar, new())).StatusCode);
        Assert.Contains("Imprimir folha", await dono.GetStringAsync(destino));
        Assert.Equal(HttpStatusCode.NotFound, (await outro.GetAsync(download)).StatusCode);
        using var arquivo = await dono.GetAsync(download);
        Assert.Equal("application/pdf", arquivo.Content.Headers.ContentType?.MediaType);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(await arquivo.Content.ReadAsByteArrayAsync());
        var texto = UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor.ContentOrderTextExtractor.GetText(pdf.GetPage(1));
        Assert.Contains("Responda", texto);
        Assert.DoesNotContain("Por que a prova", texto);
        Assert.DoesNotContain(perfil, texto);
        Assert.Equal(1, await db.HistoricoAdaptacoes.CountAsync(h => h.AdaptacaoId == versaoId));
        Assert.Equal(EstadoAdaptacao.Aprovada, (await db.Adaptacoes.SingleAsync(a => a.Id == versaoId)).Estado);
        await PostAsync(dono, tela, "/Documentos/Excluir/" + id, new());
        Assert.False((await db.Documentos.AsNoTracking().SingleAsync(d => d.Id == id)).ExclusaoSolicitada);
        Assert.Equal(HttpStatusCode.OK, (await dono.GetAsync("/Documentos/Original/" + id)).StatusCode);
    }

    [Fact]
    public async Task ReabreFolhaAntigaSemRepetirDesenhoDePaginaNasQuestoes()
    {
        using var dono = await portal.LoginAsync("100");
        using var envio = await Upload(dono, "Legado.pdf", Amostras.Pdf("1. Explique.", "2. Justifique."));
        var id = Guid.Parse(envio.Headers.Location!.OriginalString.Split('/')[^1]);
        await using var db = portal.Contexto();
        var documento = await db.Documentos.SingleAsync(d => d.Id == id);
        var conteudo = RevisaoDocumento.Ler(documento);
        var ordem = conteudo.Blocos.Count + 1;
        conteudo.Blocos.Add(new(ordem, "Página 1", "Visual de página", "Elementos vetoriais", "pdf:pagina/1/desenhos"));
        foreach (var q in conteudo.Questoes) q.ElementosVisuais.Add(ordem);
        documento.ExtracaoJson = JsonSerializer.Serialize(conteudo, RevisaoDocumento.Json);
        var versao = new AdaptacaoAvaliacao {
            Id = Guid.NewGuid(), AvaliacaoId = documento.AvaliacaoId, ModeloIa = "Apresentação local v1",
            VersaoPrompt = "apresentacao-1", RevisaoBase = "2026.11.3", Estado = EstadoAdaptacao.Aprovada,
            Resultado = JsonSerializer.Serialize(new { documentoId = id, nivelSuporte = 3, leituraAmpla = true,
                guiaInstrucoes = false, espacoResposta = false, conteudo }, RevisaoDocumento.Json)
        };
        db.Adaptacoes.Add(versao);
        await db.SaveChangesAsync();
        var html = WebUtility.HtmlDecode(await dono.GetStringAsync("/Painel/Adaptacao/" + versao.Id));
        Assert.Contains("Questão 1", html);
        Assert.Contains("Questão 2", html);
        Assert.Contains("#page=1", html);
        Assert.DoesNotContain("pdf:pagina", html);
        Assert.DoesNotContain("Elementos vetoriais", html);
        Assert.Single(html.Split("Material visual no PDF original").Skip(1));
    }

    [Fact]
    public async Task FalhaDeExtracaoPreservaOriginal()
    {
        using var c = await portal.LoginAsync("100");
        var bytes = Amostras.Docx(8001);
        using var resposta = await Upload(c, "Longa.docx", bytes);
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        var id = Guid.Parse(resposta.Headers.Location!.OriginalString.Split('/')[^1]);
        await using var db = portal.Contexto();
        var doc = await db.Documentos.SingleAsync(d => d.Id == id);
        Assert.Equal(EstadoProcessamento.Falhou, doc.Processamento);
        Assert.Null(doc.ExtracaoJson);
        Assert.Equal(bytes, await c.GetByteArrayAsync("/Documentos/Original/" + id));
    }

    [Fact]
    public async Task RecusaDocxRenomeadoComoPdf()
    {
        using var c = await portal.LoginAsync("100");
        using var resposta = await Upload(c, "formato.pdf", Amostras.Docx());
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        await using var db = portal.Contexto();
        Assert.False(await db.Documentos.AnyAsync(d => d.Avaliacao.Titulo == "formato.pdf"));
    }
}
