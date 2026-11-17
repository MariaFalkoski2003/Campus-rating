using CampusRating.Servicos;
using CampusRating.ViewModels;
using CampusRating.Componentes;

namespace CampusRating;

public partial class AdministracaoPage : ContentPage
{
    private readonly AdministracaoViewModel _viewModel = new();
    private bool _abrindoRelato;
    private bool _saindo;

    public AdministracaoPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
        var cabecalho = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            ColumnSpacing = 12,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center
        };
        var sair = new Button
        {
            Text = "Sair",
            Style = (Style)Application.Current!.Resources["BotaoSecundario"],
            FontSize = 13,
            Padding = new Thickness(16, 6),
            MinimumHeightRequest = 44,
            VerticalOptions = LayoutOptions.Center
        };
        sair.Clicked += OnLogoutClicked;
        cabecalho.Add(new CabecalhoDoCampus("Administração"));
        cabecalho.Add(sair, 1);
        NavigationPage.SetTitleView(this, cabecalho);
        SizeChanged += (_, _) => cabecalho.WidthRequest = Math.Max(0, Width - 32);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (await NavegacaoDoAplicativo.ConferirAdministradorAsync(this)) await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        ErroAviso.Mensagem = "";
        ResumoLabel.IsVisible = true;
        try { await _viewModel.CarregarAsync(); }
        catch (ExcecaoDaApi erro)
        {
            ResumoLabel.IsVisible = false;
            ErroAviso.Mensagem = erro.Message;
            await NavegacaoDoAplicativo.TratarErroAdministrativoAsync(this, erro);
        }
    }

    private void OnFilterClicked(object? sender, EventArgs e)
    {
        if (_viewModel.Carregando || sender is not Button selecionado ||
            !int.TryParse(selecionado.CommandParameter?.ToString(), out var filtro)) return;

        _viewModel.FiltroSelecionado = filtro;
        foreach (var botao in FiltrosDeAndamento.Children.OfType<Button>())
        {
            botao.Style = (Style)Application.Current!.Resources[
                botao == selecionado ? "FiltroSelecionado" : "FiltroDoCampus"];
            SemanticProperties.SetDescription(botao,
                botao == selecionado ? $"{botao.Text}, filtro selecionado" : botao.Text);
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (_saindo) return;
        _saindo = true;
        try { await NavegacaoDoAplicativo.MostrarLoginAsync(this); }
        catch (ExcecaoDaApi erro) { await DisplayAlertAsync("Não foi possível sair", erro.Message, "Entendi"); }
        finally { _saindo = false; }
    }

    private async void OnOpenReportClicked(object? sender, EventArgs e)
    {
        if (_abrindoRelato || _viewModel.Carregando || sender is not Button { CommandParameter: int id }) return;
        _abrindoRelato = true;
        try
        {
            if (await NavegacaoDoAplicativo.ConferirAdministradorAsync(this))
                await Navigation.PushAsync(new DetalhesRelatoAdministrativoPage(id));
        }
        finally { _abrindoRelato = false; }
    }
}
