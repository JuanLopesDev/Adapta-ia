using System.Text.Json;
using Adapta.Web.DTOs.Documentos;
using Adapta.Web.Models;

namespace Adapta.Web.Services.Documentos;

public record QuestaoEditada(Guid? Id, string Numero, string Enunciado, List<ExtractedAlternative> Alternativas);
public record EdicaoDocumento(List<string> Instrucoes, List<QuestaoEditada> Questoes);

public static class RevisaoDocumento
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static ExtractedAssessment Ler(Documento doc)
    {
        var dados = JsonSerializer.Deserialize<ExtractedAssessment>(doc.ExtracaoJson!, Json)!;
        if (doc.RevisaoJson != null)
            Aplicar(dados, JsonSerializer.Deserialize<EdicaoDocumento>(doc.RevisaoJson, Json)!, false);
        if (doc.Revisao == EstadoRevisao.Revisado)
            foreach (var q in dados.Questoes) q.Confianca = "Conferido pelo professor";
        return dados;
    }

    public static void Aplicar(ExtractedAssessment dados, EdicaoDocumento edicao, bool confirmar)
    {
        if (edicao.Questoes is null || edicao.Instrucoes is null || edicao.Questoes.Count > 1000 ||
            edicao.Instrucoes.Count > 1000 || edicao.Instrucoes.Any(i => i == null || i.Length > 50000))
            throw new InvalidDataException("Revisão inválida ou acima dos limites.");
        var anteriores = dados.Questoes.ToDictionary(q => q.Id);
        var novas = new List<ExtractedQuestion>();
        var vistos = new HashSet<Guid>();
        foreach (var item in edicao.Questoes)
        {
            if (item is null || item.Numero is null || item.Numero.Length > 100 || item.Enunciado is null ||
                item.Enunciado.Length > 100000 || item.Alternativas is null || item.Alternativas.Count > 100 ||
                item.Alternativas.Any(a => a is null || a.Rotulo is null || a.Rotulo.Length > 100 || a.Texto is null || a.Texto.Length > 100000))
                throw new InvalidDataException("Confira os campos das questões.");
            var id = item.Id ?? Guid.NewGuid();
            if (!vistos.Add(id)) throw new InvalidDataException("Há questões repetidas.");
            var questao = anteriores.GetValueOrDefault(id) ?? new ExtractedQuestion { Id = id };
            questao.Numero = item.Numero;
            questao.Enunciado = item.Enunciado;
            questao.Alternativas = item.Alternativas;
            questao.Ordem = novas.Count + 1;
            if (confirmar && string.IsNullOrWhiteSpace(questao.Enunciado) && questao.ElementosVisuais.Count == 0)
                throw new InvalidDataException("Preencha os enunciados antes de confirmar.");
            novas.Add(questao);
        }
        if (anteriores.Keys.Any(id => !vistos.Contains(id)) || (confirmar && novas.Count == 0))
            throw new InvalidDataException("Inclua e confira todas as questões antes de confirmar.");
        dados.Questoes = novas;
        var origens = dados.Instrucoes.Select(i => i.Bloco).ToArray();
        dados.Instrucoes.Clear();
        for (var i = 0; i < edicao.Instrucoes.Count; i++)
            dados.Instrucoes.Add(new(i < origens.Length ? origens[i] : 0, edicao.Instrucoes[i]));
    }
}
