using Microsoft.EntityFrameworkCore;
using Ingestion.Core.Infrastructure.Persistence;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// 1. DbContext con MigrationsAssembly
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        // Esto le dice a EF que las migraciones se generen en este proyecto (Api)
        x => x.MigrationsAssembly("Ingestion.Api") 
    ));

// Registrar la fábrica, NO la conexión activa
builder.Services.AddSingleton(new ConnectionFactory() { 
    HostName = "rabbitmq", 
    DispatchConsumersAsync = true 
});

// Registrar tu nuevo Worker de Outbox
builder.Services.AddHostedService<OutboxPublisherWorker>();

builder.Services.AddControllers();

var app = builder.Build();

// 3. Opcional: Aplicar migraciones automáticas al iniciar (muy útil en Docker)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al aplicar las migraciones.");
    }
}

app.MapControllers();
app.Run();