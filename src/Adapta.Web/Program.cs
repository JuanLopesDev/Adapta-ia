using Adapta.Web.Data;
using Adapta.Web.Filters;
using Adapta.Web.Models;
using Adapta.Web.Services;
using Adapta.Web.Services.Documentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

if (args.FirstOrDefault() == "--ler-documento")
{
    Environment.ExitCode = await LeitorDocumento.ExecutarFilhoAsync(args);
    return;
}
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Adapta")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Adapta para o SQL Server.");
builder.Services.AddDbContext<AdaptaDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddIdentity<Professor, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}).AddEntityFrameworkStores<AdaptaDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Entrar";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});
builder.Services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromMinutes(20));
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<RecuperacaoSenhaService>();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("conta", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        (context.Connection.RemoteIpAddress, context.Request.Path), _ => new()
        { PermitLimit = 15, Window = TimeSpan.FromMinutes(5) }));
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(Papeis.GerirUsuarios, policy => policy.RequireAuthenticatedUser()
        .RequireRole(Papeis.Administrador)
        .RequireClaim(System.Security.Claims.ClaimTypes.Name, Papeis.MatriculaAdministrador));
});
builder.Services.AddScoped<GestaoUsuariosService>();
builder.Services.AddScoped<IContaService, ContaService>();
builder.Services.AddScoped<PortalDocenteService>();
builder.Services.AddScoped<AdaptacaoService>();
builder.Services.AddSingleton<IDocumentStorageService, DocumentStorage>();
builder.Services.AddSingleton<LeitorDocumento>();
builder.Services.AddScoped<DocumentoService>();
builder.Services.AddScoped<AdministradorSeed>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<IdentificacaoDocenteFilter>();
});

var app = builder.Build();
if (args.Contains("--seed-admin", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AdministradorSeed>().ExecutarAsync();
    return;
}
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
