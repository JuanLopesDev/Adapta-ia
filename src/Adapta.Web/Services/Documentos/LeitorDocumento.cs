using System.Diagnostics;
using System.Text.Json;
using Adapta.Web.DTOs.Documentos;
using Adapta.Web.Models;

namespace Adapta.Web.Services.Documentos;

// O leitor recebe apenas os bytes pela entrada padrão. Uma falha do parser não encerra o servidor MVC.
public sealed class LeitorDocumento
{
    private readonly SemaphoreSlim vagas = new(2);
    public async Task<ExtractedAssessment?> ExecutarAsync(FormatoArquivo formato, Stream original, bool extrair, CancellationToken ct)
    {
        if (!await vagas.WaitAsync(TimeSpan.FromSeconds(10), ct))
            throw new InvalidDataException("Os leitores estão ocupados. Tente novamente.");
        try
        {
            var inicio = new ProcessStartInfo("dotnet") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            inicio.ArgumentList.Add(typeof(LeitorDocumento).Assembly.Location);
            inicio.ArgumentList.Add("--ler-documento");
            inicio.ArgumentList.Add(formato.ToString());
            inicio.ArgumentList.Add(extrair ? "extrair" : "validar");
            // Valor hexadecimal em bytes para o limite de heap gerenciado do runtime.
            inicio.Environment["DOTNET_GCHeapHardLimit"] = "10000000";
            using var processo = Process.Start(inicio) ?? throw new IOException("Leitor indisponível.");
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(TimeSpan.FromSeconds(45));
            try
            {
                var erros = processo.StandardError.BaseStream.CopyToAsync(Stream.Null, limite.Token);
                var saida = processo.StandardOutput.ReadToEndAsync(limite.Token);
                await original.CopyToAsync(processo.StandardInput.BaseStream, limite.Token);
                processo.StandardInput.Close();
                await processo.WaitForExitAsync(limite.Token);
                await erros;
                var json = await saida;
                if (processo.ExitCode != 0) throw new InvalidDataException("O leitor não conseguiu interpretar o arquivo.");
                return extrair ? JsonSerializer.Deserialize<ExtractedAssessment>(json, RevisaoDocumento.Json)
                    ?? throw new InvalidDataException() : null;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new InvalidDataException("O arquivo excedeu o tempo de processamento.");
            }
            finally
            {
                if (!processo.HasExited) { processo.Kill(entireProcessTree: true); await processo.WaitForExitAsync(); }
            }
        }
        finally { vagas.Release(); }
    }

    public static async Task<int> ExecutarFilhoAsync(string[] args)
    {
        if (args.Length != 3 || args[2] is not ("validar" or "extrair")) return 2;
        try
        {
            IDocumentProcessingService leitor = args[1] switch
            {
                "Pdf" => new PdfProcessor(), "Docx" => new DocxProcessor(), _ => throw new InvalidDataException()
            };
            using var arquivo = new MemoryStream();
            using var entrada = Console.OpenStandardInput();
            var buffer = new byte[65536];
            int n;
            while ((n = await entrada.ReadAsync(buffer)) > 0)
            {
                if (arquivo.Length + n > DocumentStorage.Limite) return 2;
                arquivo.Write(buffer, 0, n);
            }
            arquivo.Position = 0;
            if (args[2] == "validar") leitor.Validar(arquivo);
            else await JsonSerializer.SerializeAsync(Console.OpenStandardOutput(), leitor.Extrair(arquivo), RevisaoDocumento.Json);
            return 0;
        }
        catch (Exception) { return 1; }
    }
}
