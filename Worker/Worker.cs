using System.Diagnostics;
using System.Text.Json;
using Interfaces;
using Loaders;

namespace Worker;

public class Worker : BackgroundService
{
    private readonly IEnumerable<IExtractor> _extractores;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly CargadorDimensiones _cargador;

    public Worker(
        IEnumerable<IExtractor> extractores,
        IConfiguration configuration,
        ILogger<Worker> logger,
        IHostApplicationLifetime lifetime,
        CargadorDimensiones cargador)
    {
        _extractores = extractores;
        _configuration = configuration;
        _logger = logger;
        _lifetime = lifetime;
        _cargador = cargador;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cronometro = Stopwatch.StartNew();

        // Carpeta de salida configurada en appsettings.json.
        var carpetaConfigurada = _configuration["CarpetaSalida"] ?? "Staging";
        var carpetaSalida = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), carpetaConfigurada));
        Directory.CreateDirectory(carpetaSalida);

        // Todos los extractores se ejecutan al mismo tiempo.
        var tareas = _extractores.Select(extractor => ProcesarExtractorAsync(extractor, carpetaSalida));
        var resultados = await Task.WhenAll(tareas);

        foreach (var resultado in resultados)
        {
            _logger.LogInformation("Fuente {Fuente}: {Cantidad} registros.", resultado.Nombre, resultado.Cantidad);
        }

        _logger.LogInformation(
            "Errores: {Errores}. Tiempo total: {Tiempo} ms.",
            resultados.Count(resultado => resultado.Error),
            cronometro.ElapsedMilliseconds);

        // Carga de dimensiones; un fallo no detiene el programa.
        try
        {
            await _cargador.CargarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar las dimensiones.");
        }

        // El worker termina despues de una sola ejecucion.
        _lifetime.StopApplication();
    }

    private async Task<(string Nombre, int Cantidad, bool Error)> ProcesarExtractorAsync(
        IExtractor extractor,
        string carpetaSalida)
    {
        try
        {
            var comentarios = await extractor.ExtraerAsync();

            var archivo = Path.Combine(carpetaSalida, $"staging_{extractor.Nombre}.json");
            var json = JsonSerializer.Serialize(comentarios);
            await File.WriteAllTextAsync(archivo, json);

            return (extractor.Nombre, comentarios.Count, false);
        }
        catch (Exception ex)
        {
            // El fallo de un extractor no detiene a los demas.
            _logger.LogError(ex, "Error en el extractor {Fuente}.", extractor.Nombre);
            return (extractor.Nombre, 0, true);
        }
    }
}
