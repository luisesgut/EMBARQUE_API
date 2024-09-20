using System.Net.Http;
using System.Net;
using Impinj.OctaneSdk;
using AntenasSignalR.Hubs;
using AntenasSignalR.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddControllers(); // Agrega soporte para controladores

// Registrar HttpClient con un HttpClientHandler personalizado para ignorar SSL
builder.Services.AddHttpClient("ImpinjClient").ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    };
});

// Registrar EpcReaderService como un servicio singleton
builder.Services.AddSingleton<EpcReaderService>();

// Agregar Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EPC Reader API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Usar Swagger en desarrollo y producción
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EPC Reader API v1");
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<MessageHub>("/message");
    endpoints.MapControllers(); // Asegúrate de mapear los controladores aquí
});

app.Run();
