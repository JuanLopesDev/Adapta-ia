using System.Text.Json;
using Adapta.Web.Data;
using Adapta.Web.DTOs.Documentos;
using Adapta.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Adapta.Web.Services.Documentos;

public sealed class DocumentoService(AdaptaDbContext db, IDocumentStorageService storage,
    LeitorDocumento leitor, ILogger<DocumentoService> log)
{
    public Task<Documento?> BuscarAsync(string professor, Guid id, CancellationToken ct = default) =>
        db.Documentos.Include(d => d.Avaliacao).FirstOrDefaultAsync(d => d.Id == id && d.Avaliacao.ProfessorId == professor, ct);

    public async Task<Guid> ReceberAsync(string professor, string nome, Stream arquivo, CancellationToken ct)
    {
        nome = DocumentStorage.NomeSeguro(nome);
        var formato = Path.GetExtension(nome).ToLowerInvariant() switch {
            ".pdf" => FormatoArquivo.Pdf, ".docx" => FormatoArquivo.Docx,
            _ => throw new InvalidDataException("Selecione um PDF ou DOCX.") };
        var chave = await storage.GravarAsync(arquivo, ct);
        var doc = new Documento { ChaveArquivo = chave, Formato = formato,
            Avaliacao = new Avaliacao { ProfessorId = professor, Titulo = nome } };
        try
        {
            using var original = storage.Abrir(chave);
            doc.Tamanho = original.Length;
            try { await leitor.ExecutarAsync(formato, original, false, ct); }
            catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
            {
                log.LogInformation("Documento recusado na validação do formato {Formato} ({Tipo}).", formato, ex.GetType().Name);
                throw new InvalidDataException("Arquivo inválido, corrompido, protegido ou acima dos limites de leitura.");
            }
            db.Documentos.Add(doc);
            await db.SaveChangesAsync(ct);
        }
        catch { storage.Excluir(chave); throw; }
        // Após registrar o original, uma desconexão do navegador não interrompe a extração.
        await ProcessarAsync(doc);
        return doc.Id;
    }

    public async Task ProcessarAsync(Documento doc)
    {
        if (doc.ExclusaoSolicitada || doc.ExtracaoJson != null ||
            (doc.Processamento == EstadoProcessamento.Processando && doc.InicioProcessamento > DateTimeOffset.UtcNow.AddMinutes(-10)))
            throw new InvalidDataException("O documento já foi processado ou está em processamento.");
        doc.Processamento = EstadoProcessamento.Processando;
        doc.InicioProcessamento = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        try
        {
            using var original = storage.Abrir(doc.ChaveArquivo);
            var dados = (await leitor.ExecutarAsync(doc.Formato, original, true, CancellationToken.None))!;
            doc.ExtracaoJson = JsonSerializer.Serialize(dados, RevisaoDocumento.Json);
            doc.Processamento = dados.Avisos.Count == 0 ? EstadoProcessamento.Concluido : EstadoProcessamento.ConcluidoComAvisos;
            doc.Revisao = EstadoRevisao.AguardandoRevisao;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            log.LogWarning("Extração de {DocumentoId} falhou ({Tipo}).", doc.Id, ex.GetType().Name);
            doc.Processamento = EstadoProcessamento.Falhou;
        }
        doc.ProcessadoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task SalvarAsync(Documento doc, string versao, EdicaoDocumento edicao, bool confirmar)
    {
        if (doc.ExclusaoSolicitada || doc.ExtracaoJson == null) throw new InvalidDataException("Documento indisponível para revisão.");
        if (versao != Convert.ToBase64String(doc.Versao)) throw new DbUpdateConcurrencyException();
        var dados = RevisaoDocumento.Ler(doc);
        RevisaoDocumento.Aplicar(dados, edicao, confirmar);
        var normalizada = new EdicaoDocumento(dados.Instrucoes.Select(i => i.Texto).ToList(),
            dados.Questoes.Select(q => new QuestaoEditada(q.Id, q.Numero, q.Enunciado, q.Alternativas)).ToList());
        doc.RevisaoJson = JsonSerializer.Serialize(normalizada, RevisaoDocumento.Json);
        doc.Revisao = confirmar ? EstadoRevisao.Revisado : EstadoRevisao.AguardandoRevisao;
        doc.RevisadoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task ExcluirAsync(Documento doc)
    {
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            doc.ExclusaoSolicitada = true;
            await db.SaveChangesAsync();
            if (await db.Adaptacoes.AnyAsync(a => a.AvaliacaoId == doc.AvaliacaoId))
                throw new InvalidDataException("Esta avaliação já possui adaptações e deve conservar o original.");
            await tx.CommitAsync();
        }
        // A marca persistida permite repetir a remoção caso o disco esteja indisponível.
        storage.Excluir(doc.ChaveArquivo);
        db.Avaliacoes.Remove(doc.Avaliacao);
        await db.SaveChangesAsync();
    }
}
