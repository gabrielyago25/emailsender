using EmailSender.Core.Interfaces;
using EmailSender.Core.Services;
using EmailSender.Infrastructure.Configuration;
using EmailSender.Infrastructure.Services;
using EmailSender.Api.Jobs;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options => {options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());});
builder.Services.AddOpenApi();

var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();

if (emailSettings is null)
{
    throw new InvalidOperationException("As configurações de e-mail não foram encontradas. Verifique o arquivo de configuração.");
}

if (string.IsNullOrWhiteSpace(emailSettings.Host) || string.IsNullOrWhiteSpace(emailSettings.Usuario) || string.IsNullOrWhiteSpace(emailSettings.Senha) || string.IsNullOrWhiteSpace(emailSettings.Remetente))
{
    throw new InvalidOperationException("As configurações SMTP estão incompletas");
}

builder.Services.AddSingleton(emailSettings);

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<EnvioService>();

builder.Services.AddSingleton<EnvioJobStore>();
builder.Services.AddSingleton<EnvioJobQueue>();
builder.Services.AddHostedService<EnvioBackgroundService>();

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => {
    policy.WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod();
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.MapControllers();
app.Run();