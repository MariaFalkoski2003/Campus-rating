using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusRating.Servicos;

public sealed class ApiCampusRating
{
    private static readonly Lazy<ApiCampusRating> _instancia = new(() => new ApiCampusRating());

    private readonly HttpClient _cliente;

    private ApiCampusRating()
    {
        _cliente = new HttpClient
        {
            BaseAddress = new Uri(ConfiguracaoDaApi.EnderecoBase),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public static ApiCampusRating Instancia => _instancia.Value;

    public Task<RespostaDeAutenticacao> RegistrarAsync(string email, string senha, string confirmacaoSenha, string apelido)
        => EnviarAsync("/auth/registro", new PedidoDeRegistro(email, senha, confirmacaoSenha, apelido));

    public Task<RespostaDeAutenticacao> EntrarAsync(string email, string senha)
        => EnviarAsync("/auth/login", new PedidoDeLogin(email, senha));

    private async Task<RespostaDeAutenticacao> EnviarAsync<TPedido>(string caminho, TPedido pedido)
    {
        HttpResponseMessage resposta;

        try
        {
            resposta = await _cliente.PostAsJsonAsync(caminho, pedido);
        }
        catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException)
        {
            throw ExcecaoDaApi.SemConexao(erro);
        }

        var corpo = await resposta.Content.ReadAsStringAsync();

        if (!resposta.IsSuccessStatusCode)
        {
            throw InterpretarErro(corpo);
        }

        return JsonSerializer.Deserialize<RespostaDeAutenticacao>(corpo)
               ?? throw new ExcecaoDaApi("resposta_invalida", "O servidor respondeu em um formato inesperado.");
    }

    private static ExcecaoDaApi InterpretarErro(string corpo)
    {
        try
        {
            var erro = JsonSerializer.Deserialize<RespostaDeErro>(corpo)?.Erro;

            if (erro is not null)
            {
                return new ExcecaoDaApi(erro.Codigo, erro.Mensagem, erro.Campo);
            }
        }
        catch (JsonException)
        {
        }

        return new ExcecaoDaApi("erro_desconhecido", "Nao foi possivel concluir a operacao. Tente novamente.");
    }
}
