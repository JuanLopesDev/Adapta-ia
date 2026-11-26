using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.Models;

public sealed class Avaliacao
{
    public Guid Id { get; set; }
    public required string ProfessorId { get; set; }
    [MaxLength(200)]
    public required string Titulo { get; set; }
    public DateTimeOffset CriadaEm { get; set; } = DateTimeOffset.UtcNow;
}

