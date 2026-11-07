using System.Globalization;
using CampusRating.Servicos;

namespace CampusRating.Modelos;

public record CampusPlace(int Id, string Name, string Category, string Location,
                          string Description, string Image, int CommentCount)
{
    public string CommentsText => CommentCount == 1 ? "1 comentário" : $"{CommentCount} comentários";
}

public record RelatoDoAluno(int Id, string LocalNome, string Texto, string Status, string CriadoEm)
{
    public string StatusTexto => FormatacaoDoRelato.Status(Status);
    public string DataTexto => FormatacaoDoRelato.Data(CriadoEm);
}

public record AvisoDoAluno(int Id, int RelatoId, string LocalNome, string Texto, string CriadoEm, bool Lida)
{
    public string LeituraTexto => Lida ? "Lido" : "Nova resposta";
    public string DataTexto => FormatacaoDoRelato.Data(CriadoEm);
}

public record ComentarioDoLocal(string Apelido, string Texto, string Status, string CriadoEm,
    List<RespostaDoRelato>? Respostas = null)
{
    public bool TemRespostas => Respostas is { Count: > 0 };
    public string RespostasTexto => string.Join("\n\n", (Respostas ?? []).Select(resposta =>
        $"Administração · {FormatacaoDoRelato.Data(resposta.CriadoEm)}\n{resposta.Texto}"));
    public string StatusTexto => Status switch
    {
        "pendente" => "Pendente",
        "em_analise" => "Em análise",
        "resolvido" => "Resolvido",
        _ => "Status indisponível"
    };

    public string DataTexto => DateTime.TryParse(CriadoEm, CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var data)
        ? data.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : CriadoEm;
}

