using CampusRating.Servicos;
namespace CampusRating.ViewModels;
public sealed class CadastroViewModel : AutenticacaoViewModelBase
{
    private static readonly string[] Animals =
    [
        "Coruja", "Raposa", "Arara", "Lontra", "Capivara", "Tucano", "Lobo", "Panda"
    ];

    private static readonly string[] Qualities =
    [
        "Serena", "Curiosa", "Gentil", "Atenta", "Criativa", "Valente", "Sábia", "Amiga"
    ];

    private string _apelido = "Coruja Serena 47";
    public string Apelido { get => _apelido; set => Definir(ref _apelido, value); }
    private string _confirmacaoSenha = "";
    public string ConfirmacaoSenha { get => _confirmacaoSenha; set => Definir(ref _confirmacaoSenha, value); }
    private bool _aceitouRegras = false;
    public bool AceitouRegras { get => _aceitouRegras; set => Definir(ref _aceitouRegras, value); }

    public override string TextoBotao => Enviando ? "Criando conta..." : "Criar conta";

    public async Task RegistrarAsync()
    {
        if (!EmailInstitucional.EhValido(Email)) throw new ExcecaoDaApi("email_invalido", EmailInstitucional.MensagemDeErro, "email");
        if (string.IsNullOrWhiteSpace(Senha) || Senha.Length < 6) throw new ExcecaoDaApi("senha_curta", "A senha deve ter pelo menos 6 caracteres.", "senha");
        if (Senha != ConfirmacaoSenha) throw new ExcecaoDaApi("senhas_diferentes", "As senhas informadas não coincidem.", "confirmacaoSenha");
        if (!AceitouRegras) throw new ExcecaoDaApi("regras_nao_aceitas", "Leia e aceite as regras da plataforma para criar sua conta.");
        var resposta = await ApiCampusRating.Instancia.RegistrarAsync(Email.Trim(), Senha, ConfirmacaoSenha, Apelido, AceitouRegras);
        await SessaoDoUsuario.IniciarAsync(resposta);
        Apelido = resposta.Usuario.Apelido;
        Senha = ""; ConfirmacaoSenha = "";
    }
    public void SortearApelido()
    {
        string nextNickname;

        do
        {
            var animal = Animals[Random.Shared.Next(Animals.Length)];
            var quality = Qualities[Random.Shared.Next(Qualities.Length)];
            var number = Random.Shared.Next(10, 100);
            nextNickname = $"{animal} {quality} {number}";
        }
        while (nextNickname == Apelido);

        Apelido = nextNickname;

    }
}
