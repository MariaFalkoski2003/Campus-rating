using CampusRating.Modelos;
using CampusRating.Servicos;
using CampusRating.ViewModels;
using CampusRating.Componentes;

namespace CampusRating;

public partial class PerfilPage : PaginaDoCampus
{
    private bool _interfacePronta;
    public PerfilPage(HomeViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
        _interfacePronta = true;
    }
    protected override Task AtualizarPaginaAsync() =>
        MyReportsPanel.IsVisible ? CarregarMeusRelatosAsync() : Task.CompletedTask;
    private void AtualizarPerfil() => _viewModel.AtualizarPerfil();
    public async Task AbrirMeusRelatosAsync()
    {
        MyReportsPanel.IsVisible = true;
        await CarregarMeusRelatosAsync();
    }
    public async Task MostrarRegrasAsync()
    {
        if (_modalAberto) return;
        _modalAberto = true;
        try { await ModaisDaPlataforma.MostrarRegrasAsync(this); }
        finally { _modalAberto = false; }
    }
    private async void OnRulesClicked(object? sender, EventArgs e) => await MostrarRegrasAsync();
    private async void OnLogoutClicked(object? sender, EventArgs e) => await NavegacaoDoAplicativo.MostrarLoginAsync(this);

    private async void OnEditEmailClicked(object? sender, EventArgs e)
    {
        if (_modalAberto) return;
        _modalAberto = true;
        try
        {
            var salvou = await ModaisDaPlataforma.FormularioAsync(this, "Editar e-mail",
                "Use seu e-mail @aluno.uricer.edu.br. Seu apelido permanece o mesmo.",
                [new("email", "Novo e-mail institucional", Valor: SessaoDoUsuario.Usuario?.Email ?? "", Teclado: Keyboard.Email),
                 new("senhaAtual", "Senha atual", Senha: true)],
                campos => _viewModel.AlterarEmailAsync(campos["email"], campos["senhaAtual"]));
            if (salvou)
            {
                AtualizarPerfil();
                await MensagensDaInterface.ToastAsync("E-mail atualizado. Use o novo e-mail no próximo acesso.");
            }
        }
        finally { _modalAberto = false; }
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        if (_modalAberto) return;
        _modalAberto = true;
        try
        {
            var salvou = await ModaisDaPlataforma.FormularioAsync(this, "Alterar senha",
                "Confirme sua senha atual e escolha uma nova senha com pelo menos 6 caracteres.",
                [new("senhaAtual", "Senha atual", Senha: true), new("senha", "Nova senha", Senha: true),
                 new("confirmacaoSenha", "Confirmar nova senha", Senha: true)],
                campos => _viewModel.AlterarSenhaAsync(campos["senhaAtual"], campos["senha"], campos["confirmacaoSenha"]));
            if (salvou) await MensagensDaInterface.ToastAsync("Senha alterada. Use a nova senha no próximo acesso.");
        }
        finally { _modalAberto = false; }
    }

    private async void OnDeleteAccountClicked(object? sender, EventArgs e)
    {
        if (_modalAberto) return;
        _modalAberto = true;
        try
        {
            if (!await DisplayAlertAsync("Excluir conta?",
                "Sua conta, seus relatos e as respostas vinculadas serão excluídos. Essa ação não pode ser desfeita.",
                "Continuar", "Cancelar")) return;
            var excluiu = await ModaisDaPlataforma.FormularioAsync(this, "Excluir conta",
                "Digite a senha atual para confirmar a exclusão definitiva da conta e dos seus relatos.",
                [new("senhaAtual", "Senha atual", Senha: true)],
                campos => _viewModel.ExcluirContaAsync(campos["senhaAtual"]), "Excluir definitivamente", perigoso: true);
            if (excluiu)
            {
                await NavegacaoDoAplicativo.MostrarLoginAsync(this);
                await MensagensDaInterface.ToastAsync("Sua conta e seus relatos foram excluídos.");
            }
        }
        finally { _modalAberto = false; }
    }

    private async void OnMyReportsClicked(object? sender, EventArgs e)
    {
        MyReportsPanel.IsVisible = true;
        await CarregarMeusRelatosAsync();
    }

    private async Task CarregarMeusRelatosAsync()
    {
        RelatosAviso.Tom = TomDoAviso.Informacao;
        try { await _viewModel.CarregarMeusRelatosAsync(); }
        catch (ExcecaoDaApi erro)
        {
            RelatosAviso.Tom = TomDoAviso.Erro;
            await VerificarSessaoAsync(erro);
        }
    }

    private void OnReportFilterChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_interfacePronta && e.Value && sender is RadioButton radio)
            _viewModel.SelecionarFiltro(radio.Value?.ToString() ?? "todos");
    }

    private async void OnOpenReportClicked(object? sender, EventArgs e)
    {
        if (_modalAberto || sender is not BindableObject { BindingContext: RelatoDoAluno relato }) return;
        _modalAberto = true;
        try
        {
            await FeedbackDeToque.AnimarToqueAsync(sender);
            var consulta = await _viewModel.ObterRelatoAsync(relato.Id);
            await ModaisDaPlataforma.MostrarRelatoAsync(this, consulta.Dados.Relato, consulta.Mensagem);
        }
        catch (ExcecaoDaApi erro) { if (!await VerificarSessaoAsync(erro)) await DisplayAlertAsync("Não foi possível abrir", erro.Message, "Entendi"); }
        finally { _modalAberto = false; }
    }
}
