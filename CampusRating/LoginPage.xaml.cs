using CampusRating.Servicos;
using CampusRating.ViewModels;

namespace CampusRating;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    private bool _animandoEntrada;

    public LoginPage(bool administrador = false)
    {
        _viewModel = new(administrador);
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DefinirEstadoDeEnvio(true);
        try
        {
            await SessaoDoUsuario.RestaurarAsync();
            if (SessaoDoUsuario.EstaAutenticado)
                await NavegacaoDoAplicativo.MostrarInicioAsync(this);
            else if (SessaoDoUsuario.Token is not null)
                await SessaoDoUsuario.EncerrarAsync();
        }
        catch (ExcecaoDaApi erro) { ErroAviso.Mensagem = erro.Message; }
        finally { DefinirEstadoDeEnvio(false); }
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
        if (_viewModel.Enviando || _animandoEntrada)
        {
            return;
        }

        _animandoEntrada = true;
        ErroAviso.Mensagem = "";
        try
        {
            // Aguarda o efeito na borda com degradê antes de desabilitar o botão.
            await FeedbackDeToque.AnimarToqueAsync(LoginButton);

            DefinirEstadoDeEnvio(true);
            await _viewModel.EntrarAsync();

            await NavegacaoDoAplicativo.MostrarInicioAsync(this);
        }
        catch (ExcecaoDaApi erro)
        {
            ErroAviso.Mensagem = erro.Message;
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
            _animandoEntrada = false;
            DefinirEstadoDeEnvio(false);
        }
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        if (_viewModel.MostrarCadastro && !_viewModel.Enviando && !_animandoEntrada)
        {
            await Navigation.PushAsync(new SignPage());
        }
    }

    private void OnSwitchAccessClicked(object? sender, EventArgs e)
    {
        if (_viewModel.Enviando || _animandoEntrada || Window is not { } janela) return;
        janela.Page = NavegacaoDoAplicativo.CriarLogin(!_viewModel.EhLoginAdministrativo);
    }

    private void DefinirEstadoDeEnvio(bool enviando) => _viewModel.Enviando = enviando;
}
