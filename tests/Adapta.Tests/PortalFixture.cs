using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Adapta.Web.Data;
using Adapta.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Adapta.Tests;

public sealed class PortalFixture : IAsyncLifetime
{
    public string Pasta { get; } = Path.Combine(Path.GetTempPath(), "AdaptaDocumentos-" + Guid.NewGuid().ToString("N"));
    private readonly string banco = "AdaptaDocumentosTeste_" + Guid.NewGuid().ToString("N");
    private readonly string senha = Guid.NewGuid().ToString("N") + "Aa";
    private Process? servidor;
    private bool bancoCriado;
    public Uri Base { get; private set; } = null!;
    private string Conexao(string nome) => new SqlConnectionStringBuilder {
        DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = nome, IntegratedSecurity = true, TrustServerCertificate = true
    }.ConnectionString;
    public AdaptaDbContext Contexto() => new(new DbContextOptionsBuilder<AdaptaDbContext>().UseSqlServer(Conexao(banco)).Options);

    public async Task InitializeAsync()
    {
        using (var master = new SqlConnection(Conexao("master")))
        {
            await master.OpenAsync();
            using var cmd = new SqlCommand($"CREATE DATABASE [{banco}]", master);
            await cmd.ExecuteNonQueryAsync();
            bancoCriado = true;
        }
        await using var db = Contexto();
        await db.Database.MigrateAsync();
        foreach (var matricula in new[] {"100", "200"})
        {
            var p = new Professor { Nome = "Docente de teste", UserName = matricula, NormalizedUserName = matricula,
                SecurityStamp = Guid.NewGuid().ToString(), LockoutEnabled = true };
            p.PasswordHash = new PasswordHasher<Professor>().HashPassword(p, senha);
            db.Users.Add(p);
        }
        await db.SaveChangesAsync();
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz != null && !File.Exists(Path.Combine(raiz.FullName, "Adapta.sln"))) raiz = raiz.Parent;
        var projeto = Path.Combine(raiz!.FullName, "src", "Adapta.Web");
        var config = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(projeto, "bin", config, "net10.0", "Adapta.Web.dll");
        using var porta = new TcpListener(IPAddress.Loopback, 0);
        porta.Start();
        Base = new Uri($"http://127.0.0.1:{((IPEndPoint)porta.LocalEndpoint).Port}");
        porta.Stop();
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = projeto, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(dll);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(Base.ToString());
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        start.Environment["ConnectionStrings__Adapta"] = Conexao(banco);
        start.Environment["Documentos__Diretorio"] = Pasta;
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        servidor = Process.Start(start)!;
        using var cliente = new HttpClient();
        for (int i = 0; i < 120; i++)
        {
            if (servidor.HasExited) break;
            try { if ((await cliente.GetAsync(Base)).IsSuccessStatusCode) return; }
            catch (HttpRequestException) { }
            await Task.Delay(250);
        }
        throw new InvalidOperationException("Kestrel não iniciou.");
    }

    public async Task<HttpClient> LoginAsync(string matricula, string? senhaEscolhida = null)
    {
        var c = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false }) { BaseAddress = Base };
        using var r = await PostAsync(c, "/Conta/Entrar", "/Conta/Entrar", new() { ["Matricula"] = matricula, ["Senha"] = senhaEscolhida ?? senha });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        return c;
    }

    public static async Task<string> TokenAsync(HttpClient c, string tela)
    {
        var html = await c.GetStringAsync(tela);
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(m.Success, "Token antifalsificação ausente.");
        return m.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostAsync(HttpClient c, string tela, string destino, Dictionary<string,string> dados)
    {
        dados["__RequestVerificationToken"] = await TokenAsync(c, tela);
        return await c.PostAsync(destino, new FormUrlEncodedContent(dados));
    }

    public async Task DisposeAsync()
    {
        if (servidor is { HasExited:false }) { servidor.Kill(true); await servidor.WaitForExitAsync(); }
        servidor?.Dispose();
        if (bancoCriado) { await using var db = Contexto(); await db.Database.EnsureDeletedAsync(); }
        if (Directory.Exists(Pasta)) Directory.Delete(Pasta, true);
    }
}
