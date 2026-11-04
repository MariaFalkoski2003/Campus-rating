using System.Globalization;

namespace CampusRating.Servicos;

public static class FormatacaoDoRelato
{
    public static string Status(string status) => status switch
    {
        "pendente" => "Pendente", "em_analise" => "Em análise", "resolvido" => "Resolvido", _ => "Status indisponível"
    };

    public static string Data(string valor) => DateTime.TryParse(valor, CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var data)
        ? data.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : valor;
}
