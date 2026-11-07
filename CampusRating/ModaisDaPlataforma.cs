using CampusRating.Servicos;
using CampusRating.Componentes;
using Microsoft.Maui.Controls.Shapes;

namespace CampusRating;

public record CampoDoFormulario(string Nome, string Rotulo, bool Senha = false,
    bool Multilinha = false, string Valor = "", Keyboard? Teclado = null);

public static class ModaisDaPlataforma
{
    public const string Regras = """
        Regras da plataforma — versão 1

        1. Acesso institucional
        Utilize seu e-mail @aluno.uricer.edu.br e mantenha sua senha em sigilo.

        2. Participação respeitosa
        Descreva fatos e necessidades relacionados aos espaços do campus. Não publique ofensas, ameaças, discriminação, spam ou informações falsas.

        3. Proteção de informações pessoais
        Não inclua nomes reais, e-mails, telefones, senhas ou dados de outras pessoas nos relatos. O apelido sorteado identifica suas publicações. Seu e-mail é usado na conta e não aparece nos comentários públicos.

        4. Relatos e acompanhamento
        Um relato enviado começa como Pendente. Em análise indica que a administração iniciou o acompanhamento. Resolvido identifica o encerramento informado pela administração. O envio não garante um prazo de atendimento.

        5. Dados da conta
        O sistema armazena e-mail, apelido, registro do aceite e senha protegida por hash. Relatos, respostas e seus andamentos são vinculados aos registros da plataforma. O uso de apelido não significa ausência de vínculo com a conta.
        O aplicativo mantém cópias de locais, relatos e avisos no dispositivo para consulta sem conexão. Ao sair ou excluir a conta, as cópias pessoais são removidas. A sessão fica no armazenamento seguro do dispositivo; a senha não é salva no banco local.

        6. Alterações e exclusão
        Você pode alterar seu e-mail institucional e sua senha mediante a senha atual. O apelido permanece o original. Excluir a conta remove também seus relatos e as respostas vinculadas. Essa ação exige confirmação e não pode ser desfeita.
        """;

    public static async Task MostrarRegrasAsync(Page origem)
    {
        if (origem.Navigation.ModalStack.Count > 0) return;
        var fechar = new Button { Text = "Fechar", Style = (Style)Application.Current!.Resources["BotaoSecundario"] };
        var conteudo = new VerticalStackLayout
        {
            Padding = 24, Spacing = 20, MaximumWidthRequest = 640, HorizontalOptions = LayoutOptions.Fill
        };
        foreach (var trecho in Regras.Replace("\r", "").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var linhas = trecho.Split('\n', 2);
            conteudo.Add(new Label { Text = linhas[0], FontSize = linhas.Length == 1 ? 25 : 18, FontFamily = "OpenSansRegular" });
            if (linhas.Length > 1)
                conteudo.Add(new Label { Text = linhas[1], FontSize = 14, LineHeight = 1.35, TextColor = Color.FromArgb("#625D73") });
        }
        conteudo.Add(fechar);
        var pagina = new ContentPage
        {
            Title = "Regras da plataforma", BackgroundColor = Colors.White,
            Content = new ScrollView { Content = conteudo }
        };
        fechar.Clicked += async (_, _) => await pagina.Navigation.PopModalAsync();
        await origem.Navigation.PushModalAsync(pagina);
    }

    public static async Task<bool> FormularioAsync(Page origem, string titulo, string descricao,
        CampoDoFormulario[] campos, Func<IReadOnlyDictionary<string, string>, Task> salvar,
        string textoBotao = "Salvar alterações", bool perigoso = false)
    {
        if (origem.Navigation.ModalStack.Count > 0) return false;
        var pagina = new FormularioModal(origem, titulo, descricao, campos, salvar, textoBotao, perigoso);
        await origem.Navigation.PushModalAsync(pagina);
        return await pagina.Resultado;
    }

    public static async Task MostrarRelatoAsync(Page origem, RelatoDaApi relato, string mensagem = "")
    {
        if (origem.Navigation.ModalStack.Count > 0) return;
        var conteudo = new VerticalStackLayout { Padding = 24, Spacing = 14, MaximumWidthRequest = 640, HorizontalOptions = LayoutOptions.Fill };
        if (!string.IsNullOrWhiteSpace(mensagem))
            conteudo.Add(new AvisoDaInterface { Titulo = "Consulta no dispositivo", Mensagem = mensagem.Trim() });
        conteudo.Add(new Label { Text = relato.LocalNome, FontSize = 26, FontFamily = "OpenSansRegular" });
        conteudo.Add(new Label { Text = $"{relato.Apelido} · {FormatacaoDoRelato.Data(relato.CriadoEm)}", FontSize = 13, TextColor = Color.FromArgb("#625D73") });
        conteudo.Add(new SeloDeStatus { Status = relato.Status });
        conteudo.Add(new Label { Text = relato.Texto, FontSize = 15, LineHeight = 1.35 });
        conteudo.Add(new BoxView { Style = (Style)Application.Current!.Resources["Divisor"] });
        conteudo.Add(new Label { Text = "Respostas da administração", FontSize = 18, FontFamily = "OpenSansRegular" });
        if (relato.Respostas is not { Count: > 0 })
            conteudo.Add(new AvisoDaInterface { Titulo = "Aguardando resposta", Mensagem = "A administração ainda não respondeu a este relato." });
        foreach (var resposta in relato.Respostas ?? [])
        {
            var historico = new VerticalStackLayout { Spacing = 8 };
            historico.Add(new Label { Text = "Administração", FontSize = 14, TextColor = Color.FromArgb("#5140B5") });
            historico.Add(new Label { Text = FormatacaoDoRelato.Data(resposta.CriadoEm), FontSize = 12, TextColor = Color.FromArgb("#625D73") });
            historico.Add(new Label { Text = resposta.Texto, FontSize = 15, LineHeight = 1.3 });
            conteudo.Add(new Border
            {
                Padding = 14, StrokeThickness = 0, BackgroundColor = Color.FromArgb("#F5F1FF"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = historico
            });
        }
        var fechar = new Button { Text = "Fechar" };
        conteudo.Add(fechar);
        var pagina = new ContentPage { BackgroundColor = Colors.White, Content = new ScrollView { Content = conteudo } };
        fechar.Clicked += async (_, _) => await pagina.Navigation.PopModalAsync();
        await origem.Navigation.PushModalAsync(pagina);
    }

    private sealed class FormularioModal : ContentPage
    {
        private readonly TaskCompletionSource<bool> _resultado = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Page _origem;
        private bool _enviando;
        private bool _salvou;
        private bool _fechando;
        public Task<bool> Resultado => _resultado.Task;

        public FormularioModal(Page origem, string titulo, string descricao, CampoDoFormulario[] campos,
            Func<IReadOnlyDictionary<string, string>, Task> salvar, string textoBotao, bool perigoso)
        {
            _origem = origem;
            BackgroundColor = Colors.White;
            var layout = new VerticalStackLayout { Padding = 24, Spacing = 16, MaximumWidthRequest = 640, HorizontalOptions = LayoutOptions.Fill };
            layout.Add(new Label { Text = titulo, FontSize = 27, FontFamily = "OpenSansRegular" });
            layout.Add(new AvisoDaInterface
            {
                Titulo = perigoso ? "Exclusão definitiva" : "Antes de continuar", Mensagem = descricao,
                Tom = perigoso ? TomDoAviso.Erro : TomDoAviso.Informacao
            });
            var entradas = new Dictionary<string, InputView>();
            var bordas = new Dictionary<string, Border>();
            foreach (var campo in campos)
            {
                layout.Add(new Label { Text = campo.Rotulo, FontSize = 14 });
                InputView entrada = campo.Multilinha
                    ? new Editor { MinimumHeightRequest = 160, MaxLength = 2000, AutoSize = EditorAutoSizeOption.TextChanges }
                    : new Entry { IsPassword = campo.Senha, MaxLength = campo.Senha ? 64 : 254 };
                entrada.Text = campo.Valor;
                entrada.Keyboard = campo.Teclado ?? Keyboard.Default;
                SemanticProperties.SetDescription(entrada, campo.Rotulo);
                entradas[campo.Nome] = entrada;
                var borda = new Border { Style = (Style)Application.Current!.Resources["CampoDeEntrada"], Content = entrada };
                bordas[campo.Nome] = borda;
                layout.Add(borda);
                if (entrada is Editor editor)
                {
                    var contador = new Label
                    {
                        Text = $"{editor.Text?.Length ?? 0}/2.000 caracteres",
                        FontSize = 13, TextColor = Color.FromArgb("#625F6D"),
                        HorizontalTextAlignment = TextAlignment.End
                    };
                    editor.TextChanged += (_, e) => contador.Text = $"{e.NewTextValue?.Length ?? 0}/2.000 caracteres";
                    layout.Add(contador);
                }
            }
            var erroAviso = new AvisoDaInterface { Tom = TomDoAviso.Erro, Titulo = titulo == "Enviar relato" ? "Revise o relato" : "Revise os dados" };
            var progresso = new ActivityIndicator { IsVisible = false, Color = Color.FromArgb("#6251D2") };
            var botao = new Button { Text = textoBotao, Style = (Style)Application.Current!.Resources[perigoso ? "BotaoPerigoso" : "BotaoPrimario"] };
            var cancelar = new Button { Text = "Cancelar", Style = (Style)Application.Current!.Resources["BotaoSecundario"] };
            layout.Add(erroAviso);
            layout.Add(progresso);
            layout.Add(botao);
            layout.Add(cancelar);
            Content = new ScrollView { Content = layout };
            cancelar.Clicked += async (_, _) => { if (!_enviando) await FecharAsync(); };
            botao.Clicked += async (_, _) =>
            {
                if (_enviando) return;
                _enviando = true;
                erroAviso.Mensagem = "";
                foreach (var borda in bordas.Values) borda.Stroke = Color.FromArgb("#CFC4E7");
                try
                {
                    await FeedbackDeToque.AnimarToqueAsync(botao);
                    botao.IsEnabled = false;
                    cancelar.IsEnabled = false;
                    botao.Text = "Aguarde...";
                    progresso.IsVisible = true;
                    progresso.IsRunning = true;
                    foreach (var entrada in entradas.Values) entrada.IsEnabled = false;
                    await salvar(entradas.ToDictionary(par => par.Key, par => par.Value.Text ?? ""));
                    _salvou = true;
                    await FecharAsync();
                }
                catch (ExcecaoDaApi erro)
                {
                    erroAviso.Mensagem = erro.Message;
                    foreach (var entrada in entradas.Values) entrada.IsEnabled = true;
                    if (erro.Campo is not null && entradas.TryGetValue(erro.Campo, out var campo))
                    {
                        bordas[erro.Campo].Stroke = Color.FromArgb("#B13246");
                        campo.Focus();
                    }
                    if (erro.Codigo is "token_ausente" or "token_invalido" or "usuario_nao_encontrado")
                    {
                        await DisplayAlertAsync("Sessão expirada", "Entre novamente para continuar.", "Entendi");
                        await FecharAsync();
                        await NavegacaoDoAplicativo.MostrarLoginAsync(_origem);
                    }
                }
                finally
                {
                    _enviando = false;
                    botao.IsEnabled = true;
                    cancelar.IsEnabled = true;
                    botao.Text = textoBotao;
                    progresso.IsRunning = false;
                    progresso.IsVisible = false;
                    foreach (var entrada in entradas.Values) entrada.IsEnabled = true;
                }
            };
        }

        private async Task FecharAsync()
        {
            if (_fechando) return;
            _fechando = true;
            try
            {
                await Navigation.PopModalAsync();
                _resultado.TrySetResult(_salvou);
            }
            finally { _fechando = false; }
        }

        protected override bool OnBackButtonPressed()
        {
            if (!_enviando) _ = FecharAsync();
            return true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Ir para segundo plano não significa que o formulário foi cancelado.
            if (!_fechando && !Navigation.ModalStack.Contains(this))
                _resultado.TrySetResult(_salvou);
        }
    }
}
