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


// 2. RabbitMQ corregido para Docker
builder.Services.AddSingleton<IConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory
    {
        // CAMBIO: Ahora usamos coma al final
        HostName = configuration["RabbitMQ:Host"] ?? "localhost", 
        Port = 5672
        // Si tu versión de RabbitMQ es la 6.x+, DispatchConsumersAsync es por defecto
    };

    int retries = 5;
    while (retries > 0)
    {
        try
        {
            return factory.CreateConnection();
        }
        catch (Exception ex)
        {
            retries--;
            Console.WriteLine($"RabbitMQ no responde. Reintentando... ({retries} intentos restantes). Error: {ex.Message}");
            Thread.Sleep(5000); 
        }
    }
    throw new Exception("No se pudo conectar a RabbitMQ después de varios intentos.");
});

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