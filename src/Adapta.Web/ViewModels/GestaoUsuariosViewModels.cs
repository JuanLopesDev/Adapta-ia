using System.ComponentModel.DataAnnotations;
namespace Adapta.Web.ViewModels;

public sealed class CriarUsuarioViewModel : NovaSenhaViewModel
{
    [Required(ErrorMessage = "Informe a matrícula."), StringLength(64, ErrorMessage = "Use até 64 caracteres na matrícula.")]
    public string Matricula { get; set; } = "";
    [Required(ErrorMessage = "Informe o nome."), StringLength(150, ErrorMessage = "Use até 150 caracteres no nome.")]
    public string Nome { get; set; } = "";
}

public sealed class EditarNomeViewModel
{
    [Required(ErrorMessage = "Informe o nome."), StringLength(150, ErrorMessage = "Use até 150 caracteres no nome.")]
    public string Nome { get; set; } = "";
    [Required]
    public string Versao { get; set; } = "";
}
