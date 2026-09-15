using System.Security.Cryptography;

namespace JcaInventario.Helpers;

public static class SenhaHelper
{
    public static string GerarHash(string senha)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var resumo = Rfc2898DeriveBytes.Pbkdf2(senha, sal, 600000, HashAlgorithmName.SHA256, 32);
        return $"600000.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(resumo)}";
    }

    public static bool Verificar(string senha, string hash)
    {
        try
        {
            var partes = hash.Split('.');
            var calculado = Rfc2898DeriveBytes.Pbkdf2(
                senha,
                Convert.FromBase64String(partes[1]),
                int.Parse(partes[0]),
                HashAlgorithmName.SHA256,
                32);
            return CryptographicOperations.FixedTimeEquals(calculado, Convert.FromBase64String(partes[2]));
        }
        catch
        {
            return false;
        }
    }
}
