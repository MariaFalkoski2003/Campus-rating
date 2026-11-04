using CampusRating.Servicos;

namespace CampusRating;

public partial class SignPage : ContentPage
{
    private static readonly string[] Animals =
    [
        "Coruja", "Raposa", "Arara", "Lontra", "Capivara", "Tucano", "Lobo", "Panda"
    ];

    private static readonly string[] Qualities =
    [
        "Serena", "Curiosa", "Gentil", "Atenta", "Criativa", "Valente", "Sábia", "Amiga"
    ];

    private readonly ApiCampusRating _api = ApiCampusRating.Instancia;

    private string _currentNickname = "Coruja Serena 47";
    private bool _enviando;

    public SignPage()
    {
        InitializeComponent();
    }

    private void OnEmailCompleted(object? sender, EventArgs e)
    {
        PasswordEntry.Focus();
    }

    private void OnPasswordCompleted(object? sender, EventArgs e)
    {
        ConfirmPasswordEntry.Focus();
    }

    private async void OnConfirmPasswordCompleted(object? sender, EventArgs e)
    {
        await RegistrarAsync();
    }

    private void OnTogglePasswordVisibility(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordVisibilityButton.Source = PasswordEntry.IsPassword
            ? "eye_outline.png"
            : "eye_off_outline.png";
    }

    private void OnToggleConfirmPasswordVisibility(object? sender, EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        ConfirmPasswordVisibilityButton.Source = ConfirmPasswordEntry.IsPassword
            ? "eye_outline.png"
            : "eye_off_outline.png";
    }

    private void OnGenerateNickname(object? sender, EventArgs e)
    {
        string nextNickname;

        do
        {
            var animal = Animals[Random.Shared.Next(Animals.Length)];
            var quality = Qualities[Random.Shared.Next(Qualities.Length)];
            var number = Random.Shared.Next(10, 100);
            nextNickname = $"{animal} {quality} {number}";
        }
        while (nextNickname == _currentNickname);

        _currentNickname = nextNickname;
        NicknameLabel.Text = nextNickname;
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        await RegistrarAsync();
    }

    private async Task RegistrarAsync()
    {
        if (_enviando)
        {
            return;
        }

        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmation = ConfirmPasswordEntry.Text ?? string.Empty;

        if (!ValidarLocalmente(email, password, confirmation, out var problema, out var campoComProblema))
        {
            await DisplayAlertAsync(problema.Titulo, problema.Mensagem, "Entendi");
            campoComProblema?.Focus();
            return;
        }

        DefinirEstadoDeEnvio(true);

        try
        {
            var resposta = await _api.RegistrarAsync(email, password, confirmation, _currentNickname, RulesCheckBox.IsChecked);

            await SessaoDoUsuario.IniciarAsync(resposta);
            _currentNickname = resposta.Usuario.Apelido;
            NicknameLabel.Text = _currentNickname;
            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;

            await Shell.Current.GoToAsync(
                $"//{nameof(HomePage)}?nickname={Uri.EscapeDataString(_currentNickname)}");
        }
        catch (ExcecaoDaApi erro)
        {
            await DisplayAlertAsync("Não foi possível criar a conta", erro.Message, "Entendi");
            CampoDoErro(erro.Campo)?.Focus();
        }
        finally
        {
            DefinirEstadoDeEnvio(false);
        }
    }

    private bool ValidarLocalmente(
        string email,
        string password,
        string confirmation,
        out (string Titulo, string Mensagem) problema,
        out Entry? campo)
    {
        if (!EmailInstitucional.EhValido(email))
        {
            problema = ("E-mail inválido", EmailInstitucional.MensagemDeErro);
            campo = EmailEntry;
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            problema = ("Senha muito curta", "A senha deve ter pelo menos 6 caracteres.");
            campo = PasswordEntry;
            return false;
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            problema = ("Senhas diferentes", "As senhas informadas não coincidem.");
            campo = ConfirmPasswordEntry;
            return false;
        }

        if (!RulesCheckBox.IsChecked)
        {
            problema = ("Aceite necessário", "Leia e aceite as regras da plataforma para criar sua conta.");
            campo = null;
            return false;
        }

        problema = default;
        campo = null;
        return true;
    }

    private Entry? CampoDoErro(string? campo) => campo switch
    {
        "email" => EmailEntry,
        "senha" => PasswordEntry,
        "confirmacaoSenha" => ConfirmPasswordEntry,
        _ => null
    };

    private void DefinirEstadoDeEnvio(bool enviando)
    {
        _enviando = enviando;
        CreateAccountButton.IsEnabled = !enviando;
        CreateAccountButton.Text = enviando ? "Criando conta..." : "Criar conta";
        RulesCheckBox.IsEnabled = !enviando;
    }

    private async void OnRulesClicked(object? sender, EventArgs e)
    {
        if (!_enviando) await ModaisDaPlataforma.MostrarRegrasAsync(this);
    }

    private async void OnGoToLoginClicked(object? sender, EventArgs e)
    {
        if (!_enviando)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
