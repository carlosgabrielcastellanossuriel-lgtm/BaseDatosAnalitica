using System.Net.Http.Json;
using Interfaces;
using Models;

namespace Extractors;

public class ApiExtractor : IExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ApiExtractor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public string Nombre => "Api";

    public async Task<List<ComentarioStaging>> ExtraerAsync()
    {
        // La URL base se configura en appsettings.json.
        var urlBase = _configuration["ApiBaseUrl"];

        if (string.IsNullOrWhiteSpace(urlBase))
        {
            throw new InvalidOperationException("No se configuro la URL base de la API (clave 'ApiBaseUrl' en appsettings.json).");
        }

        var comentarios = new List<ComentarioStaging>();

        try
        {
            var cliente = _httpClientFactory.CreateClient();
            cliente.BaseAddress = new Uri(urlBase);

            var respuesta = await cliente.GetFromJsonAsync<List<ComentarioApi>>("/api/comentarios");

            if (respuesta is not null)
            {
                foreach (var item in respuesta)
                {
                    comentarios.Add(new ComentarioStaging
                    {
                        IdComentario = item.IdComment,
                        IdCliente = item.IdCliente,
                        IdProducto = item.IdProducto,
                        Fuente = "Red Social", // Se ignora la fuente que trae la API.
                        Fecha = item.Fecha,
                        Texto = item.Comentario,
                        Puntuacion = null,
                        Clasificacion = null
                    });
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "ApiExtractor: error al consultar la API.");
            throw;
        }

        _logger.LogInformation("ApiExtractor: se recibieron {Cantidad} registros.", comentarios.Count);

        return comentarios;
    }

    // Clase privada usada solo para deserializar la respuesta del API.
    private class ComentarioApi
    {
        public string IdComment { get; set; } = "";
        public string? IdCliente { get; set; }
        public string IdProducto { get; set; } = "";
        public string Fuente { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = "";
    }
}
