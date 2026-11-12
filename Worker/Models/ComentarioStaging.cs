namespace Models;

public class ComentarioStaging
{
    public string IdComentario { get; set; } = "";
    public string? IdCliente { get; set; }
    public string IdProducto { get; set; } = "";
    public string Fuente { get; set; } = ""; // "Encuesta", "Web" o "Red Social".
    public DateTime Fecha { get; set; }
    public string Texto { get; set; } = "";
    public int? Puntuacion { get; set; }
    public string? Clasificacion { get; set; }
}
