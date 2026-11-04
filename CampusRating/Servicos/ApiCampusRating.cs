using System.Net.Http;
using System.Net.Http.Headers;
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

    public Task<RespostaDeAutenticacao> RegistrarAsync(string email, string senha, string confirmacaoSenha, string apelido, bool aceitouRegras)
        => EnviarAutenticacaoAsync("/auth/registro", new PedidoDeRegistro(email, senha, confirmacaoSenha, apelido, aceitouRegras));

    public Task<RespostaDeAutenticacao> EntrarAsync(string email, string senha)
        => EnviarAutenticacaoAsync("/auth/login", new PedidoDeLogin(email, senha));

    public Task<RespostaDeAutenticacao> AlterarEmailAsync(string email, string senhaAtual)
        => EnviarComSessaoAsync<RespostaDeAutenticacao>(HttpMethod.Patch, "/auth/eu/email", new { email, senhaAtual });

    public Task<RespostaDeAutenticacao> AlterarSenhaAsync(string senhaAtual, string senha, string confirmacaoSenha)
        => EnviarComSessaoAsync<RespostaDeAutenticacao>(HttpMethod.Patch, "/auth/eu/senha", new { senhaAtual, senha, confirmacaoSenha });

    public Task<MensagemDaApi> ExcluirContaAsync(string senhaAtual)
        => EnviarComSessaoAsync<MensagemDaApi>(HttpMethod.Delete, "/auth/eu", new { senhaAtual });

    public Task<RespostaDeRelato> EnviarRelatoAsync(int localId, string texto)
        => EnviarComSessaoAsync<RespostaDeRelato>(HttpMethod.Post, $"/locais/{localId}/relatos", new { texto });

    public Task<RespostaDeMeusRelatos> ListarMeusRelatosAsync()
        => EnviarComSessaoAsync<RespostaDeMeusRelatos>(HttpMethod.Get, "/relatos/meus");

    public Task<RespostaDeRelato> ObterMeuRelatoAsync(int relatoId)
        => EnviarComSessaoAsync<RespostaDeRelato>(HttpMethod.Get, $"/relatos/{relatoId}");

    public Task<RespostaDeAvisos> ListarAvisosAsync()
        => EnviarComSessaoAsync<RespostaDeAvisos>(HttpMethod.Get, "/avisos");

    public Task<MensagemDaApi> MarcarAvisoLidoAsync(int avisoId)
        => EnviarComSessaoAsync<MensagemDaApi>(HttpMethod.Patch, $"/avisos/{avisoId}/lido");

    private async Task<T> EnviarComSessaoAsync<T>(HttpMethod metodo, string caminho, object? corpo = null) where T : class
    {
        if (!SessaoDoUsuario.EstaAutenticado)
            throw new ExcecaoDaApi("token_ausente", "Sua sessão expirou. Entre novamente.");
        using var pedido = new HttpRequestMessage(metodo, caminho);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SessaoDoUsuario.Token);
        if (corpo is not null) pedido.Content = JsonContent.Create(corpo);
        return await EnviarAsync<T>(pedido);
    }

    public async Task<IReadOnlyList<LocalDaApi>> ListarLocaisAsync()
    {
        var token = SessaoDoUsuario.Token;

        if (!SessaoDoUsuario.EstaAutenticado)
        {
            throw new ExcecaoDaApi("token_ausente", "Sua sessão expirou. Entre novamente.");
        }

        using var pedido = new HttpRequestMessage(HttpMethod.Get, "/locais");
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resposta = await EnviarAsync<RespostaDeLocais>(pedido);
        return resposta.Locais
               ?? throw new ExcecaoDaApi("resposta_invalida", "Não foi possível carregar os locais. Tente novamente.");
    }

    public async Task<RespostaDeDetalhesDoLocal> ObterDetalhesDoLocalAsync(int localId)
    {
        if (localId <= 0)
        {
            throw new ExcecaoDaApi("local_invalido", "Selecione um local válido.");
        }

        var token = SessaoDoUsuario.Token;
        if (!SessaoDoUsuario.EstaAutenticado)
        {
            throw new ExcecaoDaApi("token_ausente", "Sua sessão expirou. Entre novamente.");
        }

        using var pedido = new HttpRequestMessage(HttpMethod.Get, $"/locais/{localId}");
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resposta = await EnviarAsync<RespostaDeDetalhesDoLocal>(pedido);

        if (resposta.Local is null || resposta.Comentarios is null)
        {
            throw new ExcecaoDaApi("resposta_invalida", "Não foi possível carregar os detalhes do local. Tente novamente.");
        }

        return resposta;
    }

    private async Task<RespostaDeAutenticacao> EnviarAutenticacaoAsync<TPedido>(string caminho, TPedido pedido)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, caminho)
        {
            Content = JsonContent.Create(pedido)
        };

        var autenticacao = await EnviarAsync<RespostaDeAutenticacao>(requisicao);

        if (string.IsNullOrWhiteSpace(autenticacao.Token) || autenticacao.Usuario is null)
        {
            throw new ExcecaoDaApi("resposta_invalida", "O servidor respondeu em um formato inesperado. Tente novamente.");
        }

        return autenticacao;
    }

    private async Task<TResposta> EnviarAsync<TResposta>(HttpRequestMessage pedido) where TResposta : class
    {
        try
        {
            using var resposta = await _cliente.SendAsync(pedido);
            var corpo = await resposta.Content.ReadAsStringAsync();
            ConferirSessaoDoPedido(pedido);

            if (!resposta.IsSuccessStatusCode)
            {
                throw InterpretarErro(corpo);
            }

            return JsonSerializer.Deserialize<TResposta>(corpo)
                   ?? throw new ExcecaoDaApi("resposta_invalida", "O servidor respondeu em um formato inesperado. Tente novamente.");
        }
        catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException)
        {
            ConferirSessaoDoPedido(pedido);
            throw ExcecaoDaApi.SemConexao(erro);
        }
        catch (JsonException erro)
        {
            throw new ExcecaoDaApi(
                "resposta_invalida",
                "O servidor respondeu em um formato inesperado. Tente novamente.",
                origem: erro);
        }
    }

    private static void ConferirSessaoDoPedido(HttpRequestMessage pedido)
    {
        if (pedido.Headers.Authorization is not { Parameter: { } token }) return;
        if (token != SessaoDoUsuario.Token)
            throw new ExcecaoDaApi("consulta_cancelada", "A conta mudou durante a consulta. Atualize a tela.");
        if (!SessaoDoUsuario.EstaAutenticado)
            throw new ExcecaoDaApi("token_ausente", "Sua sessão expirou. Entre novamente.");
    }

    private static ExcecaoDaApi InterpretarErro(string corpo)
    {
        try
        {
            var erro = JsonSerializer.Deserialize<RespostaDeErro>(corpo)?.Erro;

            if (erro is not null && !string.IsNullOrWhiteSpace(erro.Mensagem))
            {
                return new ExcecaoDaApi(erro.Codigo, erro.Mensagem, erro.Campo);
            }
        }
        catch (JsonException)
        {
        }

        return new ExcecaoDaApi("erro_desconhecido", "Não foi possível concluir a operação. Tente novamente.");
    }
}
