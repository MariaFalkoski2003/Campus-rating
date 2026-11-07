using System.Text;
using System.Text.Json;

namespace CampusRating.Servicos;

public static class SessaoDoUsuario
{
    private const string Chave = "campusrating.sessao";
    private static readonly SemaphoreSlim Trava = new(1, 1);
    private static bool _restaurada;
    public static string? Token { get; private set; }
    public static UsuarioDaApi? Usuario { get; private set; }

    public static bool EstaAutenticado => !string.IsNullOrEmpty(Token) && TokenNoPrazo(Token);
    public static bool EhAdministrador => EstaAutenticado && Usuario?.Papel == "administrador";

    public static async Task IniciarAsync(RespostaDeAutenticacao resposta)
    {
        await Trava.WaitAsync();
        try
        {
            ServicosLocais.Banco.AtivarUsuario(resposta.Usuario.Id);
            Token = resposta.Token;
            Usuario = resposta.Usuario;
            _restaurada = true;
            try { await SecureStorage.Default.SetAsync(Chave, JsonSerializer.Serialize(resposta)); }
            catch (Exception erro)
            {
                System.Diagnostics.Debug.WriteLine($"Sessão não persistida: {erro.GetType().Name}");
                RemoverSessaoSalva();
            }
        }
        finally { Trava.Release(); }
    }

    public static async Task RestaurarAsync()
    {
        await Trava.WaitAsync();
        try
        {
            if (_restaurada) return;
            _restaurada = true;
            RespostaDeAutenticacao? resposta = null;
            try
            {
                var json = await SecureStorage.Default.GetAsync(Chave);
                if (json is not null) resposta = JsonSerializer.Deserialize<RespostaDeAutenticacao>(json);
            }
            catch (Exception erro) { System.Diagnostics.Debug.WriteLine($"Sessão não restaurada: {erro.GetType().Name}"); }
            if (resposta?.Usuario is { Id: > 0 } && TokenNoPrazo(resposta.Token)
                && ServicosLocais.Banco.PertenceAoUsuario(resposta.Usuario.Id))
            {
                ServicosLocais.Banco.AtivarUsuario(resposta.Usuario.Id);
                Token = resposta.Token;
                Usuario = resposta.Usuario;
            }
            else
            {
                Token = null;
                Usuario = null;
                RemoverSessaoSalva();
                ServicosLocais.Banco.EncerrarSessao();
            }
        }
        finally { Trava.Release(); }
    }

    // A API continua validando assinatura, autorização e revogação a cada requisição.
    private static bool TokenNoPrazo(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var partes = token.Split('.');
            if (partes.Length != 3) return false;
            var payload = partes[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return json.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var segundos)
                && segundos > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        catch (Exception erro) when (erro is FormatException or JsonException or ArgumentException) { return false; }
    }

    public static async Task EncerrarAsync()
    {
        await Trava.WaitAsync();
        try
        {
            Token = null;
            Usuario = null;
            _restaurada = true;
            RemoverSessaoSalva();
            ServicosLocais.Banco.EncerrarSessao();
        }
        finally { Trava.Release(); }
    }

    private static void RemoverSessaoSalva()
    {
        try { SecureStorage.Default.Remove(Chave); }
        catch (Exception erro) { System.Diagnostics.Debug.WriteLine($"Sessão não removida: {erro.GetType().Name}"); }
    }
}
