using System.Globalization;
using CsvHelper;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Unico endpoint: lee el CSV y devuelve la lista de comentarios como JSON.
app.MapGet("/api/comentarios", IResult (IConfiguration config) =>
{
    // La ruta del CSV se configura en appsettings.json.
    var rutaConfigurada = config["RutaCsv"];

    if (string.IsNullOrWhiteSpace(rutaConfigurada))
    {
        return Results.Problem("No se configuro la ruta del CSV (clave 'RutaCsv' en appsettings.json).", statusCode: 500);
    }

    // La ruta es relativa a la carpeta del proyecto (directorio de trabajo al ejecutar con dotnet run).
    var rutaCompleta = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), rutaConfigurada));

    if (!File.Exists(rutaCompleta))
    {
        return Results.Problem($"No se encontro el archivo CSV en la ruta: {rutaCompleta}", statusCode: 500);
    }

    try
    {
        using var reader = new StreamReader(rutaCompleta);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Los campos de texto vacios (por ejemplo IdCliente) se devuelven como null.
        csv.Context.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("");

        var comentarios = csv.GetRecords<Comentario>().ToList();
        return Results.Ok(comentarios);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al leer el archivo CSV: {ex.Message}", statusCode: 500);
    }
});

app.Run();
