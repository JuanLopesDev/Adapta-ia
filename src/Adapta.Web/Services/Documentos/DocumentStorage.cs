using System.Globalization;
using System.Text;

namespace Adapta.Web.Services.Documentos;

public interface IDocumentStorageService
{
    Task<string> GravarAsync(Stream arquivo, CancellationToken ct);
    Stream Abrir(string chave);
    void Excluir(string chave);
}

public sealed class DocumentStorage : IDocumentStorageService
{
    public const int Limite = 10 * 1024 * 1024;
    private readonly string pasta;
    public DocumentStorage(IWebHostEnvironment env, IConfiguration config)
    {
        pasta = Path.GetFullPath(config["Documentos:Diretorio"] ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Adapta", "Documentos"));
        var relativo = Path.GetRelativePath(env.WebRootPath, pasta);
        if (!Path.IsPathRooted(relativo) && relativo != ".." && !relativo.StartsWith(".." + Path.DirectorySeparatorChar))
            throw new InvalidOperationException("Configure um diretório privado para documentos.");
    }

    public static string NomeSeguro(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 200 || nome != nome.Trim() ||
            nome.IndexOfAny(['/', '\\', ':', '<', '>', '"', '|', '*', '?']) >= 0 ||
            nome.Any(c => char.IsControl(c) || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format))
            throw new InvalidDataException("Nome de arquivo inválido.");
        return nome.Normalize(NormalizationForm.FormC);
    }

    private string Caminho(string chave)
    {
        if (!Guid.TryParseExact(chave, "N", out var id)) throw new InvalidDataException("Identificador de arquivo inválido.");
        return Path.Combine(pasta, id.ToString("N"));
    }

    public async Task<string> GravarAsync(Stream arquivo, CancellationToken ct)
    {
        Directory.CreateDirectory(pasta);
        var chave = Guid.NewGuid().ToString("N");
        var caminho = Caminho(chave);
        var saida = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true);
        try
        {
            await using (saida)
            {
                var buffer = new byte[65536];
                int n;
                long total = 0;
                while ((n = await arquivo.ReadAsync(buffer, ct)) > 0)
                {
                    if ((total += n) > Limite) throw new InvalidDataException("O limite é 10 MiB.");
                    await saida.WriteAsync(buffer.AsMemory(0, n), ct);
                }
                if (total == 0) throw new InvalidDataException("O arquivo está vazio.");
            }
            return chave;
        }
        catch { File.Delete(caminho); throw; }
    }
    public Stream Abrir(string chave) => File.OpenRead(Caminho(chave));
    public void Excluir(string chave) => File.Delete(Caminho(chave));
}
