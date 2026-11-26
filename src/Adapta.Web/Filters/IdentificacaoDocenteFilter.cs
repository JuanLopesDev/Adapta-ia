using System.Security.Claims;
using Adapta.Web.DTOs;
using Adapta.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Adapta.Web.Filters;

public sealed class IdentificacaoDocenteFilter(IContaService contas) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var id = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is not null && context.Result is ViewResult view && context.RouteData.Values["action"]?.ToString() != "Error")
            view.ViewData["Docente"] = view.Model as ContaDocenteDto ?? await contas.ObterAsync(id);
        await next();
    }
}
