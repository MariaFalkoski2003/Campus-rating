using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CampusRating.Modelos;
using CampusRating.Servicos;

namespace CampusRating.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private DadosDoCampus _dados => ServicosLocais.Dados;
    private string _origemRelatos = "";
    private CampusPlace[] _allPlaces = [];
    private string _category = "Todos";
    private string _filtroRelatos = "todos";
    private List<RelatoDaApi> _todosRelatos = [];
    private bool _carregandoRelatos;
    private bool _carregandoAvisos;
    public ObservableCollection<CampusPlace> Places { get; } = new();
    public ObservableCollection<RelatoDoAluno> MeusRelatos { get; } = new();
    public ObservableCollection<AvisoDoAluno> Avisos { get; } = new();
    private string _mensagemLocal = "";
    public string MensagemLocal { get => _mensagemLocal; set => Definir(ref _mensagemLocal, value); }
    private bool _mostrarMensagemLocal = false;
    public bool MostrarMensagemLocal { get => _mostrarMensagemLocal; set => Definir(ref _mostrarMensagemLocal, value); }
    private bool _mostrarErroLocal = false;
    public bool MostrarErroLocal { get => _mostrarErroLocal; set => Definir(ref _mostrarErroLocal, value); }
    private string _erroLocal = "";
    public string ErroLocal { get => _erroLocal; set => Definir(ref _erroLocal, value); }
    private bool _carregandoLocais = false;
    public bool CarregandoLocais { get => _carregandoLocais; set => Definir(ref _carregandoLocais, value); }
    private bool _mostrarLocais = true;
    public bool MostrarLocais { get => _mostrarLocais; set => Definir(ref _mostrarLocais, value); }
    private string _mensagemRelatos = "";
    public string MensagemRelatos { get => _mensagemRelatos; set => Definir(ref _mensagemRelatos, value); }
    private string _mensagemAvisos = "";
    public string MensagemAvisos { get => _mensagemAvisos; set => Definir(ref _mensagemAvisos, value); }
    private bool _mostrarAvisos = false;
    public bool MostrarAvisos { get => _mostrarAvisos; set => Definir(ref _mostrarAvisos, value); }
    private string _apelido = "Seu perfil anônimo";
    public string Apelido { get => _apelido; set => Definir(ref _apelido, value); }
    private string _email = "";
    public string Email { get => _email; set => Definir(ref _email, value); }

    private string _busca = "";
    public string Busca { get => _busca; set { Definir(ref _busca, value); UpdatePlaces(); } }
    public void SelecionarCategoria(string categoria) { _category = categoria; UpdatePlaces(); }
    public void SelecionarFiltro(string filtro) { _filtroRelatos = filtro; FiltrarMeusRelatos(); }
    public void LimparDadosPessoais()
    {
        _todosRelatos = []; _origemRelatos = "";
        MeusRelatos.Clear(); Avisos.Clear(); Places.Clear(); _allPlaces = [];
        MensagemRelatos = ""; MensagemAvisos = "";
        Email = ""; Apelido = "Seu perfil anônimo";
    }
    public Task<ConsultaSincronizada<RespostaDeRelato>> ObterRelatoAsync(int id) => _dados.ObterMeuRelatoAsync(id);
    public Task<bool> MarcarAvisoLidoAsync(int id) => _dados.MarcarAvisoLidoAsync(id);
    public async Task AlterarEmailAsync(string email, string senhaAtual)
    {
        if (!EmailInstitucional.EhValido(email)) throw new ExcecaoDaApi("email_invalido", EmailInstitucional.MensagemDeErro, "email");
        await SessaoDoUsuario.IniciarAsync(await ApiCampusRating.Instancia.AlterarEmailAsync(email, senhaAtual));
        AtualizarPerfil();
    }
    public async Task AlterarSenhaAsync(string senhaAtual, string senha, string confirmacao)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6) throw new ExcecaoDaApi("senha_curta", "A senha deve ter pelo menos 6 caracteres.", "senha");
        if (senha != confirmacao) throw new ExcecaoDaApi("senhas_diferentes", "As senhas informadas não coincidem.", "confirmacaoSenha");
        await SessaoDoUsuario.IniciarAsync(await ApiCampusRating.Instancia.AlterarSenhaAsync(senhaAtual, senha, confirmacao));
    }
    public async Task ExcluirContaAsync(string senhaAtual) { await ApiCampusRating.Instancia.ExcluirContaAsync(senhaAtual); }
    public async Task CarregarLocaisAsync()
    {
        if (CarregandoLocais) return;

        MostrarErroLocal = false;
        MostrarMensagemLocal = false;
        DefinirEstadoDeCarregamento(true);
        _allPlaces = [];
        UpdatePlaces();

        try
        {
            var consulta = await _dados.ListarLocaisAsync();
            MensagemLocal = consulta.Mensagem;
            MostrarMensagemLocal = consulta.Mensagem.Length > 0;
            _allPlaces = consulta.Dados.Select(local => new CampusPlace(
                local.Id, local.Nome, local.Categoria, local.Localizacao,
                local.Descricao, local.Imagem, local.QuantidadeComentarios)).ToArray();
            UpdatePlaces();
        }
        catch (ExcecaoDaApi erro)
        {
            ErroLocal = erro.Message;
            MostrarErroLocal = true;
            throw;
        }
        finally
        {
            DefinirEstadoDeCarregamento(false);
        }
    }

    private void DefinirEstadoDeCarregamento(bool carregando)
    {
        CarregandoLocais = carregando;
        MostrarLocais = !carregando && !MostrarErroLocal;
    }

    private void UpdatePlaces()
    {
        var query = Normalize(Busca?.Trim() ?? "");
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

    public void AtualizarPerfil()
    {
        Apelido = SessaoDoUsuario.Usuario?.Apelido ?? "Seu perfil anônimo";
        Email = SessaoDoUsuario.Usuario?.Email ?? "";
    }

    public async Task CarregarMeusRelatosAsync()
    {
        if (_carregandoRelatos) return;
        _carregandoRelatos = true;
        MensagemRelatos = "Carregando seus relatos...";
        MeusRelatos.Clear();
        try
        {
            var consulta = await _dados.ListarMeusRelatosAsync();
            _todosRelatos = consulta.Dados.Relatos;
            _origemRelatos = consulta.Mensagem;
            FiltrarMeusRelatos();
        }
        catch (ExcecaoDaApi erro)
        {
            _todosRelatos = [];
            _origemRelatos = erro.Message;
            MensagemRelatos = erro.Message;
            throw;
        }
        finally { _carregandoRelatos = false; }
    }

    private void FiltrarMeusRelatos()
    {
        MeusRelatos.Clear();
        foreach (var relato in _todosRelatos.Where(relato => _filtroRelatos == "todos" || relato.Status == _filtroRelatos))
            MeusRelatos.Add(new(relato.Id, relato.LocalNome, relato.Texto, relato.Status, relato.CriadoEm));
        MensagemRelatos = MeusRelatos.Count == 0 ? "Nenhum relato encontrado neste filtro." : $"{MeusRelatos.Count} relato(s) encontrado(s).";
        if (_origemRelatos.Length > 0) MensagemRelatos += "\n" + _origemRelatos;
    }

    public async Task CarregarAvisosAsync()
    {
        if (_carregandoAvisos) return;
        _carregandoAvisos = true;
        MensagemAvisos = "Carregando avisos...";
        MostrarAvisos = false;
        Avisos.Clear();
        try
        {
            var consulta = await _dados.ListarAvisosAsync();
            foreach (var aviso in consulta.Dados.Avisos)
                Avisos.Add(new(aviso.Id, aviso.RelatoId, aviso.LocalNome, aviso.Texto, aviso.CriadoEm, aviso.Lida));
            MensagemAvisos = $"{Avisos.Count(aviso => !aviso.Lida)} resposta(s) não lida(s).";
            if (consulta.Mensagem.Length > 0) MensagemAvisos += "\n" + consulta.Mensagem;
            MostrarAvisos = true;
        }
        catch (ExcecaoDaApi erro)
        {
            MensagemAvisos = erro.Message;
            throw;
        }
        finally { _carregandoAvisos = false; }
    }
}
