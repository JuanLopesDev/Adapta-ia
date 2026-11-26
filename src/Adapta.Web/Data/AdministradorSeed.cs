using Adapta.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Adapta.Web.Data;

public sealed class AdministradorSeed(AdaptaDbContext db, UserManager<Professor> usuarios,
    RoleManager<IdentityRole> papeis, IConfiguration config)
{
    public async Task ExecutarAsync()
    {
        const string matricula = Papeis.MatriculaAdministrador;
        await using var transacao = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @status int;
            EXEC @status = sp_getapplock @Resource = 'Adapta.SeedAdministrador',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
            IF @status < 0 THROW 50001, 'Seed ocupado. Tente novamente.', 1;
            """);
        var professor = await usuarios.FindByNameAsync(matricula);
        if (professor is not null)
        {
            if (!await usuarios.IsInRoleAsync(professor, Papeis.Administrador))
                throw new InvalidOperationException("A matrícula inicial já pertence a uma conta sem perfil administrador.");
            await transacao.CommitAsync();
            return;
        }

        var senha = config["Seed:SenhaAdministrador"];
        if (string.IsNullOrWhiteSpace(senha))
            throw new InvalidOperationException("Configure Seed__SenhaAdministrador ou o User Secret Seed:SenhaAdministrador.");

        if (!await papeis.RoleExistsAsync(Papeis.Administrador))
            Verificar(await papeis.CreateAsync(new IdentityRole(Papeis.Administrador)));

        professor = new Professor { UserName = matricula, Nome = "Professor Administrador" };
        Verificar(await usuarios.CreateAsync(professor, senha));
        Verificar(await usuarios.AddToRoleAsync(professor, Papeis.Administrador));
        await transacao.CommitAsync();
    }

    private static void Verificar(IdentityResult resultado)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException("Falha no seed: " + string.Join(", ", resultado.Errors.Select(e => e.Code)));
    }
}
