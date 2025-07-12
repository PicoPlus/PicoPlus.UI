using Microsoft.JSInterop;
using MudBlazor.Services;
using PicoPlus.Components;
using Blazored.SessionStorage;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
builder.Configuration.AddJsonFile("appsettings.json");
// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Configuration.AddJsonFile("appsettings.json");
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Contact>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Objects.Deal>();
builder.Services.AddScoped<PicoPlus.Services.Identity.Zibal>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Commerce.Product>();   
builder.Services.AddScoped<PicoPlus.Services.CRM.Commerce.LineItem>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Pipelines>();
builder.Services.AddScoped<PicoPlus.Services.CRM.Associate>();

builder.Services.AddScoped<PicoPlus.Services.CRM.Owners>();
builder.Services.AddScoped<PicoPlus.Services.SMS.SMS.Send>();
builder.Services.AddScoped<PicoPlus.Views.Deal.Create>();
builder.Services.AddScoped<HttpClient>();
builder.Services.AddRazorPages();
builder.Services.AddMudServices();
builder.Services.AddBlazoredSessionStorage();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
