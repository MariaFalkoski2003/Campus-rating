using CampusRating.Servicos;
using Microsoft.Maui.Controls.Shapes;

namespace CampusRating.Componentes;

public sealed class SeloDeStatus : Border
{
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(string), typeof(SeloDeStatus), "pendente",
        propertyChanged: (elemento, _, _) => ((SeloDeStatus)elemento).Atualizar());
    private readonly Label _texto = new() { FontSize = 12, FontFamily = "OpenSansRegular" };
    public string Status { get => (string)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    public SeloDeStatus()
    {
        Padding = new Thickness(10, 5);
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 12 };
        HorizontalOptions = LayoutOptions.Start;
        Content = _texto;
        Atualizar();
    }

    private void Atualizar()
    {
        var (fundo, texto) = Status switch
        {
            "em_analise" => ("#FFF0CB", "#79500B"),
            "resolvido" => ("#E6F5E9", "#24643D"),
            _ => ("#EEE8FC", "#5140B5")
        };
        BackgroundColor = Color.FromArgb(fundo);
        _texto.TextColor = Color.FromArgb(texto);
        _texto.Text = FormatacaoDoRelato.Status(Status);
    }
}
