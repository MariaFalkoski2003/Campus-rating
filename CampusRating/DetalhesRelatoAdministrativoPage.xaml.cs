using CampusRating.Servicos;
using CampusRating.ViewModels;
using CampusRating.Componentes;

namespace CampusRating;

public partial class DetalhesRelatoAdministrativoPage : ContentPage
{
    private readonly DetalhesRelatoAdministrativoViewModel _viewModel;

    public DetalhesRelatoAdministrativoPage(int relatoId)
    {
        InitializeComponent();
        NavigationPage.SetTitleView(this, new CabecalhoDoCampus("Gerenciar relato"));
        _viewModel = new(relatoId);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (await NavegacaoDoAplicativo.ConferirAdministradorAsync(this)) await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        AvisoResultado.Tom = TomDoAviso.Informacao;
        AvisoResultado.Titulo = "";
        try { await _viewModel.CarregarAsync(); }
        catch (ExcecaoDaApi erro)
        {
            AvisoResultado.Tom = TomDoAviso.Erro;
            AvisoResultado.Titulo = "Não foi possível carregar o relato";
            await NavegacaoDoAplicativo.TratarErroAdministrativoAsync(this, erro);
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.PodeSalvar) return;
        try
        {
            await _viewModel.SalvarAsync();
            AvisoResultado.Tom = TomDoAviso.Sucesso;
            AvisoResultado.Titulo = "Alterações salvas";
            await MensagensDaInterface.SnackbarAsync("Alterações do relato salvas.");
        }
        catch (ExcecaoDaApi erro)
        {
            AvisoResultado.Tom = TomDoAviso.Erro;
            AvisoResultado.Titulo = "Não foi possível salvar";
            if (!await NavegacaoDoAplicativo.TratarErroAdministrativoAsync(this, erro))
            {
                await DisplayAlertAsync("Não foi possível salvar", erro.Message, "Entendi");
                if (erro.Campo == "texto") ResponseEditor.Focus();
            }
        }
    }
}
