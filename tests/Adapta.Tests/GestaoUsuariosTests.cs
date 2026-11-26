using System.Net;
using Adapta.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Adapta.Tests.PortalFixture;

namespace Adapta.Tests;

public sealed class GestaoUsuariosTests(PortalFixture portal) : IClassFixture<PortalFixture>
{
    [Fact]
    public async Task SomenteAdministradorInicialCriaContasERenomeia()
    {
        await using var db = portal.Contexto();
        var titular = await db.Users.SingleAsync(p => p.UserName == "200");
        titular.UserName = titular.NormalizedUserName = "4805";
        var outro = await db.Users.SingleAsync(p => p.UserName == "100");
        var papel = new IdentityRole("Administrador") { NormalizedName = "ADMINISTRADOR" };
        db.Roles.Add(papel);
        foreach (var id in new[] { titular.Id, outro.Id })
            db.UserRoles.Add(new IdentityUserRole<string> { RoleId = papel.Id, UserId = id });
        await db.SaveChangesAsync();

        using var admin = await portal.LoginAsync("4805");
        const string lista = "/Conta/Administracao", criar = lista + "/criar";
        const string editar = lista + "/editar-nome?matricula=0075";
        var senha = Guid.NewGuid().ToString("N") + "Aa";
        var campos = new Dictionary<string,string> { ["Matricula"] = "0075", ["Nome"] = "Ana Souza",
            ["Nova"] = senha, ["Confirmacao"] = senha, ["Papel"] = "Administrador" };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(criar, new FormUrlEncodedContent(campos))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(admin, criar, criar, campos)).StatusCode);
        var duplicata = await PostAsync(admin, criar, criar, campos);
        Assert.Contains("Já existe", WebUtility.HtmlDecode(await duplicata.Content.ReadAsStringAsync()));
        Assert.DoesNotContain(senha, await duplicata.Content.ReadAsStringAsync());
        var nova = await db.Users.AsNoTracking().SingleAsync(p => p.UserName == "0075");
        Assert.False(await db.UserRoles.AnyAsync(r => r.UserId == nova.Id));
        var edicao = new Dictionary<string,string> { ["Nome"] = "Ana Lima", ["Versao"] = nova.ConcurrencyStamp! };

        using var docente = await portal.LoginAsync("0075", senha);
        using var outroAdmin = await portal.LoginAsync("100");
        foreach (var semPermissao in new[] { docente, outroAdmin })
        {
            foreach (var rota in new[] { lista, criar, editar })
                Assert.StartsWith("/Conta/AcessoNegado", (await semPermissao.GetAsync(rota)).Headers.Location!.PathAndQuery);
            foreach (var (rota, dados) in new[] { (criar, campos), (editar, edicao) })
                Assert.StartsWith("/Conta/AcessoNegado", (await PostAsync(semPermissao, "/Conta", rota, dados)).Headers.Location!.PathAndQuery);
        }

        Assert.Contains("Ana Souza", await admin.GetStringAsync(editar));
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(admin, editar, editar, edicao)).StatusCode);
        Assert.Contains("Ana Lima", await docente.GetStringAsync("/Conta"));
        edicao["Nome"] = "Alteração desatualizada";
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(admin, editar, editar, edicao)).StatusCode);
        Assert.Equal("Ana Lima", (await db.Users.AsNoTracking().SingleAsync(p => p.Id == nova.Id)).Nome);
        var meuNome = lista + "/editar-nome?matricula=4805";
        await PostAsync(admin, meuNome, meuNome, new() { ["Nome"] = "Responsável docente", ["Versao"] = titular.ConcurrencyStamp! });
        Assert.Contains("Responsável docente", WebUtility.HtmlDecode(await admin.GetStringAsync("/Conta")));
    }
}
