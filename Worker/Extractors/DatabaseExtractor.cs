using Interfaces;
using Microsoft.Data.SqlClient;
using Models;

namespace Extractors;

public class DatabaseExtractor : IExtractor
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseExtractor> _logger;

    public DatabaseExtractor(IConfiguration configuration, ILogger<DatabaseExtractor> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string Nombre => "BaseDatos";

    public async Task<List<ComentarioStaging>> ExtraerAsync()
    {
        // La cadena de conexion se configura en appsettings.json.
        var cadenaConexion = _configuration["CadenaConexionSql"];

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException("No se configuro la cadena de conexion (clave 'CadenaConexionSql' en appsettings.json).");
        }

        var comentarios = new List<ComentarioStaging>();

        try
        {
            await using var conexion = new SqlConnection(cadenaConexion);
            await conexion.OpenAsync();

            await using var comando = new SqlCommand(
                "SELECT IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating FROM WebReviews",
                conexion);

            await using var reader = await comando.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                comentarios.Add(new ComentarioStaging
                {
                    IdComentario = reader.GetValue(0).ToString() ?? "",
                    IdCliente = reader.IsDBNull(1) ? null : reader.GetValue(1).ToString(),
                    IdProducto = reader.GetValue(2).ToString() ?? "",
                    Fuente = "Web",
                    Fecha = reader.GetDateTime(3),
                    Texto = reader.GetValue(4).ToString() ?? "",
                    Puntuacion = reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5)),
                    Clasificacion = null
                });
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "DatabaseExtractor: error al leer la tabla WebReviews.");
            throw;
        }

        _logger.LogInformation("DatabaseExtractor: se leyeron {Cantidad} registros.", comentarios.Count);

        return comentarios;
    }
}
