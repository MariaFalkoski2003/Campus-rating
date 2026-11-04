using System.ComponentModel;

namespace CampusRating;

public static class FeedbackDeToque
{
    public static readonly BindableProperty AtivoProperty = BindableProperty.CreateAttached(
        "Ativo", typeof(bool), typeof(FeedbackDeToque), false, propertyChanged: OnAtivoChanged);

    public static bool GetAtivo(BindableObject elemento) => (bool)elemento.GetValue(AtivoProperty);

    public static void SetAtivo(BindableObject elemento, bool valor) => elemento.SetValue(AtivoProperty, valor);

    private static void OnAtivoChanged(BindableObject elemento, object valorAnterior, object novoValor)
    {
        var ativo = (bool)novoValor;
        if (elemento is Button botao)
        {
            botao.Pressed -= OnPressed;
            botao.Released -= OnReleased;
            if (ativo)
            {
                botao.Pressed += OnPressed;
                botao.Released += OnReleased;
            }
        }
        else if (elemento is ImageButton imagem)
        {
            imagem.Pressed -= OnPressed;
            imagem.Released -= OnReleased;
            if (ativo)
            {
                imagem.Pressed += OnPressed;
                imagem.Released += OnReleased;
            }
        }
        else
        {
            return;
        }

        elemento.PropertyChanged -= OnPropertyChanged;
        if (ativo) elemento.PropertyChanged += OnPropertyChanged;
        else if (elemento is VisualElement visual) Restaurar(visual);
    }

    private static async void OnPressed(object? sender, EventArgs e)
    {
        if (sender is not VisualElement { IsEnabled: true } elemento) return;
        var alvo = AlvoDaAnimacao(elemento);
        alvo.CancelAnimations();
        await alvo.ScaleToAsync(0.95, 60, Easing.CubicOut);
    }

    private static async void OnReleased(object? sender, EventArgs e)
    {
        if (sender is not VisualElement elemento) return;
        var alvo = AlvoDaAnimacao(elemento);
        alvo.CancelAnimations();
        await alvo.ScaleToAsync(1, 100, Easing.CubicOut);
    }

    private static void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName
            && sender is VisualElement { IsEnabled: false } elemento)
        {
            Restaurar(elemento);
        }
    }

    private static void Restaurar(VisualElement elemento)
    {
        var alvo = AlvoDaAnimacao(elemento);
        alvo.CancelAnimations();
        alvo.Scale = 1;
    }

    private static VisualElement AlvoDaAnimacao(VisualElement elemento) =>
        elemento.Parent is Border borda && ReferenceEquals(borda.Content, elemento) ? borda : elemento;

    public static async Task AnimarToqueAsync(object? origem)
    {
        if (origem is not VisualElement { IsEnabled: true } elemento) return;
        elemento.CancelAnimations();
        var cancelada = await elemento.ScaleToAsync(0.97, 60, Easing.CubicOut);
        if (!cancelada) await elemento.ScaleToAsync(1, 100, Easing.CubicOut);
    }
}
