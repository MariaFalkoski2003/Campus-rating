using CampusRating.Servicos;
using CampusRating.ViewModels;
using CampusRating.Componentes;

namespace CampusRating;

public abstract class PaginaDoCampus : ContentPage
{
    protected readonly HomeViewModel _viewModel;
    protected bool _modalAberto;

    protected PaginaDoCampus(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        NavigationPage.SetTitleView(this, new CabecalhoDoCampus());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_modalAberto) return;
        if (!SessaoDoUsuario.EstaAutenticado)
        {
            await NavegacaoDoAplicativo.MostrarLoginAsync(this);
            return;
        }
        _viewModel.AtualizarPerfil();
        await AtualizarPaginaAsync();
    }

    protected abstract Task AtualizarPaginaAsync();

    protected async Task ExecutarConsultaAsync(Func<Task> consulta)
    {
        try { await consulta(); }
        catch (ExcecaoDaApi erro) { await VerificarSessaoAsync(erro); }
    }

    protected async Task<bool> VerificarSessaoAsync(ExcecaoDaApi erro)
    {
        if (Window?.Page is not PaginaPrincipal) return true;
        if (erro.Codigo is not ("token_ausente" or "token_invalido" or "usuario_nao_encontrado")) return false;
        await DisplayAlertAsync("Sessão expirada", "Entre novamente para continuar.", "Entendi");
        await NavegacaoDoAplicativo.MostrarLoginAsync(this);
        return true;
    }
}
