using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.ViewModels;

public sealed class AlterarSenhaViewModel : NovaSenhaViewModel
{
    [Display(Name = "Senha atual"), DataType(DataType.Password), Required(ErrorMessage = "Informe a senha atual.")]
    public string Atual { get; set; } = "";
}
