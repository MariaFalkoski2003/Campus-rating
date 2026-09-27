using System.Text.Json.Serialization;

namespace CampusRating.Servicos;

public sealed record UsuarioDaApi(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("apelido")] string Apelido,
    [property: JsonPropertyName("criadoEm")] string CriadoEm);

public sealed record RespostaDeAutenticacao(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("usuario")] UsuarioDaApi Usuario);

public sealed record DetalheDoErro(
    [property: JsonPropertyName("codigo")] string Codigo,
    [property: JsonPropertyName("mensagem")] string Mensagem,
    [property: JsonPropertyName("campo")] string? Campo);

public sealed record RespostaDeErro(
    [property: JsonPropertyName("erro")] DetalheDoErro Erro);

public sealed record PedidoDeRegistro(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("senha")] string Senha,
    [property: JsonPropertyName("confirmacaoSenha")] string ConfirmacaoSenha,
    [property: JsonPropertyName("apelido")] string Apelido);

public sealed record PedidoDeLogin(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("senha")] string Senha);
