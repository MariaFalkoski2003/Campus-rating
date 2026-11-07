using Microsoft.Maui.Controls.Shapes;

namespace CampusRating.Componentes;

public enum TomDoAviso { Informacao, Erro, Sucesso }

public sealed class AvisoDaInterface : Border
{
    public static readonly BindableProperty MensagemProperty = BindableProperty.Create(
        nameof(Mensagem), typeof(string), typeof(AvisoDaInterface), "", propertyChanged: AtualizarAviso);
    public static readonly BindableProperty TituloProperty = BindableProperty.Create(
        nameof(Titulo), typeof(string), typeof(AvisoDaInterface), "", propertyChanged: AtualizarAviso);
    public static readonly BindableProperty TomProperty = BindableProperty.Create(
        nameof(Tom), typeof(TomDoAviso), typeof(AvisoDaInterface), TomDoAviso.Informacao, propertyChanged: AtualizarAviso);

    private readonly Label _titulo = new() { FontFamily = "OpenSansSemibold", FontSize = 14 };
    private readonly Label _mensagem = new() { FontFamily = "OpenSansRegular", FontSize = 13, LineHeight = 1.3 };
    private readonly Image _icone = new() { WidthRequest = 20, HeightRequest = 20, VerticalOptions = LayoutOptions.Start };

    public string Mensagem { get => (string)GetValue(MensagemProperty); set => SetValue(MensagemProperty, value); }
    public string Titulo { get => (string)GetValue(TituloProperty); set => SetValue(TituloProperty, value); }
    public TomDoAviso Tom { get => (TomDoAviso)GetValue(TomProperty); set => SetValue(TomProperty, value); }

    public AvisoDaInterface()
    {
        Padding = 12;
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 12 };
        var texto = new VerticalStackLayout { Spacing = 4, Children = { _titulo, _mensagem } };
        var conteudo = new Grid { ColumnDefinitions = [new(28), new(GridLength.Star)], ColumnSpacing = 4 };
        conteudo.Add(_icone);
        conteudo.Add(texto, 1);
        Content = conteudo;
        Atualizar();
    }

    private static void AtualizarAviso(BindableObject elemento, object anterior, object atual) =>
        ((AvisoDaInterface)elemento).Atualizar();

    private void Atualizar()
    {
        var (fundo, texto, icone) = Tom switch
        {
            TomDoAviso.Erro => ("#FFF0F1", "#9D2539", "campus_warning.png"),
            TomDoAviso.Sucesso => ("#ECF8EF", "#24643D", "campus_success.png"),
            _ => ("#F0EBFC", "#5140B5", "campus_info.png")
        };
        BackgroundColor = Color.FromArgb(fundo);
        _titulo.TextColor = _mensagem.TextColor = Color.FromArgb(texto);
        _titulo.Text = Titulo;
        _titulo.IsVisible = !string.IsNullOrWhiteSpace(Titulo);
        _mensagem.Text = Mensagem;
        _icone.Source = icone;
        IsVisible = !string.IsNullOrWhiteSpace(Mensagem);
    }
}
