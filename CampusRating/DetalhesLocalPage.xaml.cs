using System.Collections.ObjectModel;
using System.Globalization;
using CampusRating.Servicos;

namespace CampusRating;

public record ComentarioDoLocal(string Apelido, string Texto, string Status, string CriadoEm,
    List<RespostaDoRelato>? Respostas = null)
{
    public bool TemRespostas => Respostas is { Count: > 0 };
    public string RespostasTexto => string.Join("\n\n", (Respostas ?? []).Select(resposta =>
        $"Administração · {FormatacaoDoRelato.Data(resposta.CriadoEm)}\n{resposta.Texto}"));
    public string StatusTexto => Status switch
    {
        "pendente" => "Pendente",
        "em_analise" => "Em análise",
        "resolvido" => "Resolvido",
        _ => "Status indisponível"
    };

    public string DataTexto => DateTime.TryParse(CriadoEm, CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var data)
        ? data.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : CriadoEm;
}

public partial class DetalhesLocalPage : ContentPage, IQueryAttributable
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
    private bool _carregando;
    private bool _enviandoRelato;

    public ObservableCollection<ComentarioDoLocal> Comentarios { get; } = new();

    public DetalhesLocalPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _localId = query.TryGetValue("localId", out var valor)
            && int.TryParse(valor?.ToString(), out var id) ? id : 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_enviandoRelato) return;
        if (!SessaoDoUsuario.EstaAutenticado)
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        await CarregarLocalAsync();
    }

    private async Task CarregarLocalAsync()
    {
        if (_carregando) return;

        _carregando = true;
        LoadingPanel.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoadErrorPanel.IsVisible = false;
        CommentsCollection.IsVisible = false;
        SendPanel.IsVisible = false;
        Comentarios.Clear();

        try
        {
            var consulta = await _dados.ObterDetalhesDoLocalAsync(_localId);
            var resposta = consulta.Dados;
            LocalCacheInfoLabel.Text = consulta.Mensagem;
            LocalCacheInfoLabel.IsVisible = consulta.Mensagem.Length > 0;
            var local = resposta.Local;
            LocalImage.Source = ImagensDetalhadas.TryGetValue(local.Imagem, out var imagemDetalhada)
                ? imagemDetalhada : local.Imagem;
            LocalNameLabel.Text = local.Nome;
            LocalCategoryLabel.Text = local.Categoria;
            LocalLocationLabel.Text = local.Localizacao;
            LocalDescriptionLabel.Text = local.Descricao;
            CommentsCountLabel.Text = local.QuantidadeComentarios == 1
                ? "1 comentário" : $"{local.QuantidadeComentarios} comentários";

            foreach (var comentario in resposta.Comentarios)
            {
                Comentarios.Add(new ComentarioDoLocal(
                    comentario.Apelido, comentario.Texto, comentario.Status, comentario.CriadoEm, comentario.Respostas));
            }

            CommentsCollection.IsVisible = true;
            SendPanel.IsVisible = true;
        }
        catch (ExcecaoDaApi erro)
        {
            if (erro.Codigo is "token_ausente" or "token_invalido" or "usuario_nao_encontrado")
            {
                await SessaoDoUsuario.EncerrarAsync();
                await DisplayAlertAsync("Sessão expirada", "Entre novamente para consultar o local.", "Entendi");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            LoadErrorLabel.Text = erro.Message;
            LoadErrorPanel.IsVisible = true;
        }
        finally
        {
            _carregando = false;
            LoadingPanel.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void OnRetryClicked(object? sender, EventArgs e) => await CarregarLocalAsync();

    private async void OnSendReportClicked(object? sender, EventArgs e)
    {
        if (_enviandoRelato) return;
        _enviandoRelato = true;
        try
        {
            var avisoLocal = "";
            var enviou = await ModaisDaPlataforma.FormularioAsync(this, "Enviar relato",
                $"Local: {LocalNameLabel.Text}\nApelido: {SessaoDoUsuario.Usuario?.Apelido}\nNão inclua dados pessoais. Limite de 2.000 caracteres.",
                [new("texto", "Descreva a situação", Multilinha: true)],
                async campos =>
                {
                    var texto = campos["texto"].Trim();
                    if (string.IsNullOrWhiteSpace(texto))
                        throw new ExcecaoDaApi("texto_invalido", "Escreva o relato antes de enviar.", "texto");
                    if (texto.Length > 2000)
                        throw new ExcecaoDaApi("texto_invalido", "O relato deve ter até 2.000 caracteres.", "texto");
                    avisoLocal = await _dados.EnviarRelatoAsync(_localId, texto);
                }, "Enviar relato");
            if (enviou)
            {
                await DisplayAlertAsync("Relato enviado", "Seu relato foi registrado como Pendente." +
                    (avisoLocal.Length > 0 ? "\n" + avisoLocal : ""), "Entendi");
                await CarregarLocalAsync();
            }
        }
        finally { _enviandoRelato = false; }
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
