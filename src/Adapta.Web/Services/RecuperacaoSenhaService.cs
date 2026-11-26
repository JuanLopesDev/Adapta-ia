using Adapta.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Adapta.Web.Services;

public sealed class RecuperacaoSenhaService(UserManager<Professor> usuarios, EmailService email,
    IConfiguration config, ILogger<RecuperacaoSenhaService> log)
{
    public async Task SolicitarAsync(string matricula)
    {
        var professor = await usuarios.FindByNameAsync(matricula.Trim());
        if (professor?.Email is null || !professor.EmailConfirmed)
            return;
        if (!Uri.TryCreate(config["App:UrlPublica"], UriKind.Absolute, out var origem) ||
            (origem.Scheme != "https" && !(origem.IsLoopback && origem.Scheme == "http")))
        {
            log.LogWarning("Configure App:UrlPublica para enviar links de recuperação.");
            return;
        }
        var token = await usuarios.GeneratePasswordResetTokenAsync(professor);
        var link = QueryHelpers.AddQueryString(new Uri(origem, "Conta/RedefinirSenha").AbsoluteUri,
            new Dictionary<string, string?> { ["id"] = professor.Id, ["token"] = token });
        await email.EnviarAsync(professor.Email, "Adapta · Recuperação de senha",
            $"Para escolher uma nova senha, abra o link em até 20 minutos:\n{link}\nSe você não solicitou a alteração, ignore esta mensagem.");
    }

    public async Task<bool> RedefinirAsync(string id, string token, string senha)
    {
        var professor = await usuarios.FindByIdAsync(id);
        return professor is not null && (await usuarios.ResetPasswordAsync(professor, token, senha)).Succeeded;
    }
}
