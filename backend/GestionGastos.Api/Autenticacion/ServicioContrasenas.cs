namespace GestionGastos.Api.Autenticacion;

/// <summary>Hashea y verifica contrasenas (RNF-03).</summary>
public interface IServicioContrasenas
{
    string Hashear(string contrasena);

    bool Verificar(string contrasena, string hash);
}

/// <summary>
/// Implementacion con bcrypt. El factor de trabajo 12 es el equilibrio habitual entre
/// costo de login y resistencia a fuerza bruta; subirlo encarece ambos.
/// </summary>
public class ServicioContrasenasBCrypt : IServicioContrasenas
{
    private const int FactorDeTrabajo = 12;

    public string Hashear(string contrasena) =>
        BCrypt.Net.BCrypt.HashPassword(contrasena, FactorDeTrabajo);

    public bool Verificar(string contrasena, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(contrasena, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash corrupto o con formato desconocido: se trata como credencial invalida.
            return false;
        }
    }
}
