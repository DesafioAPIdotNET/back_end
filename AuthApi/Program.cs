using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using AuthApi.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configuração de Logs ---
// O Render captura automaticamente o que vai para o Console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- 2. Configuração do Banco de Dados (Neon - PostgreSQL) ---
// Pega a string de conexão das Variáveis de Ambiente (Segurança)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- 3. Rate Limiting (Proteção contra abuso) ---
// Limita a 10 requisições a cada 10 segundos por IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(10)
            }));
});

// --- 4. Configuração de CORS (Segurança de Origem) ---
var allowedOrigins = "_allowedOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowedOrigins,
        policy =>
        {
            policy.WithOrigins(
                "https://back.lhtecnologia.net.br", 
                "https://front.lhtecnologia.net.br",
                "http://localhost:3000") // Para testes locais se precisar
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 5. Configuração do Swagger (Documentação) ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "LH Tecnologia API", 
        Version = "v1",
        Description = "API RESTful com C# .NET 8 e PostgreSQL (Neon)"
    });
});

var app = builder.Build();

// --- 6. Pipeline de Execução (Middleware) ---

// Habilita Swagger em qualquer ambiente (Prod e Dev) para facilitar seus testes
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Aplica o CORS antes de qualquer outra coisa
app.UseCors(allowedOrigins);

// Aplica o Rate Limiter
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

// --- 7. Migração Automática no Startup (Facilitador para o Render) ---
// Isso cria o banco automaticamente ao subir a aplicação
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try 
    {
        // Aplica migrações pendentes ou cria o banco se não existir
        dbContext.Database.Migrate();
        Console.WriteLine("Banco de dados migrado com sucesso.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao migrar banco: {ex.Message}");
    }
}

// Render usa a porta definida na variável PORT ou default 8080
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");