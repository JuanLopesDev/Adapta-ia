using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.ViewModels;

public sealed class EsqueciSenhaViewModel
{
    [Display(Name = "Matrícula"), Required(ErrorMessage = "Informe sua matrícula."), MaxLength(64)]
    public string Matricula { get; set; } = "";
}

public sealed class RedefinirSenhaViewModel : NovaSenhaViewModel
{
    [Required] public string Id { get; set; } = "";
    [Required] public string Token { get; set; } = "";
}
