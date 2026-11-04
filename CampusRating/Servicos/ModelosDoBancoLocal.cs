using SQLite;

namespace CampusRating.Servicos;

[Table("locais")]
public sealed class LocalArmazenado
{
    [PrimaryKey] public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Localizacao { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Imagem { get; set; } = "";
    public int QuantidadeComentarios { get; set; }
    public int Ordem { get; set; }
}

[Table("relatos")]
public sealed class RelatoArmazenado
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int UsuarioId { get; set; }
    public int LocalId { get; set; }
    public string LocalNome { get; set; } = "";
    public string Apelido { get; set; } = "";
    public string Texto { get; set; } = "";
    public string Status { get; set; } = "";
    public string CriadoEm { get; set; } = "";
    public string? RespostasJson { get; set; }
    public string? DetalhesAtualizadosEm { get; set; }
}

[Table("avisos")]
public sealed class AvisoArmazenado
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int UsuarioId { get; set; }
    public int RelatoId { get; set; }
    public string LocalNome { get; set; } = "";
    public string Texto { get; set; } = "";
    public string CriadoEm { get; set; } = "";
    public bool Lida { get; set; }
}

[Table("consultas_salvas")]
public sealed class ConsultaArmazenada
{
    [PrimaryKey] public string Chave { get; set; } = "";
    public int UsuarioId { get; set; }
    public string AtualizadoEm { get; set; } = "";
    public bool Completa { get; set; } = true;
    public string? ConteudoJson { get; set; }
}

public readonly record struct ContextoDoBanco(int UsuarioId, long Geracao);
public sealed record CopiaLocal<T>(T Dados, string AtualizadoEm, bool Completa = true);
