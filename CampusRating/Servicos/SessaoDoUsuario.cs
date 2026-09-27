namespace CampusRating.Servicos;

public static class SessaoDoUsuario
{
    public static string? Token { get; private set; }

    public static UsuarioDaApi? Usuario { get; private set; }

    public static bool EstaAutenticado => !string.IsNullOrEmpty(Token);

    public static void Iniciar(RespostaDeAutenticacao resposta)
    {
        Token = resposta.Token;
        Usuario = resposta.Usuario;
    }

    public static void Encerrar()
    {
        Token = null;
        Usuario = null;
    }
}
