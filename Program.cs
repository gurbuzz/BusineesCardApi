using BusinessCardAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models; // Swagger için gerekli

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5001");

// Servis kayıtları
builder.Services.AddHttpClient<OllamaClient>();
builder.Services.AddTransient<LLMService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger yapılandırması
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BusinessCardAPI",
        Version = "v1",
        Description = "Business Card Extraction API"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BusinessCardAPI v1");
    });
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();
