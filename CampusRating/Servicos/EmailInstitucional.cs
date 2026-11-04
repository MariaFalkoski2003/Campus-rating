namespace CampusRating.Servicos;

public static class EmailInstitucional
{
    public const string Dominio = "aluno.uricer.edu.br";

    public static string MensagemDeErro => $"Utilize seu e-mail institucional (@{Dominio}).";

    public static bool EhValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var partes = email.Trim().Split('@');

        return partes.Length == 2
            && partes[0].Length > 0
            && !partes[0].Any(char.IsWhiteSpace)
            && string.Equals(partes[1], Dominio, StringComparison.OrdinalIgnoreCase);
    }
}
