using System.Text.Json.Serialization;

namespace CampusRating.Servicos;

public sealed record UsuarioDaApi(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("apelido")] string Apelido,
    [property: JsonPropertyName("criadoEm")] string CriadoEm,
    [property: JsonPropertyName("papel")] string Papel = "aluno");

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
    [property: JsonPropertyName("apelido")] string Apelido,
    [property: JsonPropertyName("aceitouRegras")] bool AceitouRegras);

public sealed record PedidoDeLogin(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("senha")] string Senha);

public sealed record LocalDaApi(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("categoria")] string Categoria,
    [property: JsonPropertyName("localizacao")] string Localizacao,
    [property: JsonPropertyName("descricao")] string Descricao,
    [property: JsonPropertyName("imagem")] string Imagem,
    [property: JsonPropertyName("quantidadeComentarios")] int QuantidadeComentarios);

public sealed record RespostaDeLocais(
    [property: JsonPropertyName("locais")] List<LocalDaApi> Locais);

public sealed record ComentarioDaApi(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("apelido")] string Apelido,
    [property: JsonPropertyName("texto")] string Texto,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("criadoEm")] string CriadoEm,
    [property: JsonPropertyName("respostas")] List<RespostaDoRelato>? Respostas = null);

public sealed record RespostaDeDetalhesDoLocal(
    [property: JsonPropertyName("local")] LocalDaApi Local,
    [property: JsonPropertyName("comentarios")] List<ComentarioDaApi> Comentarios);

public sealed record RespostaDoRelato(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("texto")] string Texto,
    [property: JsonPropertyName("criadoEm")] string CriadoEm);

public sealed record RelatoDaApi(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("localId")] int LocalId,
    [property: JsonPropertyName("localNome")] string LocalNome,
    [property: JsonPropertyName("apelido")] string Apelido,
    [property: JsonPropertyName("texto")] string Texto,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("criadoEm")] string CriadoEm,
    [property: JsonPropertyName("respostas")] List<RespostaDoRelato>? Respostas = null);

public sealed record RespostaDeRelato([property: JsonPropertyName("relato")] RelatoDaApi Relato);
public sealed record RespostaDeMeusRelatos([property: JsonPropertyName("relatos")] List<RelatoDaApi> Relatos);
public sealed record AvisoDaApi(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("relatoId")] int RelatoId,
    [property: JsonPropertyName("localNome")] string LocalNome,
    [property: JsonPropertyName("texto")] string Texto,
    [property: JsonPropertyName("criadoEm")] string CriadoEm,
    [property: JsonPropertyName("lida")] bool Lida);
public sealed record RespostaDeAvisos([property: JsonPropertyName("avisos")] List<AvisoDaApi> Avisos);
public sealed record MensagemDaApi([property: JsonPropertyName("mensagem")] string Mensagem);
