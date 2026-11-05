using Catalog.API.Data;
using Catalog.API.Integration;
using Catalog.API.Integration.ItemEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();


/* builder.Services.AddDbContext<CatalogContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"))); */

// la conexión a la bd está definida en appsettings
string? db2Use = Environment.GetEnvironmentVariable("DB2USE");
// En caso de que el entorno de despliegue producción (Docker)
if (db2Use == "DockerSQLServer")
{
    builder.Services.AddDbContext<CatalogContext>(options => {
        options.UseSqlServer(builder.Configuration
        .GetConnectionString("SQLServerDockerConnection"),
        sqlServerOptionsAction: sqlOptions => {
            //Recuperamos el nombre del Ensamblado donde está la migración
            sqlOptions.MigrationsAssembly(
    Assembly.GetExecutingAssembly().GetName().Name);
            //Configuramos una conexión resiliente a la BD
            sqlOptions.
    EnableRetryOnFailure(maxRetryCount: 5,
    maxRetryDelay: TimeSpan.FromSeconds(30),
    errorNumbersToAdd: null);
        });
    });
}
else
{
    builder.Services.AddDbContext<CatalogContext>(options => options
    .UseSqlServer(builder.Configuration
    .GetConnectionString("DefaultConnection")));
}


// Configuración de Swagger.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catalog.API",
        Contact = new OpenApiContact
        {
            // Sustituir por tus datos.
            Email = "pcarper@upv.es",
            Name = "Pablo Carrasco Pérez",
            Url = new Uri("https://www.upv.es")
        },
        License = new OpenApiLicense
        {
            Name = "Apache License, 2.0",
            Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
        },
        Version = "v1"
    });

    // Ruta del archivo de documentación XML.
    // Estas líneas todavía no incorporan el XML a Swagger.
    var xmlfile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlfile);
});

builder.Services.Configure<RabbitOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.Configure<RabbitMqExchange>(
    builder.Configuration.GetSection("RabbitMqExchange"));

builder.Services.AddSingleton<IEventsPublisher, EventsPublisher>();

var app = builder.Build();

// Activar Swagger durante el desarrollo.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        // Aplicar las migraciones pendientes durante el desarrollo.
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.MapControllers();

var eventsPublisher =
    app.Services.GetRequiredService<IEventsPublisher>();

try
{

    await eventsPublisher.Start();


    await app.RunAsync();
}
finally
{
    await eventsPublisher.Stop();
}