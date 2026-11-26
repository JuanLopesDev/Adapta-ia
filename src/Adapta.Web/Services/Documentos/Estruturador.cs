using System.Text.RegularExpressions;
using Adapta.Web.DTOs.Documentos;

namespace Adapta.Web.Services.Documentos;

public sealed class Estruturador
{
    private static readonly Regex Numero = new(@"^\s*(?:(?:quest[ãa]o|pergunta|item)\s+(\d+[a-z]?)\s*[:.)º–-]?|(\d+[a-z]?)[.)º–-](?!\d))\s*(.*)$", RegexOptions.IgnoreCase);
    private static readonly Regex Opcao = new(@"^\s*\(?([a-h])\s*[).–-]\s*(.+)$", RegexOptions.IgnoreCase);
    private static readonly Regex Instrucao = new(@"^(instru[çc][õo]es|orienta[çc][õo]es|leia|responda|marque|assinale)\b", RegexOptions.IgnoreCase);
    private static readonly Regex Apoio = new(@"^(texto(\s+de\s+apoio|\s+[IVX0-9]+)?|leitura|fonte)\s*[:–-]", RegexOptions.IgnoreCase);
    private readonly ExtractedAssessment resultado = new();
    private ExtractedQuestion? atual;
    private bool emApoio;
    private int caracteres;

    public void Avisar(string origem, string mensagem) => resultado.Avisos.Add(new(origem, mensagem));

    public void Adicionar(string origem, string tipo, string texto, string? referencia = null, string[][]? celulas = null)
    {
        if (resultado.Blocos.Count >= 8000 || (caracteres += texto.Length) > 500_000)
            throw new InvalidDataException("Conteúdo acima do limite de extração.");
        if (tipo == "Texto" && Regex.IsMatch(texto.Trim(), @"^[_\.]{3,}$")) tipo = "Espaço de resposta";
        var bloco = new BlocoOriginal(resultado.Blocos.Count + 1, origem, tipo, texto, referencia, celulas);
        resultado.Blocos.Add(bloco);
        if (tipo != "Texto")
        {
            if (atual is not null && tipo != "Visual de página")
            {
                atual.Blocos.Add(bloco.Ordem);
                if (tipo is "Imagem" or "Gráfico") atual.ElementosVisuais.Add(bloco.Ordem);
            }
            return;
        }
        var linha = texto.Trim();
        if (linha.Length == 0) return;
        var numero = Numero.Match(linha);
        if (numero.Success)
        {
            atual = new ExtractedQuestion { Ordem = resultado.Questoes.Count + 1,
                Numero = numero.Groups[1].Success ? numero.Groups[1].Value : numero.Groups[2].Value,
                Enunciado = numero.Groups[3].Value };
            resultado.Questoes.Add(atual);
            atual.Blocos.Add(bloco.Ordem);
            emApoio = false;
            return;
        }
        if (Apoio.IsMatch(linha)) emApoio = true;
        var alternativa = Opcao.Match(linha);
        if (atual is not null)
        {
            atual.Blocos.Add(bloco.Ordem);
            if (alternativa.Success)
            {
                atual.Alternativas.Add(new(alternativa.Groups[1].Value, alternativa.Groups[2].Value));
                emApoio = false;
            }
            else if (emApoio) atual.Apoios.Add(new(bloco.Ordem, texto));
            else if (atual.Alternativas.Count > 0)
            {
                var ultima = atual.Alternativas[^1];
                atual.Alternativas[^1] = ultima with { Texto = ultima.Texto + "\n" + texto };
            }
            else atual.Enunciado = (atual.Enunciado + "\n" + texto).Trim();
        }
        else if (Instrucao.IsMatch(linha))
        {
            resultado.Instrucoes.Add(new(bloco.Ordem, texto));
            emApoio = false;
        }
        else resultado.Apoios.Add(new(bloco.Ordem, texto));
    }

    public ExtractedAssessment Concluir()
    {
        if (resultado.Questoes.Count == 0) Avisar("Documento", "Nenhuma questão numerada foi reconhecida. Confira os blocos e adicione questões na revisão.");
        foreach (var q in resultado.Questoes)
        {
            var texto = q.Enunciado.ToLowerInvariant();
            q.Tipo = texto.Contains("verdadeiro") || texto.Contains("falso") ? "Verdadeiro ou falso" :
                texto.Contains("relacione") || texto.Contains("associe") ? "Associação de colunas" :
                texto.Contains("lacuna") || texto.Contains("___") ? "Preenchimento de lacunas" :
                q.Alternativas.Count > 0 ? "Múltipla escolha" :
                q.ElementosVisuais.Count > 0 || texto.Contains("gráfico") || texto.Contains("figura") ? "Questão visual" :
                Regex.IsMatch(texto, @"\b(explique|descreva|justifique|escreva|comente)\b") ? "Discursiva" : "Indeterminada";
            q.Confianca = q.Tipo == "Indeterminada" || q.ElementosVisuais.Count > 0 || texto.Length == 0 ? "Revisão necessária" : "Identificação heurística";
            if (q.Confianca == "Revisão necessária") Avisar($"Questão {q.Numero}", "Confira a estrutura e o enunciado no original.");
        }
        return resultado;
    }
}
