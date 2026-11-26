using System.ComponentModel.DataAnnotations;

namespace Adapta.Web.ViewModels;

public class NovaSenhaViewModel
{
    public const string Requisitos = "Use pelo menos seis caracteres, com uma letra maiúscula e uma minúscula.";
    [Display(Name = "Nova senha"), DataType(DataType.Password), Required(ErrorMessage = "Informe a nova senha.")]
    [MinLength(6, ErrorMessage = Requisitos), RegularExpression(@"(?s)(?=.*[A-Z])(?=.*[a-z]).*", ErrorMessage = Requisitos)]
    public string Nova { get; set; } = "";

    [Display(Name = "Confirmar nova senha"), DataType(DataType.Password), Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(Nova), ErrorMessage = "A confirmação deve coincidir com a nova senha.")]
    public string Confirmacao { get; set; } = "";
}
