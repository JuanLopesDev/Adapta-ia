using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Adapta.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Adapta.Web.Controllers;

[AllowAnonymous]
public sealed class HomeController : Controller
{
    public IActionResult Index() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Painel") : View();

    public IActionResult Guia() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
