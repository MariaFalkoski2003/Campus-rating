using CampusRating.Servicos;
using CampusRating.ViewModels;
using CampusRating.Componentes;

namespace CampusRating;

public partial class SignPage : ContentPage
{
    private readonly CadastroViewModel _viewModel = new();

    public SignPage()
    {
        InitializeComponent();
        NavigationPage.SetTitleView(this, new CabecalhoDoCampus("Criar conta"));
        BindingContext = _viewModel;
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

    private void OnGenerateNickname(object? sender, EventArgs e) => _viewModel.SortearApelido();

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        await RegistrarAsync();
    }

    private async Task RegistrarAsync()
    {
        if (_viewModel.Enviando)
        {
            return;
        }

        DefinirEstadoDeEnvio(true);
        ErroAviso.Mensagem = "";

        try
        {
            await _viewModel.RegistrarAsync();

            await NavegacaoDoAplicativo.MostrarInicioAsync(this);
        }
        catch (ExcecaoDaApi erro)
        {
            ErroAviso.Mensagem = erro.Message;
            CampoDoErro(erro.Campo)?.Focus();
        }
        finally
        {
            DefinirEstadoDeEnvio(false);
        }
    }

    private Entry? CampoDoErro(string? campo) => campo switch
    {
        "email" => EmailEntry,
        "senha" => PasswordEntry,
        "confirmacaoSenha" => ConfirmPasswordEntry,
        _ => null
    };

    private void DefinirEstadoDeEnvio(bool enviando) => _viewModel.Enviando = enviando;

    protected override bool OnBackButtonPressed() => _viewModel.Enviando || base.OnBackButtonPressed();

    private async void OnRulesClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.Enviando) await ModaisDaPlataforma.MostrarRegrasAsync(this);
    }

    private async void OnGoToLoginClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.Enviando)
        {
            await Navigation.PopAsync();
        }
    }
}
