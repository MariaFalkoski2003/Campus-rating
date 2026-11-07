using CampusRating.Servicos;
using CampusRating.ViewModels;
using CampusRating.Componentes;

namespace CampusRating;

public partial class DetalhesLocalPage : ContentPage
{
    private readonly DetalhesLocalViewModel _viewModel = new();
    private bool _enviandoRelato;

    public DetalhesLocalPage(int localId)
    {
        InitializeComponent();
        NavigationPage.SetTitleView(this, new CabecalhoDoCampus("Detalhes do local"));
        BindingContext = _viewModel;
        _viewModel.LocalId = localId;
    }



    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_enviandoRelato) return;
        if (!SessaoDoUsuario.EstaAutenticado)
        {
            await NavegacaoDoAplicativo.MostrarLoginAsync(this);
            return;
        }

        await CarregarLocalAsync();
    }

    private async Task CarregarLocalAsync()
    {
        try { await _viewModel.CarregarLocalAsync(); }
        catch (ExcecaoDaApi erro)
        {
            if (Window?.Page is not PaginaPrincipal) return;
            if (erro.Codigo is "token_ausente" or "token_invalido" or "usuario_nao_encontrado")
            {
                await DisplayAlertAsync("Sessão expirada", "Entre novamente para consultar o local.", "Entendi");
                await NavegacaoDoAplicativo.MostrarLoginAsync(this);
                return;
            }

        }
    }

    private async void OnRetryClicked(object? sender, EventArgs e) => await CarregarLocalAsync();

    private async void OnSendReportClicked(object? sender, EventArgs e)
    {
        if (_enviandoRelato) return;
        _enviandoRelato = true;
        try
        {
            var avisoLocal = "";
            var enviou = await ModaisDaPlataforma.FormularioAsync(this, "Enviar relato",
                string.Empty,
                [new("texto", "Descreva a situação", Multilinha: true)],
                async campos =>
                {
                    avisoLocal = await _viewModel.EnviarRelatoAsync(campos["texto"]);
                }, "Enviar relato");
            if (enviou)
            {
                await MensagensDaInterface.SnackbarAsync("Relato enviado e registrado como Pendente." +
                    (avisoLocal.Length > 0 ? "\n" + avisoLocal : ""));
                await CarregarLocalAsync();
            }
        }
        finally { _enviandoRelato = false; }
    }

}
