namespace Adapta.Web.Models;

public sealed class HistoricoAdaptacao
{
    public Guid Id { get; set; }
    public Guid AdaptacaoId { get; set; }
    public EstadoAdaptacao Estado { get; set; }
    public DateTimeOffset RegistradoEm { get; set; } = DateTimeOffset.UtcNow;
}
