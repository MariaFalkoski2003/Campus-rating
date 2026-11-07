using CampusRating.Modelos;
using CampusRating.Servicos;
using CampusRating.ViewModels;

namespace CampusRating;

public partial class HomePage : PaginaDoCampus
{
    private bool _abrindoLocal;
    public HomePage(HomeViewModel viewModel) : base(viewModel) { InitializeComponent(); }
    protected override Task AtualizarPaginaAsync() => CarregarLocaisAsync();

    private Task CarregarLocaisAsync() => ExecutarConsultaAsync(_viewModel.CarregarLocaisAsync);

    private void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button selected) return;
        _viewModel.SelecionarCategoria(selected.CommandParameter?.ToString() ?? "Todos");
        foreach (var button in CategoryButtons.Children.OfType<Button>())
        {
            button.Style = (Style)Application.Current!.Resources[button == selected ? "FiltroSelecionado" : "FiltroDoCampus"];
        }
    }

    private async void OnPlaceTapped(object? sender, TappedEventArgs e)
    {
        if (_abrindoLocal || sender is not BindableObject { BindingContext: CampusPlace place }) return;

        _abrindoLocal = true;
        try
        {
            await FeedbackDeToque.AnimarToqueAsync(sender);
            await Navigation.PushAsync(new DetalhesLocalPage(place.Id));
        }
        finally
        {
            _abrindoLocal = false;
        }
    }
    private async void OnRetryPlacesClicked(object? sender, EventArgs e) => await CarregarLocaisAsync();
}
