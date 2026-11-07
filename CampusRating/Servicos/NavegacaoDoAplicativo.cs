namespace CampusRating.Servicos;

public static class NavegacaoDoAplicativo
{
    public static NavigationPage CriarLogin(bool administrador = false) => new(new LoginPage(administrador));

    public static Task MostrarInicioAsync(Page origem)
    {
        if (!SessaoDoUsuario.EstaAutenticado)
            throw new ExcecaoDaApi("token_ausente", "Entre novamente para continuar.");
        if (origem.Window is { } janela)
            janela.Page = SessaoDoUsuario.EhAdministrador
                ? new NavigationPage(new AdministracaoPage())
                {
                    BarBackgroundColor = Color.FromArgb("#F5F1FF"), BarTextColor = Color.FromArgb("#242039")
                }
                : new PaginaPrincipal();
        return Task.CompletedTask;
    }

    public static async Task MostrarLoginAsync(Page origem)
    {
        // Uma página já removida não pode encerrar uma nova sessão da janela.
        if (origem.Window is not { } janela) return;
        var administrador = SessaoDoUsuario.EhAdministrador
            || janela.Page is NavigationPage { RootPage: AdministracaoPage };
        await SessaoDoUsuario.EncerrarAsync();
        if (janela.Page is PaginaPrincipal principal) principal.LimparDados();
        // Trocar a raiz descarta as abas, detalhes e modais da conta anterior.
        janela.Page = CriarLogin(administrador);
    }

    public static async Task<bool> ConferirAdministradorAsync(Page origem)
    {
        if (SessaoDoUsuario.EhAdministrador) return true;
        await MostrarLoginAsync(origem);
        return false;
    }

    public static async Task<bool> TratarErroAdministrativoAsync(Page origem, ExcecaoDaApi erro)
    {
        if (origem.Window?.Page is not (PaginaPrincipal or NavigationPage { RootPage: AdministracaoPage })) return true;
        if (erro.Codigo is not ("token_ausente" or "token_invalido" or "usuario_nao_encontrado" or "acesso_negado"))
            return false;
        await origem.DisplayAlertAsync("Acesso administrativo", erro.Message, "Entendi");
        await MostrarLoginAsync(origem);
        return true;
    }
}
