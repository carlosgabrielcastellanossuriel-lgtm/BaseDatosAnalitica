using System.Data;
using Microsoft.Data.SqlClient;

namespace Loaders;

public class CargadorDimensiones
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CargadorDimensiones> _logger;

    public CargadorDimensiones(IConfiguration configuration, ILogger<CargadorDimensiones> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CargarAsync()
    {
        // La cadena de conexion se configura en appsettings.json.
        var cadenaConexion = _configuration["CadenaConexionAnalitica"];

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException("No se configuro la cadena de conexion (clave 'CadenaConexionAnalitica' en appsettings.json).");
        }

        try
        {
            // Una sola conexion para los tres procedures.
            await using var conexion = new SqlConnection(cadenaConexion);
            await conexion.OpenAsync();

            var fuentes = await EjecutarAsync(conexion, "usp_CargarFuentes");
            var clientes = await EjecutarAsync(conexion, "usp_CargarClientes");
            var productos = await EjecutarAsync(conexion, "usp_CargarProductos");

            // Resumen con los totales de las tres tablas.
            _logger.LogInformation(
                "Resumen dimensiones: procesados {Procesados}, insertados {Insertados}, rechazados {Rechazados}.",
                fuentes.Procesados + clientes.Procesados + productos.Procesados,
                fuentes.Insertados + clientes.Insertados + productos.Insertados,
                fuentes.Rechazados + clientes.Rechazados + productos.Rechazados);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "CargadorDimensiones: error al cargar las tablas dimension.");
            throw;
        }
    }

    // Ejecuta un procedure y devuelve sus tres contadores.
    private async Task<(int Procesados, int Insertados, int Rechazados)> EjecutarAsync(
        SqlConnection conexion,
        string procedimiento)
    {
        await using var comando = new SqlCommand(procedimiento, conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await comando.ExecuteReaderAsync();

        var procesados = 0;
        var insertados = 0;
        var rechazados = 0;

        if (await reader.ReadAsync())
        {
            procesados = reader.GetInt32(0);
            insertados = reader.GetInt32(1);
            rechazados = reader.GetInt32(2);
        }

        _logger.LogInformation(
            "{Tabla}: procesados {Procesados}, insertados {Insertados}, rechazados {Rechazados}.",
            procedimiento, procesados, insertados, rechazados);

        return (procesados, insertados, rechazados);
    }
}
