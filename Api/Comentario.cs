using System.Text.Json.Serialization;
using CsvHelper.Configuration.Attributes;

public class Comentario
{
    public string IdComment { get; set; } = "";
    public string? IdCliente { get; set; } // Puede venir vacio en el CSV.
    public string IdProducto { get; set; } = "";
    public string Fuente { get; set; } = "";
    public DateTime Fecha { get; set; }

    // La columna del CSV se llama "Comentario" y en el JSON tambien debe salir como "Comentario".
    [Name("Comentario")]
    [JsonPropertyName("comentario")]
    public string Contenido { get; set; } = "";
}
