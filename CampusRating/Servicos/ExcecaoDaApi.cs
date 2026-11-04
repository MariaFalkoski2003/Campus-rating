namespace CampusRating.Servicos;

public sealed class ExcecaoDaApi : Exception
{
    public ExcecaoDaApi(string codigo, string mensagem, string? campo = null, Exception? origem = null)
        : base(mensagem, origem)
    {
        Codigo = codigo;
        Campo = campo;
    }

    public string Codigo { get; }

    public string? Campo { get; }

    public static ExcecaoDaApi SemConexao(Exception origem) => new(
        "sem_conexao",
        "Não foi possível conectar ao servidor. Tente novamente.",
        origem: origem);
}
