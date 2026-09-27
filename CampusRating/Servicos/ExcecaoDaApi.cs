namespace CampusRating.Servicos;

public sealed class ExcecaoDaApi : Exception
{
    public ExcecaoDaApi(string codigo, string mensagem, string? campo = null)
        : base(mensagem)
    {
        Codigo = codigo;
        Campo = campo;
    }

    public string Codigo { get; }

    public string? Campo { get; }

    public static ExcecaoDaApi SemConexao(Exception origem) => new(
        "sem_conexao",
        $"Nao foi possivel falar com o servidor em {ConfiguracaoDaApi.EnderecoBase}. Verifique se a API esta rodando.",
        null);
}
