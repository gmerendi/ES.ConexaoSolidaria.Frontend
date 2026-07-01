using Blazored.LocalStorage;
using Frontend;
using Frontend.Auth;
using Frontend.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// ── HttpClient apontando para o Gateway ──────────────────────────────────────
var gatewayUrl = builder.Configuration["GatewayUrl"] ?? "";

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(gatewayUrl)
});


// ── MudBlazor ────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();


// ── LocalStorage ─────────────────────────────────────────────────────────────
builder.Services.AddBlazoredLocalStorage();


// ── Autenticacao ─────────────────────────────────────────────────────────────
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddScoped<JwtAuthStateProvider>();


// ── Servicos ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CampanhaPublicaService>();
builder.Services.AddScoped<CampanhaAdminService>();
builder.Services.AddScoped<DoacaoService>();

await builder.Build().RunAsync();