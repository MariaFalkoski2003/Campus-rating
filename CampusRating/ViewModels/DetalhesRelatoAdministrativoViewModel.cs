using System.Collections.ObjectModel;
using CampusRating.Modelos;
using CampusRating.Servicos;

namespace CampusRating.ViewModels;

public sealed class DetalhesRelatoAdministrativoViewModel(int relatoId) : ViewModelBase
{
    private RelatoAdministrativo? _relato;
    private bool _ocupado;
    private string _mensagem = "";
    private int _statusSelecionado;
    private string _textoResposta = "";
    private string _chaveEnvio = Guid.NewGuid().ToString();

    public ObservableCollection<RespostaAdministrativa> Respostas { get; } = new();
    public RelatoAdministrativo? Relato { get => _relato; private set => Definir(ref _relato, value); }
    public bool TemRelato => Relato is not null;
    public bool SemRespostas => Respostas.Count == 0;
    public bool Ocupado
    {
        get => _ocupado;
        private set
        {
            Definir(ref _ocupado, value);
            Notificar(nameof(PodeInteragir));
            Notificar(nameof(PodeSalvar));
        }
    }
    public bool PodeInteragir => !Ocupado && TemRelato;
    public bool PodeSalvar => PodeInteragir &&
        (!string.IsNullOrWhiteSpace(TextoResposta) || StatusDoFormulario != Relato!.Status);
    public string Mensagem { get => _mensagem; private set => Definir(ref _mensagem, value); }
    private string StatusDoFormulario => StatusSelecionado switch
    {
        0 => "pendente", 1 => "em_analise", 2 => "resolvido", _ => ""
    };
    public int StatusSelecionado
    {
        get => _statusSelecionado;
        set
        {
            if (_statusSelecionado == value) return;
            Definir(ref _statusSelecionado, value);
            _chaveEnvio = Guid.NewGuid().ToString();
            Notificar(nameof(PodeSalvar));
        }
    }
    public string TextoResposta
    {
        get => _textoResposta;
        set
        {
            value ??= "";
            if (_textoResposta == value) return;
            Definir(ref _textoResposta, value);
            _chaveEnvio = Guid.NewGuid().ToString();
            Notificar(nameof(PodeSalvar));
        }
    }

    public async Task CarregarAsync()
    {
        if (Ocupado) return;
        Ocupado = true;
        Mensagem = "Carregando relato...";
        // Uma atualização malsucedida não deixa ações habilitadas sobre dados antigos.
        Relato = null;
        Respostas.Clear();
        Notificar(nameof(TemRelato));
        try { Aplicar((await ApiCampusRating.Instancia.ObterRelatoAdministrativoAsync(relatoId)).Relato); }
        catch (ExcecaoDaApi erro) { Mensagem = erro.Message; throw; }
        finally { Ocupado = false; }
    }

    public async Task SalvarAsync()
    {
        if (!PodeSalvar) return;
        var status = StatusDoFormulario;
        if (status.Length == 0) throw new ExcecaoDaApi("status_invalido", "Selecione um andamento válido.", "status");
        if (TextoResposta.Length > 2000)
            throw new ExcecaoDaApi("texto_invalido", "Escreva uma resposta com até 2.000 caracteres.", "texto");
        var temResposta = !string.IsNullOrWhiteSpace(TextoResposta);
        Ocupado = true;
        try
        {
            var resposta = temResposta
                ? await ApiCampusRating.Instancia.ResponderRelatoAsync(relatoId, TextoResposta, _chaveEnvio, status, Relato!.Status)
                : await ApiCampusRating.Instancia.AlterarStatusDoRelatoAsync(relatoId, status, Relato!.Status);
            Aplicar(resposta.Relato);
            TextoResposta = "";
            Mensagem = temResposta
                ? "Alterações salvas. O aluno poderá consultar a resposta em Meus relatos e Avisos."
                : "Andamento atualizado.";
        }
        catch (ExcecaoDaApi erro) { Mensagem = erro.Message; throw; }
        finally { Ocupado = false; }
    }

    private void Aplicar(RelatoDaApi relato)
    {
        Relato = new(relato.Id, relato.LocalNome, relato.Apelido, relato.Texto, relato.Status, relato.CriadoEm);
        StatusSelecionado = relato.Status switch { "pendente" => 0, "em_analise" => 1, "resolvido" => 2, _ => -1 };
        Respostas.Clear();
        foreach (var resposta in relato.Respostas ?? []) Respostas.Add(new(resposta.Texto, resposta.CriadoEm));
        Mensagem = "";
        Notificar(nameof(TemRelato));
        Notificar(nameof(SemRespostas));
    }
}
