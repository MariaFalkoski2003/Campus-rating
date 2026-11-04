using CampusRating.Servicos;

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
        var fechar = new Button { Text = "Fechar", BackgroundColor = Color.FromArgb("#8065F1") };
        var pagina = new ContentPage
        {
            Title = "Regras da plataforma", BackgroundColor = Color.FromArgb("#FDFCFF"),
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = 24, Spacing = 20, MaximumWidthRequest = 640, HorizontalOptions = LayoutOptions.Fill,
                    Children = { new Label { Text = Regras, TextColor = Color.FromArgb("#25242C"), FontSize = 16 }, fechar }
                }
            }
        };
        fechar.Clicked += async (_, _) => await pagina.Navigation.PopModalAsync();
        await origem.Navigation.PushModalAsync(pagina);
    }

    public static async Task<bool> FormularioAsync(Page origem, string titulo, string descricao,
        CampoDoFormulario[] campos, Func<IReadOnlyDictionary<string, string>, Task> salvar,
        string textoBotao = "Salvar alterações", bool perigoso = false)
    {
        if (origem.Navigation.ModalStack.Count > 0) return false;
        var pagina = new FormularioModal(titulo, descricao, campos, salvar, textoBotao, perigoso);
        await origem.Navigation.PushModalAsync(pagina);
        return await pagina.Resultado;
    }

    public static async Task MostrarRelatoAsync(Page origem, RelatoDaApi relato, string mensagem = "")
    {
        if (origem.Navigation.ModalStack.Count > 0) return;
        var conteudo = new VerticalStackLayout { Padding = 24, Spacing = 16, MaximumWidthRequest = 640 };
        if (!string.IsNullOrWhiteSpace(mensagem))
            conteudo.Add(new Label { Text = mensagem.Trim(), FontSize = 14, TextColor = Color.FromArgb("#6251D2") });
        conteudo.Add(new Label { Text = relato.LocalNome, FontSize = 26, TextColor = Color.FromArgb("#25242C") });
        conteudo.Add(new Label { Text = $"{relato.Apelido} · {FormatacaoDoRelato.Data(relato.CriadoEm)}", TextColor = Color.FromArgb("#625F6D") });
        conteudo.Add(new Label { Text = FormatacaoDoRelato.Status(relato.Status), TextColor = Color.FromArgb("#6251D2") });
        conteudo.Add(new Label { Text = relato.Texto, FontSize = 16, TextColor = Color.FromArgb("#25242C") });
        conteudo.Add(new Label { Text = "Respostas da administração", FontSize = 20, TextColor = Color.FromArgb("#25242C") });
        if (relato.Respostas is not { Count: > 0 })
            conteudo.Add(new Label { Text = "Este relato ainda não recebeu uma resposta.", TextColor = Color.FromArgb("#625F6D") });
        foreach (var resposta in relato.Respostas ?? [])
        {
            conteudo.Add(new Label { Text = FormatacaoDoRelato.Data(resposta.CriadoEm), FontSize = 13, TextColor = Color.FromArgb("#625F6D") });
            conteudo.Add(new Label { Text = resposta.Texto, FontSize = 16, TextColor = Color.FromArgb("#25242C") });
        }
        var fechar = new Button { Text = "Fechar", BackgroundColor = Color.FromArgb("#8065F1") };
        conteudo.Add(fechar);
        var pagina = new ContentPage { BackgroundColor = Color.FromArgb("#FDFCFF"), Content = new ScrollView { Content = conteudo } };
        fechar.Clicked += async (_, _) => await pagina.Navigation.PopModalAsync();
        await origem.Navigation.PushModalAsync(pagina);
    }

    private sealed class FormularioModal : ContentPage
    {
        private readonly TaskCompletionSource<bool> _resultado = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _enviando;
        private bool _salvou;
        private bool _fechando;
        public Task<bool> Resultado => _resultado.Task;

        public FormularioModal(string titulo, string descricao, CampoDoFormulario[] campos,
            Func<IReadOnlyDictionary<string, string>, Task> salvar, string textoBotao, bool perigoso)
        {
            BackgroundColor = Color.FromArgb("#FDFCFF");
            var layout = new VerticalStackLayout { Padding = 24, Spacing = 16, MaximumWidthRequest = 640 };
            layout.Add(new Label { Text = titulo, FontSize = 28, TextColor = Color.FromArgb("#25242C") });
            layout.Add(new Label { Text = descricao, FontSize = 15, TextColor = Color.FromArgb("#625F6D") });
            var entradas = new Dictionary<string, InputView>();
            foreach (var campo in campos)
            {
                layout.Add(new Label { Text = campo.Rotulo, FontSize = 15, TextColor = Color.FromArgb("#25242C") });
                InputView entrada = campo.Multilinha
                    ? new Editor { HeightRequest = 180, MaxLength = 2000, AutoSize = EditorAutoSizeOption.TextChanges }
                    : new Entry { IsPassword = campo.Senha, MaxLength = campo.Senha ? 64 : 254 };
                entrada.Text = campo.Valor;
                entrada.Keyboard = campo.Teclado ?? Keyboard.Default;
                entrada.TextColor = Color.FromArgb("#25242C");
                entrada.BackgroundColor = Colors.White;
                SemanticProperties.SetDescription(entrada, campo.Rotulo);
                entradas[campo.Nome] = entrada;
                layout.Add(entrada);
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
            var erroLabel = new Label { IsVisible = false, TextColor = Color.FromArgb("#A1283B"), FontSize = 14 };
            var progresso = new ActivityIndicator { IsVisible = false, Color = Color.FromArgb("#8065F1") };
            var botao = new Button { Text = textoBotao, BackgroundColor = Color.FromArgb(perigoso ? "#A1283B" : "#8065F1") };
            var cancelar = new Button { Text = "Cancelar", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#6251D2") };
            layout.Add(erroLabel);
            layout.Add(progresso);
            layout.Add(botao);
            layout.Add(cancelar);
            Content = new ScrollView { Content = layout };
            cancelar.Clicked += async (_, _) => { if (!_enviando) await FecharAsync(); };
            botao.Clicked += async (_, _) =>
            {
                if (_enviando) return;
                _enviando = true;
                erroLabel.IsVisible = false;
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
                    erroLabel.Text = erro.Message;
                    erroLabel.IsVisible = true;
                    foreach (var entrada in entradas.Values) entrada.IsEnabled = true;
                    if (erro.Campo is not null && entradas.TryGetValue(erro.Campo, out var campo)) campo.Focus();
                    if (erro.Codigo is "token_ausente" or "token_invalido" or "usuario_nao_encontrado")
                    {
                        await SessaoDoUsuario.EncerrarAsync();
                        await DisplayAlertAsync("Sessão expirada", "Entre novamente para continuar.", "Entendi");
                        await FecharAsync();
                        await Shell.Current.GoToAsync("//LoginPage");
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
