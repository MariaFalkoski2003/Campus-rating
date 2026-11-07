namespace CampusRating.ViewModels;

public abstract class AutenticacaoViewModelBase : ViewModelBase
{
    private string _email = "";
    public string Email { get => _email; set => Definir(ref _email, value); }
    private string _senha = "";
    public string Senha { get => _senha; set => Definir(ref _senha, value); }
    private bool _enviando;
    public bool Enviando
    {
        get => _enviando;
        set
        {
            if (_enviando == value) return;
            Definir(ref _enviando, value);
            Notificar(nameof(PodeEnviar));
            Notificar(nameof(TextoBotao));
        }
    }
    public bool PodeEnviar => !Enviando;
    public abstract string TextoBotao { get; }
}
