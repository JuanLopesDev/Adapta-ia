namespace Adapta.Web.Models;

public static class PerfisAvaliacao
{
    public const string Tea = "TEA";
    public const string Dislexia = "Dislexia";
    public static bool Disponivel(string? perfil) => perfil is Tea or Dislexia;
    public static string Nome(string perfil) => perfil == Tea ? "Autismo (TEA)" : perfil;
}
