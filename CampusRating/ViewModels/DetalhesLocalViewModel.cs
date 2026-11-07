using System.Collections.ObjectModel;
using CampusRating.Modelos;
using CampusRating.Servicos;

namespace CampusRating.ViewModels;

public sealed class DetalhesLocalViewModel : ViewModelBase
{
    private DadosDoCampus _dados => ServicosLocais.Dados;
    private static readonly IReadOnlyDictionary<string, string> ImagensDetalhadas = new Dictionary<string, string>
    {
        ["campus_library.jpg"] = "campus_library_detail.png",
        ["campus_bathrooms.jpg"] = "campus_bathrooms_detail.png",
        ["campus_canteen.jpg"] = "campus_canteen_detail.png",
        ["campus_laboratory.jpg"] = "campus_laboratory_detail.png"
    };
    private int _localId;

    public ObservableCollection<ComentarioDoLocal> Comentarios { get; } = new();

    private string _mensagemLocal = "";
    public string MensagemLocal { get => _mensagemLocal; set => Definir(ref _mensagemLocal, value); }
    private string _imagem = "";
    public string Imagem { get => _imagem; set => Definir(ref _imagem, value); }
    private string _nome = "";
    public string Nome { get => _nome; set => Definir(ref _nome, value); }
    private string _categoria = "";
    public string Categoria { get => _categoria; set => Definir(ref _categoria, value); }
    private string _localizacao = "";
    public string Localizacao { get => _localizacao; set => Definir(ref _localizacao, value); }
    private string _descricao = "";
    public string Descricao { get => _descricao; set => Definir(ref _descricao, value); }
    private string _contagem = "";
    public string Contagem { get => _contagem; set => Definir(ref _contagem, value); }
    private string _erro = "";
    public string Erro { get => _erro; set => Definir(ref _erro, value); }
    private bool _mostrarMensagemLocal = false;
    public bool MostrarMensagemLocal { get => _mostrarMensagemLocal; set => Definir(ref _mostrarMensagemLocal, value); }
    private bool _carregando = false;
    public bool Carregando { get => _carregando; set => Definir(ref _carregando, value); }
    private bool _mostrarErro = false;
    public bool MostrarErro { get => _mostrarErro; set => Definir(ref _mostrarErro, value); }
    private bool _mostrarDetalhes = false;
    public bool MostrarDetalhes { get => _mostrarDetalhes; set => Definir(ref _mostrarDetalhes, value); }

    public int LocalId { get => _localId; set => _localId = value; }
    public Task<string> EnviarRelatoAsync(string texto)
    {
        texto = texto.Trim();
        if (string.IsNullOrWhiteSpace(texto)) throw new ExcecaoDaApi("texto_invalido", "Escreva o relato antes de enviar.", "texto");
        if (texto.Length > 2000) throw new ExcecaoDaApi("texto_invalido", "O relato deve ter até 2.000 caracteres.", "texto");
        return _dados.EnviarRelatoAsync(_localId, texto);
    }
    public async Task CarregarLocalAsync()
    {
        if (_carregando) return;

        Carregando = true;
        MostrarErro = false;
        MostrarDetalhes = false;
        Comentarios.Clear();

        try
        {
            var consulta = await _dados.ObterDetalhesDoLocalAsync(_localId);
            var resposta = consulta.Dados;
            MensagemLocal = consulta.Mensagem;
            MostrarMensagemLocal = consulta.Mensagem.Length > 0;
            var local = resposta.Local;
            Imagem = ImagensDetalhadas.TryGetValue(local.Imagem, out var imagemDetalhada)
                ? imagemDetalhada : local.Imagem;
            Nome = local.Nome;
            Categoria = local.Categoria;
            Localizacao = local.Localizacao;
            Descricao = local.Descricao;
            Contagem = local.QuantidadeComentarios == 1
                ? "1 comentário" : $"{local.QuantidadeComentarios} comentários";

            foreach (var comentario in resposta.Comentarios)
            {
                Comentarios.Add(new ComentarioDoLocal(
                    comentario.Apelido, comentario.Texto, comentario.Status, comentario.CriadoEm, comentario.Respostas));
            }

            MostrarDetalhes = true;
        }
        catch (ExcecaoDaApi erro) { Erro = erro.Message; MostrarErro = true; throw; }
        finally
        {
            Carregando = false;
        }
    }
}
