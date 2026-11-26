using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Adapta.Web.Models;

public sealed class Professor : IdentityUser
{
    [MaxLength(150)]
    public required string Nome { get; set; }
}

public static class Papeis
{
    public const string MatriculaAdministrador = "4805";
    public const string GerirUsuarios = "GerirUsuarios";
    public const string Administrador = "Administrador";
}
