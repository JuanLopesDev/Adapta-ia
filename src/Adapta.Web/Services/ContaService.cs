using Adapta.Web.DTOs;
using Adapta.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Adapta.Web.Services;

public sealed class ContaService(UserManager<Professor> usuarios, SignInManager<Professor> sessoes) : IContaService
{
    public async Task<bool> EntrarAsync(string matricula, string senha)
    {
        var resultado = await sessoes.PasswordSignInAsync(matricula.Trim(), senha,
            isPersistent: false, lockoutOnFailure: true);
        return resultado.Succeeded;
    }

    public Task SairAsync() => sessoes.SignOutAsync();

    public async Task<ContaDocenteDto?> ObterAsync(string id)
    {
        var professor = await usuarios.FindByIdAsync(id);
        return professor is null ? null : new(professor.Nome, professor.UserName!);
    }

    public async Task<bool> AlterarSenhaAsync(string id, string atual, string nova)
    {
        var professor = await usuarios.FindByIdAsync(id);
        if (professor is null || atual == nova)
            return false;
        var resultado = await usuarios.ChangePasswordAsync(professor, atual, nova);
        if (resultado.Succeeded)
            await sessoes.RefreshSignInAsync(professor);
        return resultado.Succeeded;
    }
}
