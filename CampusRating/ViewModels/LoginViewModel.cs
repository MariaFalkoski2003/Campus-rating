using CampusRating.Servicos;
using System.Net.Mail;
namespace CampusRating.ViewModels;
public sealed class LoginViewModel(bool administrador = false) : AutenticacaoViewModelBase
{
    public bool EhLoginAdministrativo { get; } = administrador;
    public bool MostrarCadastro => !EhLoginAdministrativo;
    public string Titulo => EhLoginAdministrativo ? "Administração" : "Entrar";
    public string Subtitulo => EhLoginAdministrativo ? "Acesse o painel administrativo." : "Utilize seu e-mail institucional.";
    public string PlaceholderEmail => EhLoginAdministrativo ? "E-mail do administrador" : "E-mail institucional";
    public string TextoAlternarPerfil => EhLoginAdministrativo ? "É um aluno?" : "É um administrador?";
    public override string TextoBotao => Enviando ? "Entrando..." : "Entrar";

    public async Task EntrarAsync()
    {
        var email = Email?.Trim() ?? "";
        if (EhLoginAdministrativo)
        {
            if (!MailAddress.TryCreate(email, out var endereco) || !string.Equals(endereco.Address, email, StringComparison.OrdinalIgnoreCase))
                throw new ExcecaoDaApi("email_invalido", "Informe um e-mail válido para a administração.", "email");
        }
        else if (!EmailInstitucional.EhValido(email))
            throw new ExcecaoDaApi("email_invalido", EmailInstitucional.MensagemDeErro, "email");
        if (string.IsNullOrWhiteSpace(Senha)) throw new ExcecaoDaApi("senha_ausente", "Digite sua senha para continuar.", "senha");
        var resposta = EhLoginAdministrativo
            ? await ApiCampusRating.Instancia.EntrarAdministradorAsync(email, Senha)
            : await ApiCampusRating.Instancia.EntrarAsync(email, Senha);
        await SessaoDoUsuario.IniciarAsync(resposta);
        Senha = "";
    }
}
