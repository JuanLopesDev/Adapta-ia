using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.Models;

public sealed class Aluno
{
    public Guid Id { get; set; }
    public required string ProfessorId { get; set; }
    [MaxLength(64)]
    public required string Apelido { get; set; }
    public Guid? PerfilAdaptacaoId { get; set; }
}
