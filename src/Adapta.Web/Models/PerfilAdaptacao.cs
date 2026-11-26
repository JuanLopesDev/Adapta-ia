using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.Models;

public sealed class PerfilAdaptacao
{
    public Guid Id { get; set; }
    [MaxLength(120)]
    public required string Nome { get; set; }
}
