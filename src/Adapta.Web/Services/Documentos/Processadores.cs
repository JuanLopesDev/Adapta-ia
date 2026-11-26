using Adapta.Web.DTOs.Documentos;
using Adapta.Web.Models;

namespace Adapta.Web.Services.Documentos;

public interface IDocumentProcessingService
{
    FormatoArquivo Formato { get; }
    void Validar(Stream arquivo);
    ExtractedAssessment Extrair(Stream arquivo);
}
