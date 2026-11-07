using CampusRating.Servicos;
using CampusRating.ViewModels;
using CampusRating.Componentes;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using Button = Microsoft.Maui.Controls.Button;
using Application = Microsoft.Maui.Controls.Application;

namespace CampusRating;

public sealed class PaginaPrincipal : FlyoutPage
{
    private readonly HomeViewModel _viewModel = new();
    private readonly Microsoft.Maui.Controls.TabbedPage _abas;
    private readonly PerfilPage _perfil;
    private bool _executandoMenu;

    public PaginaPrincipal()
    {
        Title = "Campus Rating";
        FlyoutLayoutBehavior = FlyoutLayoutBehavior.Popover;
        _perfil = new PerfilPage(_viewModel);
        _abas = new Microsoft.Maui.Controls.TabbedPage
        {
            Title = "Campus Rating", BarBackgroundColor = Colors.White, BarTextColor = Color.FromArgb("#625D73"),
            SelectedTabColor = Color.FromArgb("#6251D2"), UnselectedTabColor = Color.FromArgb("#625F6D")
        };
        _abas.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);
        _abas.Children.Add(CriarAba(new HomePage(_viewModel), "Locais", "location_purple.png"));
        _abas.Children.Add(CriarAba(_perfil, "Perfil", "profile_purple.png"));
        _abas.Children.Add(CriarAba(new AvisosPage(_viewModel), "Avisos", "bell_purple.png"));

        var menu = new VerticalStackLayout { Padding = 24, Spacing = 14 };
        menu.Add(new CabecalhoDoCampus());
        menu.Add(new Label { Text = "Navegação", FontFamily = "OpenSansRegular", FontSize = 14, TextColor = Color.FromArgb("#625D73"), Margin = new Thickness(0, 12, 0, 4) });
        AdicionarOpcao(menu, "Locais", () => SelecionarAbaAsync(0));
        AdicionarOpcao(menu, "Perfil", () => SelecionarAbaAsync(1));
        AdicionarOpcao(menu, "Avisos", () => SelecionarAbaAsync(2));
        if (SessaoDoUsuario.EhAdministrador)
            AdicionarOpcao(menu, "Administração", async () =>
            {
                await SelecionarAbaAsync(0);
                if (SessaoDoUsuario.EhAdministrador)
                    await _abas.CurrentPage.Navigation.PushAsync(new AdministracaoPage());
            });
        AdicionarOpcao(menu, "Meus relatos", async () =>
        {
            await SelecionarAbaAsync(1);
            if (SessaoDoUsuario.EstaAutenticado) await _perfil.AbrirMeusRelatosAsync();
        });
        AdicionarOpcao(menu, "Regras da plataforma", async () =>
        {
            await SelecionarAbaAsync(1);
            if (SessaoDoUsuario.EstaAutenticado) await _perfil.MostrarRegrasAsync();
        });
        AdicionarOpcao(menu, "Sair", () => NavegacaoDoAplicativo.MostrarLoginAsync(this));
        Flyout = new ContentPage
        {
            Title = "Menu", BackgroundColor = Colors.White,
            Content = new ScrollView { Content = menu }
        };
        Detail = _abas;
    }

    private static NavigationPage CriarAba(Page pagina, string titulo, string icone)
    {
        var aba = new NavigationPage(pagina)
        {
            Title = titulo, IconImageSource = icone,
            BarBackgroundColor = Color.FromArgb("#F5F1FF"), BarTextColor = Color.FromArgb("#242039")
        };
        return aba;
    }

    private async Task SelecionarAbaAsync(int indice)
    {
        IsPresented = false;
        if (!SessaoDoUsuario.EstaAutenticado)
        {
            await NavegacaoDoAplicativo.MostrarLoginAsync(this);
            return;
        }
        _abas.CurrentPage = _abas.Children[indice];
        if (_abas.CurrentPage is NavigationPage navegacao)
            await navegacao.PopToRootAsync();
    }

    private void AdicionarOpcao(VerticalStackLayout menu, string texto, Func<Task> acao)
    {
        var botao = new Button { Text = texto, Style = (Style)Application.Current!.Resources["BotaoSecundario"] };
        FeedbackDeToque.SetAtivo(botao, true);
        botao.Clicked += async (_, _) =>
        {
            if (_executandoMenu) return;
            _executandoMenu = true;
            IsPresented = false;
            try { await acao(); }
            catch (ExcecaoDaApi erro) { await DisplayAlertAsync("Não foi possível continuar", erro.Message, "Entendi"); }
            finally { _executandoMenu = false; }
        };
        menu.Add(botao);
    }

    public void LimparDados()
    {
        IsPresented = false;
        _viewModel.LimparDadosPessoais();
    }
}
