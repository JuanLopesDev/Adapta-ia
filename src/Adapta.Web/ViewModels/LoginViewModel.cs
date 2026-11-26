using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.ViewModels;

public sealed class LoginViewModel
{
    [Display(Name = "Matrícula"), Required(ErrorMessage = "Informe sua matrícula."), MaxLength(64, ErrorMessage = "Use até 64 caracteres.")]
    public string Matricula { get; set; } = "";

    [DataType(DataType.Password), Required(ErrorMessage = "Informe sua senha.")]
    public string Senha { get; set; } = "";

}
