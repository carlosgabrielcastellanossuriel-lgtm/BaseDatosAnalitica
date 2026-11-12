using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Interfaces;
using Models;

namespace Extractors;

public class CsvExtractor : IExtractor
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CsvExtractor> _logger;

    public CsvExtractor(IConfiguration configuration, ILogger<CsvExtractor> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string Nombre => "Csv";

    public async Task<List<ComentarioStaging>> ExtraerAsync()
    {
        // La ruta se configura en appsettings.json.
        var rutaConfigurada = _configuration["RutaCsv"];

        if (string.IsNullOrWhiteSpace(rutaConfigurada))
        {
            throw new InvalidOperationException("No se configuro la ruta del CSV (clave 'RutaCsv' en appsettings.json).");
        }

        // La ruta es relativa a la carpeta del proyecto.
        var rutaCompleta = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), rutaConfigurada));

        if (!File.Exists(rutaCompleta))
        {
            throw new FileNotFoundException($"No se encontro el archivo CSV en la ruta: {rutaCompleta}", rutaCompleta);
        }

        var comentarios = new List<ComentarioStaging>();

        using var reader = new StreamReader(rutaCompleta);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Primero se lee la fila de encabezados.
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            try
            {
                var fila = csv.GetRecord<FilaCsv>();

                // Validacion simple de los datos obligatorios.
                if (string.IsNullOrWhiteSpace(fila.IdOpinion) ||
                    string.IsNullOrWhiteSpace(fila.IdProducto) ||
                    string.IsNullOrWhiteSpace(fila.Comentario))
                {
                    _logger.LogWarning("Fila {Fila} omitida: faltan datos obligatorios.", csv.Parser.Row);
                    continue;
                }

                comentarios.Add(new ComentarioStaging
                {
                    IdComentario = fila.IdOpinion,
                    IdCliente = fila.IdCliente,
                    IdProducto = fila.IdProducto,
                    Fuente = "Encuesta", // La columna Fuente del archivo se ignora.
                    Fecha = fila.Fecha,
                    Texto = fila.Comentario,
                    Puntuacion = fila.Puntuacion,
                    Clasificacion = fila.Clasificacion
                });
            }
            catch (Exception ex)
            {
                // Una fila invalida no detiene la extraccion.
                _logger.LogWarning(ex, "Fila {Fila} omitida por datos invalidos.", csv.Parser.Row);
            }
        }

        _logger.LogInformation("CsvExtractor: se leyeron {Cantidad} registros desde {Ruta}.", comentarios.Count, rutaCompleta);

        return comentarios;
    }

    // Clase privada usada solo para leer las columnas del CSV.
    private class FilaCsv
    {
        [Name("IdOpinion")]
        public string IdOpinion { get; set; } = "";

        [Name("IdCliente")]
        public string? IdCliente { get; set; }

        [Name("IdProducto")]
        public string IdProducto { get; set; } = "";

        [Name("Fecha")]
        public DateTime Fecha { get; set; }

        [Name("Comentario")]
        public string Comentario { get; set; } = "";

        [Name("Clasificación")]
        public string? Clasificacion { get; set; }

        [Name("PuntajeSatisfacción")]
        public int? Puntuacion { get; set; }
    }
}
