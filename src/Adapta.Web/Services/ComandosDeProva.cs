using Adapta.Web.DTOs.Documentos;
using Adapta.Web.ViewModels;

namespace Adapta.Web.Services;

public static class ComandosDeProva
{
    // Alterações restritas ao início de comandos operacionais, conservando o restante do item.
    private static readonly (string Antes, string Depois)[] Formas =
    [
        ("Leia atentamente o texto a seguir e, em seguida, responda", "Leia com atenção o texto a seguir. Depois, responda"),
        ("Leia o texto a seguir e responda", "Leia o texto a seguir. Responda"),
        ("Assinale com um X a alternativa", "Marque com um X a alternativa"),
        ("Assinale a alternativa", "Marque a alternativa"),
        ("Efetue o cálculo", "Faça o cálculo"),
        ("Com base no texto acima, responda", "Use o texto acima para responder")
    ];

    public static List<AjusteDeComando> Aplicar(ExtractedAssessment conteudo)
    {
        var ajustes = new List<AjusteDeComando>();
        string Ajustar(string texto, string local)
        {
            foreach (var (antes, depois) in Formas)
            {
                if (!texto.StartsWith(antes, StringComparison.OrdinalIgnoreCase)) continue;
                if (texto.Length > antes.Length && !char.IsWhiteSpace(texto[antes.Length]) &&
                    texto[antes.Length] is not ('.' or ':' or ',' or '?' or '!')) continue;
                ajustes.Add(new(local, texto[..antes.Length], depois));
                return depois + texto[antes.Length..];
            }
            return texto;
        }

        for (int i = 0; i < conteudo.Instrucoes.Count; i++)
        {
            var instrucao = conteudo.Instrucoes[i];
            conteudo.Instrucoes[i] = instrucao with { Texto = Ajustar(instrucao.Texto, $"Instrução {i + 1}") };
        }
        foreach (var questao in conteudo.Questoes)
            questao.Enunciado = Ajustar(questao.Enunciado, $"Questão {questao.Numero}");
        return ajustes;
    }
}
