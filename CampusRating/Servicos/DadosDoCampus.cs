namespace CampusRating.Servicos;

public sealed record ConsultaSincronizada<T>(T Dados, string Mensagem = "", bool Offline = false);

// A API confirma as alterações; o SQLite guarda apenas cópias para consulta.
public sealed class DadosDoCampus(BancoLocal banco)
{
    internal async Task<ConsultaSincronizada<T>> ConsultarAsync<T>(Func<Task<T>> consultar,
        Action<ContextoDoBanco, T> salvar, Func<ContextoDoBanco, CopiaLocal<T>?> ler)
    {
        var contexto = banco.CapturarContexto();
        T dados;
        try { dados = await consultar(); }
        catch (ExcecaoDaApi erro)
        {
            banco.ConferirContexto(contexto);
            if (erro.Codigo != "sem_conexao") throw;
            var copia = ler(contexto) ?? throw new ExcecaoDaApi("sem_dados_locais",
                "Sem conexão com o servidor. Abra esta tela com conexão primeiro para salvar os dados no dispositivo.");
            var data = DateTimeOffset.TryParse(copia.AtualizadoEm, out var instante)
                ? instante.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : copia.AtualizadoEm;
            return new(copia.Dados, $"Sem conexão · cópia salva em {data}." +
                (copia.Completa ? "" : " Lista parcial: apenas registros já consultados ou enviados."), true);
        }
        banco.ConferirContexto(contexto);
        try { salvar(contexto, dados); }
        catch (ExcecaoDaApi erro) when (erro.Codigo == "banco_local")
        {
            return new(dados, "Dados do servidor carregados, mas a cópia no dispositivo não pôde ser atualizada.");
        }
        return new(dados);
    }

    public Task<ConsultaSincronizada<IReadOnlyList<LocalDaApi>>> ListarLocaisAsync() =>
        ConsultarAsync(ApiCampusRating.Instancia.ListarLocaisAsync, banco.SalvarLocais, banco.LerLocais);

    public Task<ConsultaSincronizada<RespostaDeDetalhesDoLocal>> ObterDetalhesDoLocalAsync(int id) =>
        ConsultarAsync(() => ApiCampusRating.Instancia.ObterDetalhesDoLocalAsync(id), banco.SalvarDetalhes,
            contexto => banco.LerDetalhes(contexto, id));

    public Task<ConsultaSincronizada<RespostaDeMeusRelatos>> ListarMeusRelatosAsync() =>
        ConsultarAsync(ApiCampusRating.Instancia.ListarMeusRelatosAsync,
            (contexto, resposta) => banco.SalvarMeusRelatos(contexto, resposta.Relatos), banco.LerMeusRelatos);

    public Task<ConsultaSincronizada<RespostaDeRelato>> ObterMeuRelatoAsync(int id) =>
        ConsultarAsync(() => ApiCampusRating.Instancia.ObterMeuRelatoAsync(id),
            (contexto, resposta) => banco.SalvarMeuRelato(contexto, resposta.Relato), contexto => banco.LerMeuRelato(contexto, id));

    public Task<ConsultaSincronizada<RespostaDeAvisos>> ListarAvisosAsync() =>
        ConsultarAsync(ApiCampusRating.Instancia.ListarAvisosAsync,
            (contexto, resposta) => banco.SalvarAvisos(contexto, resposta.Avisos), banco.LerAvisos);

    public async Task<string> EnviarRelatoAsync(int localId, string texto)
    {
        var contexto = banco.CapturarContexto();
        var resposta = await ApiCampusRating.Instancia.EnviarRelatoAsync(localId, texto);
        banco.ConferirContexto(contexto);
        try { banco.SalvarEnvio(contexto, resposta.Relato); }
        catch (ExcecaoDaApi erro) when (erro.Codigo == "banco_local")
        {
            // O envio já foi confirmado: não pedir repetição e criar um relato duplicado.
            return "O relato foi enviado, mas não foi possível salvar a cópia no dispositivo.";
        }
        return "";
    }

    public async Task<bool> MarcarAvisoLidoAsync(int id)
    {
        var contexto = banco.CapturarContexto();
        try { await ApiCampusRating.Instancia.MarcarAvisoLidoAsync(id); }
        catch (ExcecaoDaApi erro) when (erro.Codigo == "sem_conexao") { return false; }
        banco.ConferirContexto(contexto);
        try { banco.MarcarLido(contexto, id); }
        catch (ExcecaoDaApi erro) when (erro.Codigo == "banco_local") { /* A API já confirmou a leitura. */ }
        return true;
    }
}
