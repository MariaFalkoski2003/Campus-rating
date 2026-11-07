using CampusRating.Modelos;
using CampusRating.Servicos;
using CampusRating.ViewModels;
using CampusRating.Componentes;

namespace CampusRating;

public partial class AvisosPage : PaginaDoCampus
{
    public AvisosPage(HomeViewModel viewModel) : base(viewModel) { InitializeComponent(); }
    protected override Task AtualizarPaginaAsync() => CarregarAvisosAsync();

    private async Task CarregarAvisosAsync()
    {
        AvisosMensagem.Tom = TomDoAviso.Informacao;
        try { await _viewModel.CarregarAvisosAsync(); }
        catch (ExcecaoDaApi erro)
        {
            AvisosMensagem.Tom = TomDoAviso.Erro;
            await VerificarSessaoAsync(erro);
        }
    }

    private async void OnNoticeClicked(object? sender, EventArgs e)
    {
        if (_modalAberto || sender is not BindableObject { BindingContext: AvisoDoAluno aviso }) return;
        _modalAberto = true;
        try
        {
            await FeedbackDeToque.AnimarToqueAsync(sender);
            var consulta = await _viewModel.ObterRelatoAsync(aviso.RelatoId);
            var marcou = !consulta.Offline && await _viewModel.MarcarAvisoLidoAsync(aviso.Id);
            await CarregarAvisosAsync();
            await ModaisDaPlataforma.MostrarRelatoAsync(this, consulta.Dados.Relato,
                consulta.Mensagem + (marcou ? "" : "\nA leitura do aviso só será registrada quando houver conexão."));
        }
        catch (ExcecaoDaApi erro) { if (!await VerificarSessaoAsync(erro)) await DisplayAlertAsync("Não foi possível abrir", erro.Message, "Entendi"); }
        finally { _modalAberto = false; }
    }
    private async void OnRefreshNoticesClicked(object? sender, EventArgs e) => await CarregarAvisosAsync();
}
