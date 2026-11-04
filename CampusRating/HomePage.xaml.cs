using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CampusRating.Servicos;

namespace CampusRating;

public record CampusPlace(int Id, string Name, string Category, string Location,
                          string Description, string Image, int CommentCount)
{
    public string CommentsText => CommentCount == 1 ? "1 comentário" : $"{CommentCount} comentários";
}

public record RelatoDoAluno(int Id, string LocalNome, string Texto, string Status, string CriadoEm)
{
    public string StatusTexto => FormatacaoDoRelato.Status(Status);
    public string DataTexto => FormatacaoDoRelato.Data(CriadoEm);
}

public record AvisoDoAluno(int Id, int RelatoId, string LocalNome, string Texto, string CriadoEm, bool Lida)
{
    public string LeituraTexto => Lida ? "Lido" : "Nova resposta";
    public string DataTexto => FormatacaoDoRelato.Data(CriadoEm);
}

[QueryProperty(nameof(Nickname), "nickname")]
public partial class HomePage : ContentPage
{
    private readonly ApiCampusRating _api = ApiCampusRating.Instancia;
    private DadosDoCampus _dados => ServicosLocais.Dados;
    private string _mensagemRelatos = "";
    private CampusPlace[] _allPlaces = [];
    private string _category = "Todos";
    private bool _carregando;
    private bool _abrindoLocal;
    private bool _modalAberto;
    private bool _carregandoRelatos;
    private bool _carregandoAvisos;
    private bool _interfacePronta;
    private string _abaAtual = "places";
    private string _filtroRelatos = "todos";
    private List<RelatoDaApi> _todosRelatos = [];

    public ObservableCollection<CampusPlace> Places { get; } = new();
    public ObservableCollection<RelatoDoAluno> MeusRelatos { get; } = new();
    public ObservableCollection<AvisoDoAluno> Avisos { get; } = new();

    public string Nickname
    {
        set => NicknameLabel.Text = string.IsNullOrWhiteSpace(value) ? "Seu perfil anônimo" : value;
    }

    public HomePage()
    {
        InitializeComponent();
        _interfacePronta = true;
        BindingContext = this;
        UpdatePlaces();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_modalAberto) return;

        if (!SessaoDoUsuario.EstaAutenticado)
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        AtualizarPerfil();
        SelectTab(_abaAtual);
        if (_abaAtual == "places") await CarregarLocaisAsync();
        else if (_abaAtual == "notices") await CarregarAvisosAsync();
        else if (MyReportsPanel.IsVisible) await CarregarMeusRelatosAsync();
    }

    private async Task CarregarLocaisAsync()
    {
        if (_carregando) return;

        LoadErrorPanel.IsVisible = false;
        LocalCacheInfoLabel.IsVisible = false;
        DefinirEstadoDeCarregamento(true);
        _allPlaces = [];
        UpdatePlaces();

        try
        {
            var consulta = await _dados.ListarLocaisAsync();
            LocalCacheInfoLabel.Text = consulta.Mensagem;
            LocalCacheInfoLabel.IsVisible = consulta.Mensagem.Length > 0;
            _allPlaces = consulta.Dados.Select(local => new CampusPlace(
                local.Id, local.Nome, local.Categoria, local.Localizacao,
                local.Descricao, local.Imagem, local.QuantidadeComentarios)).ToArray();
            UpdatePlaces();
        }
        catch (ExcecaoDaApi erro)
        {
            if (erro.Codigo is "token_ausente" or "token_invalido" or "usuario_nao_encontrado")
            {
                await SessaoDoUsuario.EncerrarAsync();
                await DisplayAlertAsync("Sessão expirada", "Entre novamente para consultar os locais.", "Entendi");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            LoadErrorLabel.Text = erro.Message;
            LoadErrorPanel.IsVisible = true;
        }
        finally
        {
            DefinirEstadoDeCarregamento(false);
        }
    }

    private void DefinirEstadoDeCarregamento(bool carregando)
    {
        _carregando = carregando;
        LoadingPanel.IsVisible = carregando;
        LoadingIndicator.IsRunning = carregando;
        PlacesCollection.IsVisible = !carregando && !LoadErrorPanel.IsVisible;
    }

    private async void OnRetryPlacesClicked(object? sender, EventArgs e) => await CarregarLocaisAsync();

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => UpdatePlaces();

    private void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button selected) return;
        _category = selected.CommandParameter?.ToString() ?? "Todos";
        foreach (var button in CategoryButtons.Children.OfType<Button>())
        {
            var active = button == selected;
            button.BackgroundColor = Color.FromArgb(active ? "#F1EDFF" : "#FFFFFF");
            button.TextColor = Color.FromArgb(active ? "#8065F1" : "#7D7B85");
            button.BorderColor = Color.FromArgb(active ? "#CEC0FF" : "#E5E3EA");
        }
        UpdatePlaces();
    }

    private void UpdatePlaces()
    {
        var query = Normalize(SearchEntry.Text?.Trim() ?? "");
        Places.Clear();
        foreach (var place in _allPlaces)
        {
            if (_category != "Todos" && place.Category != _category) continue;
            if (!Normalize($"{place.Name} {place.Location}").Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
            Places.Add(place);
        }
    }

    private static string Normalize(string text)
    {
        var chars = text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    }

    private async void OnPlaceTapped(object? sender, TappedEventArgs e)
    {
        if (_abrindoLocal || sender is not BindableObject { BindingContext: CampusPlace place }) return;

        _abrindoLocal = true;
        try
        {
            await FeedbackDeToque.AnimarToqueAsync(sender);
            await Shell.Current.GoToAsync($"{nameof(DetalhesLocalPage)}?localId={place.Id}");
        }
        finally
        {
            _abrindoLocal = false;
        }
    }

    private async void OnPlacesTabTapped(object? sender, TappedEventArgs e)
    {
        await FeedbackDeToque.AnimarToqueAsync(sender);
        SelectTab("places");
        await CarregarLocaisAsync();
    }

    private async void OnProfileTabTapped(object? sender, TappedEventArgs e)
    {
        await FeedbackDeToque.AnimarToqueAsync(sender);
        SelectTab("profile");
        AtualizarPerfil();
    }

    private async void OnNoticesTabTapped(object? sender, TappedEventArgs e)
    {
        await FeedbackDeToque.AnimarToqueAsync(sender);
        SelectTab("notices");
        await CarregarAvisosAsync();
    }

    private void SelectTab(string tab)
    {
        _abaAtual = tab;
        PlacesPanel.IsVisible = tab == "places";
        ProfilePanel.IsVisible = tab == "profile";
        NoticesPanel.IsVisible = tab == "notices";
        PlacesTabIcon.Source = tab == "places" ? "location_purple.png" : "location_gray.png";
        ProfileTabIcon.Source = tab == "profile" ? "profile_purple.png" : "profile_gray.png";
        NoticesTabIcon.Source = tab == "notices" ? "bell_purple.png" : "bell_gray.png";
        PlacesTabLabel.TextColor = Color.FromArgb(tab == "places" ? "#8065F1" : "#898794");
        ProfileTabLabel.TextColor = Color.FromArgb(tab == "profile" ? "#8065F1" : "#898794");
        NoticesTabLabel.TextColor = Color.FromArgb(tab == "notices" ? "#8065F1" : "#898794");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        await SessaoDoUsuario.EncerrarAsync();
        LimparDadosPessoais();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private void AtualizarPerfil()
    {
        NicknameLabel.Text = SessaoDoUsuario.Usuario?.Apelido ?? "Seu perfil anônimo";
        EmailLabel.Text = SessaoDoUsuario.Usuario?.Email ?? "";
    }

    private void LimparDadosPessoais()
    {
        _todosRelatos = [];
        _mensagemRelatos = "";
        MyReportsMessageLabel.Text = "";
        NoticesMessageLabel.Text = "";
        MeusRelatos.Clear();
        Avisos.Clear();
        MyReportsPanel.IsVisible = false;
        _abaAtual = "places";
        EmailLabel.Text = "";
        NicknameLabel.Text = "Seu perfil anônimo";
    }

    private async void OnRulesClicked(object? sender, EventArgs e) => await ModaisDaPlataforma.MostrarRegrasAsync(this);

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
                async campos =>
                {
                    if (!EmailInstitucional.EhValido(campos["email"]))
                        throw new ExcecaoDaApi("email_invalido", EmailInstitucional.MensagemDeErro, "email");
                    await SessaoDoUsuario.IniciarAsync(await _api.AlterarEmailAsync(campos["email"], campos["senhaAtual"]));
                });
            if (salvou)
            {
                AtualizarPerfil();
                await DisplayAlertAsync("E-mail atualizado", "Use o novo e-mail no próximo acesso.", "Entendi");
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
                async campos =>
                {
                    if (string.IsNullOrWhiteSpace(campos["senha"]) || campos["senha"].Length < 6)
                        throw new ExcecaoDaApi("senha_curta", "A senha deve ter pelo menos 6 caracteres.", "senha");
                    if (campos["senha"] != campos["confirmacaoSenha"])
                        throw new ExcecaoDaApi("senhas_diferentes", "As senhas informadas não coincidem.", "confirmacaoSenha");
                    await SessaoDoUsuario.IniciarAsync(await _api.AlterarSenhaAsync(campos["senhaAtual"], campos["senha"], campos["confirmacaoSenha"]));
                });
            if (salvou) await DisplayAlertAsync("Senha alterada", "Use a nova senha no próximo acesso.", "Entendi");
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
                async campos => { await _api.ExcluirContaAsync(campos["senhaAtual"]); }, "Excluir definitivamente", perigoso: true);
            if (excluiu)
            {
                await SessaoDoUsuario.EncerrarAsync();
                LimparDadosPessoais();
                await Shell.Current.GoToAsync("//LoginPage");
                await Shell.Current.DisplayAlertAsync("Conta excluída", "Sua conta e seus relatos foram excluídos.", "Entendi");
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
        if (_carregandoRelatos) return;
        _carregandoRelatos = true;
        MyReportsMessageLabel.Text = "Carregando seus relatos...";
        MeusRelatos.Clear();
        try
        {
            var consulta = await _dados.ListarMeusRelatosAsync();
            _todosRelatos = consulta.Dados.Relatos;
            _mensagemRelatos = consulta.Mensagem;
            FiltrarMeusRelatos();
        }
        catch (ExcecaoDaApi erro)
        {
            _todosRelatos = [];
            _mensagemRelatos = erro.Message;
            MyReportsMessageLabel.Text = erro.Message;
            await VerificarSessaoAsync(erro);
        }
        finally { _carregandoRelatos = false; }
    }

    private void OnReportFilterChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!_interfacePronta || !e.Value || sender is not RadioButton radio) return;
        _filtroRelatos = radio.Value?.ToString() ?? "todos";
        FiltrarMeusRelatos();
    }

    private void FiltrarMeusRelatos()
    {
        MeusRelatos.Clear();
        foreach (var relato in _todosRelatos.Where(relato => _filtroRelatos == "todos" || relato.Status == _filtroRelatos))
            MeusRelatos.Add(new(relato.Id, relato.LocalNome, relato.Texto, relato.Status, relato.CriadoEm));
        MyReportsMessageLabel.Text = MeusRelatos.Count == 0 ? "Nenhum relato encontrado neste filtro." : $"{MeusRelatos.Count} relato(s) encontrado(s).";
        if (_mensagemRelatos.Length > 0) MyReportsMessageLabel.Text += "\n" + _mensagemRelatos;
    }

    private async void OnMyReportTapped(object? sender, TappedEventArgs e)
    {
        if (_modalAberto || sender is not BindableObject { BindingContext: RelatoDoAluno relato }) return;
        _modalAberto = true;
        try
        {
            await FeedbackDeToque.AnimarToqueAsync(sender);
            var consulta = await _dados.ObterMeuRelatoAsync(relato.Id);
            await ModaisDaPlataforma.MostrarRelatoAsync(this, consulta.Dados.Relato, consulta.Mensagem);
        }
        catch (ExcecaoDaApi erro) { if (!await VerificarSessaoAsync(erro)) await DisplayAlertAsync("Não foi possível abrir", erro.Message, "Entendi"); }
        finally { _modalAberto = false; }
    }

    private async Task CarregarAvisosAsync()
    {
        if (_carregandoAvisos) return;
        _carregandoAvisos = true;
        NoticesMessageLabel.Text = "Carregando avisos...";
        NoticesCollection.IsVisible = false;
        Avisos.Clear();
        try
        {
            var consulta = await _dados.ListarAvisosAsync();
            foreach (var aviso in consulta.Dados.Avisos)
                Avisos.Add(new(aviso.Id, aviso.RelatoId, aviso.LocalNome, aviso.Texto, aviso.CriadoEm, aviso.Lida));
            NoticesMessageLabel.Text = $"{Avisos.Count(aviso => !aviso.Lida)} resposta(s) não lida(s).";
            if (consulta.Mensagem.Length > 0) NoticesMessageLabel.Text += "\n" + consulta.Mensagem;
            NoticesCollection.IsVisible = true;
        }
        catch (ExcecaoDaApi erro)
        {
            NoticesMessageLabel.Text = erro.Message;
            await VerificarSessaoAsync(erro);
        }
        finally { _carregandoAvisos = false; }
    }

    private async void OnRefreshNoticesClicked(object? sender, EventArgs e) => await CarregarAvisosAsync();

    private async void OnNoticeTapped(object? sender, TappedEventArgs e)
    {
        if (_modalAberto || sender is not BindableObject { BindingContext: AvisoDoAluno aviso }) return;
        _modalAberto = true;
        try
        {
            await FeedbackDeToque.AnimarToqueAsync(sender);
            var consulta = await _dados.ObterMeuRelatoAsync(aviso.RelatoId);
            var marcou = !consulta.Offline && await _dados.MarcarAvisoLidoAsync(aviso.Id);
            await CarregarAvisosAsync();
            await ModaisDaPlataforma.MostrarRelatoAsync(this, consulta.Dados.Relato,
                consulta.Mensagem + (marcou ? "" : "\nA leitura do aviso só será registrada quando houver conexão."));
        }
        catch (ExcecaoDaApi erro) { if (!await VerificarSessaoAsync(erro)) await DisplayAlertAsync("Não foi possível abrir", erro.Message, "Entendi"); }
        finally { _modalAberto = false; }
    }

    private async Task<bool> VerificarSessaoAsync(ExcecaoDaApi erro)
    {
        if (erro.Codigo is not ("token_ausente" or "token_invalido" or "usuario_nao_encontrado")) return false;
        await SessaoDoUsuario.EncerrarAsync();
        LimparDadosPessoais();
        await DisplayAlertAsync("Sessão expirada", "Entre novamente para continuar.", "Entendi");
        await Shell.Current.GoToAsync("//LoginPage");
        return true;
    }
}
