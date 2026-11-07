using System.Collections.ObjectModel;
using CampusRating.Modelos;
using CampusRating.Servicos;

namespace CampusRating.ViewModels;

public sealed class AdministracaoViewModel : ViewModelBase
{
    private List<RelatoDaApi> _todosRelatos = [];
    private bool _carregando;
    private int _filtroSelecionado;
    private string _mensagem = "";

    public ObservableCollection<RelatoAdministrativo> Relatos { get; } = new();
    public bool Carregando
    {
        get => _carregando;
        private set { Definir(ref _carregando, value); Notificar(nameof(PodeInteragir)); }
    }
    public bool PodeInteragir => !Carregando;
    public string Mensagem { get => _mensagem; private set => Definir(ref _mensagem, value); }
    public int FiltroSelecionado
    {
        get => _filtroSelecionado;
        set { Definir(ref _filtroSelecionado, value); Filtrar(); }
    }

    public async Task CarregarAsync()
    {
        if (Carregando) return;
        Carregando = true;
        Mensagem = "Carregando relatos...";
        _todosRelatos = [];
        Relatos.Clear();
        try
        {
            var resposta = await ApiCampusRating.Instancia.ListarRelatosAdministrativosAsync();
            _todosRelatos = resposta.Relatos;
            Filtrar();
        }
        catch (ExcecaoDaApi erro) { Mensagem = erro.Message; throw; }
        finally { Carregando = false; }
    }

    private void Filtrar()
    {
        var status = FiltroSelecionado switch { 1 => "pendente", 2 => "em_analise", 3 => "resolvido", _ => "todos" };
        Relatos.Clear();
        foreach (var relato in _todosRelatos.Where(relato => status == "todos" || relato.Status == status))
            Relatos.Add(new(relato.Id, relato.LocalNome, relato.Apelido, relato.Texto, relato.Status, relato.CriadoEm));
        Mensagem = Relatos.Count == 0 ? "Nenhum relato encontrado neste filtro." : $"{Relatos.Count} relato(s) encontrado(s).";
    }
}
