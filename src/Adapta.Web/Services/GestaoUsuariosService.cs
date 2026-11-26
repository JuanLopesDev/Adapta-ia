using Adapta.Web.DTOs;
using Adapta.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Adapta.Web.Services;

public sealed class GestaoUsuariosService(UserManager<Professor> usuarios)
{
    public Task<List<ContaDocenteDto>> ListarAsync(CancellationToken ct) =>
        usuarios.Users.AsNoTracking().OrderBy(p => p.Nome).ThenBy(p => p.UserName)
            .Select(p => new ContaDocenteDto(p.Nome, p.UserName!)).ToListAsync(ct);

    public Task<Professor?> BuscarAsync(string matricula) => usuarios.FindByNameAsync(matricula);

    public async Task<IdentityResult> CriarAsync(string nome, string matricula, string senha)
    {
        var usuario = new Professor { UserName = matricula.Trim(), Nome = nome.Trim() };
        try { return await usuarios.CreateAsync(usuario, senha); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return IdentityResult.Failed(usuarios.ErrorDescriber.DuplicateUserName(usuario.UserName));
        }
    }

    public Task<IdentityResult> RenomearAsync(Professor professor, string nome, string versao)
    {
        if (professor.ConcurrencyStamp != versao)
            return Task.FromResult(IdentityResult.Failed(usuarios.ErrorDescriber.ConcurrencyFailure()));
        professor.Nome = nome.Trim();
        return usuarios.UpdateAsync(professor);
    }
}
