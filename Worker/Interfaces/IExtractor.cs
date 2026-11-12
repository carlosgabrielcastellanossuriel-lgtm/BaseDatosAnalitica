using Models;

namespace Interfaces;

public interface IExtractor
{
    string Nombre { get; }
    Task<List<ComentarioStaging>> ExtraerAsync();
}
