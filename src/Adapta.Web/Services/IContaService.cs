using Adapta.Web.DTOs;

namespace Adapta.Web.Services;

public interface IContaService
{
    Task<bool> EntrarAsync(string matricula, string senha);
    Task SairAsync();
    Task<ContaDocenteDto?> ObterAsync(string id);
    Task<bool> AlterarSenhaAsync(string id, string atual, string nova);
}
