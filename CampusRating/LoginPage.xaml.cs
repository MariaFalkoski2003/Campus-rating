using CampusRating.Servicos;

namespace CampusRating;

public partial class LoginPage : ContentPage
{
    private readonly ApiCampusRating _api = ApiCampusRating.Instancia;
    private bool _enviando;

    public LoginPage()
    {
        InitializeComponent();
    }

    private void OnEmailCompleted(object? sender, EventArgs e)
    {
        PasswordEntry.Focus();
    }

    private async void OnPasswordCompleted(object? sender, EventArgs e)
    {
        await EntrarAsync();
    }

    private void OnTogglePasswordVisibility(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordVisibilityButton.Source = PasswordEntry.IsPassword
            ? "login_eye.png"
            : "login_eye_off.png";
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await EntrarAsync();
    }

    private async Task EntrarAsync()
    {
        if (_enviando)
        {
            return;
        }

        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var senha = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@aluno.uricer.edu.br"))
        {
            await DisplayAlertAsync("E-mail inválido", "Informe seu e-mail institucional.", "Entendi");
            EmailEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(senha))
        {
            await DisplayAlertAsync("Senha necessária", "Digite sua senha para continuar.", "Entendi");
            PasswordEntry.Focus();
            return;
        }

        DefinirEstadoDeEnvio(true);

        try
        {
            var resposta = await _api.EntrarAsync(email, senha);
            SessaoDoUsuario.Iniciar(resposta);
            PasswordEntry.Text = string.Empty;

            await Shell.Current.GoToAsync(
                $"//{nameof(HomePage)}?nickname={Uri.EscapeDataString(resposta.Usuario.Apelido)}");
        }
        catch (ExcecaoDaApi erro)
        {
            await DisplayAlertAsync("Não foi possível entrar", erro.Message, "Entendi");
            if (erro.Campo == "email")
            {
                EmailEntry.Focus();
            }
            else if (erro.Campo == "senha")
            {
                PasswordEntry.Focus();
            }
        }
        finally
        {
            DefinirEstadoDeEnvio(false);
        }
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        if (!_enviando)
        {
            await Shell.Current.GoToAsync(nameof(MainPage));
        }
    }

    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "Recuperar senha",
            "A recuperação de senha ainda não está disponível.",
            "Entendi");
    }

    private void DefinirEstadoDeEnvio(bool enviando)
    {
        _enviando = enviando;
        LoginButton.IsEnabled = !enviando;
        LoginButton.Text = enviando ? "Entrando..." : "Entrar";
    }
}
