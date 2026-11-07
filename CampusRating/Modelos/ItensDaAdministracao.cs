using CampusRating.Servicos;

namespace CampusRating.Modelos;

public sealed record RelatoAdministrativo(int Id, string LocalNome, string Apelido, string Texto, string Status, string CriadoEm)
{
    public string StatusTexto => FormatacaoDoRelato.Status(Status);
    public string DataTexto => FormatacaoDoRelato.Data(CriadoEm);
}

public sealed record RespostaAdministrativa(string Texto, string CriadoEm)
{
    public string DataTexto => FormatacaoDoRelato.Data(CriadoEm);
}
