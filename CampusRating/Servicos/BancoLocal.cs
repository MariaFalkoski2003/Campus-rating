using System.Text.Json;
using SQLite;

namespace CampusRating.Servicos;

// DAO local. As transações também impedem que respostas antigas repovoem o cache após sair.
public sealed class BancoLocal : IDisposable
{
    private readonly SQLiteConnection _banco;
    private readonly object _trava = new();
    private int _usuarioId;
    private long _geracao;
    public string Caminho { get; }

    public BancoLocal(string caminho)
    {
        Caminho = caminho;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(caminho))!);
        _banco = new SQLiteConnection(caminho, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        _banco.ExecuteScalar<int>("PRAGMA secure_delete = ON");
        _banco.CreateTable<LocalArmazenado>();
        _banco.CreateTable<RelatoArmazenado>();
        _banco.CreateTable<AvisoArmazenado>();
        _banco.CreateTable<ConsultaArmazenada>();
        _usuarioId = _banco.Find<ConsultaArmazenada>("proprietario")?.UsuarioId ?? 0;
    }

    internal bool PertenceAoUsuario(int usuarioId)
    {
        lock (_trava) return usuarioId > 0 && _usuarioId == usuarioId;
    }

    public void AtivarUsuario(int usuarioId)
    {
        lock (_trava)
        {
            if (_usuarioId != usuarioId) LimparPessoais();
            _usuarioId = usuarioId;
            _geracao++;
            _banco.InsertOrReplace(new ConsultaArmazenada { Chave = "proprietario", UsuarioId = usuarioId });
        }
    }

    public void EncerrarSessao()
    {
        lock (_trava)
        {
            _usuarioId = 0;
            _geracao++;
            LimparPessoais();
        }
    }

    private void LimparPessoais() => _banco.RunInTransaction(() =>
    {
        _banco.DeleteAll<RelatoArmazenado>();
        _banco.DeleteAll<AvisoArmazenado>();
        _banco.Execute("DELETE FROM consultas_salvas WHERE UsuarioId <> 0");
    });

    public ContextoDoBanco CapturarContexto()
    {
        lock (_trava)
        {
            if (_usuarioId <= 0) throw new ExcecaoDaApi("token_ausente", "Entre novamente para continuar.");
            return new(_usuarioId, _geracao);
        }
    }

    public void ConferirContexto(ContextoDoBanco contexto)
    {
        lock (_trava)
            if (contexto.UsuarioId != _usuarioId || contexto.Geracao != _geracao)
                throw new ExcecaoDaApi("consulta_cancelada", "A conta mudou durante a consulta. Atualize a tela.");
    }

    private T Usar<T>(ContextoDoBanco contexto, Func<T> acao)
    {
        lock (_trava)
        {
            ConferirContexto(contexto);
            try { return acao(); }
            catch (Exception erro) when (erro is SQLiteException or IOException or UnauthorizedAccessException or JsonException)
            {
                throw new ExcecaoDaApi("banco_local", "Não foi possível acessar os dados salvos no dispositivo.", origem: erro);
            }
        }
    }

    private ConsultaArmazenada? Marca(string chave) => _banco.Find<ConsultaArmazenada>(chave);
    private void Marcar(string chave, int usuarioId, bool completa = true, string? json = null) =>
        _banco.InsertOrReplace(new ConsultaArmazenada
        {
            Chave = chave, UsuarioId = usuarioId, Completa = completa,
            AtualizadoEm = DateTimeOffset.UtcNow.ToString("O"), ConteudoJson = json
        });

    private static LocalArmazenado Armazenar(LocalDaApi local, int ordem = 0) => new()
    {
        Id = local.Id, Nome = local.Nome, Categoria = local.Categoria, Localizacao = local.Localizacao,
        Descricao = local.Descricao, Imagem = local.Imagem, QuantidadeComentarios = local.QuantidadeComentarios, Ordem = ordem
    };
    private static LocalDaApi Apresentar(LocalArmazenado local) => new(local.Id, local.Nome, local.Categoria,
        local.Localizacao, local.Descricao, local.Imagem, local.QuantidadeComentarios);
    private static RelatoDaApi Apresentar(RelatoArmazenado relato) => new(relato.Id, relato.LocalId, relato.LocalNome,
        relato.Apelido, relato.Texto, relato.Status, relato.CriadoEm,
        relato.RespostasJson is null ? null : JsonSerializer.Deserialize<List<RespostaDoRelato>>(relato.RespostasJson));

    public void SalvarLocais(ContextoDoBanco contexto, IReadOnlyList<LocalDaApi> locais) => Usar(contexto, () =>
    {
        _banco.RunInTransaction(() =>
        {
            _banco.DeleteAll<LocalArmazenado>();
            for (var i = 0; i < locais.Count; i++) _banco.Insert(Armazenar(locais[i], i));
            Marcar("locais", 0);
        });
        return true;
    });

    public CopiaLocal<IReadOnlyList<LocalDaApi>>? LerLocais(ContextoDoBanco contexto) => Usar(contexto, () =>
    {
        var marca = Marca("locais");
        return marca is null ? null : new CopiaLocal<IReadOnlyList<LocalDaApi>>(
            _banco.Table<LocalArmazenado>().OrderBy(local => local.Ordem).ToList().Select(Apresentar).ToList(), marca.AtualizadoEm, marca.Completa);
    });

    public void SalvarDetalhes(ContextoDoBanco contexto, RespostaDeDetalhesDoLocal resposta) => Usar(contexto, () =>
    {
        _banco.RunInTransaction(() =>
        {
            var ordem = _banco.Find<LocalArmazenado>(resposta.Local.Id)?.Ordem ?? 0;
            _banco.InsertOrReplace(Armazenar(resposta.Local, ordem));
            Marcar($"local:{resposta.Local.Id}", contexto.UsuarioId, json: JsonSerializer.Serialize(resposta));
            if (Marca("locais") is null) Marcar("locais", 0, completa: false);
        });
        return true;
    });

    public CopiaLocal<RespostaDeDetalhesDoLocal>? LerDetalhes(ContextoDoBanco contexto, int id) => Usar(contexto, () =>
    {
        var marca = Marca($"local:{id}");
        return marca?.UsuarioId != contexto.UsuarioId || marca.ConteudoJson is null ? null
            : new CopiaLocal<RespostaDeDetalhesDoLocal>(JsonSerializer.Deserialize<RespostaDeDetalhesDoLocal>(marca.ConteudoJson)!, marca.AtualizadoEm);
    });

    private void GravarRelato(ContextoDoBanco contexto, RelatoDaApi relato, bool detalhes)
    {
        var anterior = _banco.Find<RelatoArmazenado>(relato.Id);
        _banco.InsertOrReplace(new RelatoArmazenado
        {
            Id = relato.Id, UsuarioId = contexto.UsuarioId, LocalId = relato.LocalId, LocalNome = relato.LocalNome,
            Apelido = relato.Apelido, Texto = relato.Texto, Status = relato.Status, CriadoEm = relato.CriadoEm,
            RespostasJson = detalhes ? JsonSerializer.Serialize(relato.Respostas ?? []) : anterior?.RespostasJson,
            DetalhesAtualizadosEm = detalhes ? DateTimeOffset.UtcNow.ToString("O") : anterior?.DetalhesAtualizadosEm
        });
    }

    public void SalvarMeusRelatos(ContextoDoBanco contexto, IReadOnlyList<RelatoDaApi> relatos) => Usar(contexto, () =>
    {
        _banco.RunInTransaction(() =>
        {
            var ids = relatos.Select(relato => relato.Id).ToHashSet();
            foreach (var existente in _banco.Table<RelatoArmazenado>().Where(relato => relato.UsuarioId == contexto.UsuarioId).ToList())
                if (!ids.Contains(existente.Id)) _banco.Delete(existente);
            foreach (var relato in relatos) GravarRelato(contexto, relato, detalhes: relato.Respostas is not null);
            Marcar("meus-relatos", contexto.UsuarioId);
        });
        return true;
    });

    public CopiaLocal<RespostaDeMeusRelatos>? LerMeusRelatos(ContextoDoBanco contexto) => Usar(contexto, () =>
    {
        var marca = Marca("meus-relatos");
        return marca?.UsuarioId != contexto.UsuarioId ? null : new CopiaLocal<RespostaDeMeusRelatos>(new(
            _banco.Table<RelatoArmazenado>().Where(relato => relato.UsuarioId == contexto.UsuarioId)
                .OrderByDescending(relato => relato.Id).ToList().Select(Apresentar).ToList()), marca.AtualizadoEm, marca.Completa);
    });

    public void SalvarMeuRelato(ContextoDoBanco contexto, RelatoDaApi relato) => Usar(contexto, () =>
    {
        _banco.RunInTransaction(() =>
        {
            GravarRelato(contexto, relato, detalhes: true);
            if (Marca("meus-relatos") is null) Marcar("meus-relatos", contexto.UsuarioId, completa: false);
        });
        return true;
    });

    public CopiaLocal<RespostaDeRelato>? LerMeuRelato(ContextoDoBanco contexto, int id) => Usar(contexto, () =>
    {
        var relato = _banco.Find<RelatoArmazenado>(id);
        return relato?.UsuarioId != contexto.UsuarioId || relato.DetalhesAtualizadosEm is null ? null
            : new CopiaLocal<RespostaDeRelato>(new(Apresentar(relato)), relato.DetalhesAtualizadosEm);
    });

    public void SalvarEnvio(ContextoDoBanco contexto, RelatoDaApi relato) => Usar(contexto, () =>
    {
        _banco.RunInTransaction(() =>
        {
            var novo = _banco.Find<RelatoArmazenado>(relato.Id) is null;
            GravarRelato(contexto, relato, detalhes: true);
            if (Marca("meus-relatos") is null) Marcar("meus-relatos", contexto.UsuarioId, completa: false);
            var local = _banco.Find<LocalArmazenado>(relato.LocalId);
            if (novo && local is not null) { local.QuantidadeComentarios++; _banco.Update(local); }
            var detalhe = LerDetalhes(contexto, relato.LocalId);
            if (detalhe is not null)
            {
                var comentarios = detalhe.Dados.Comentarios.Where(comentario => comentario.Id != relato.Id).ToList();
                comentarios.Insert(0, new(relato.Id, relato.Apelido, relato.Texto, relato.Status, relato.CriadoEm, []));
                var atualizado = detalhe.Dados with
                {
                    Local = detalhe.Dados.Local with { QuantidadeComentarios = detalhe.Dados.Local.QuantidadeComentarios + (novo ? 1 : 0) },
                    Comentarios = comentarios
                };
                Marcar($"local:{relato.LocalId}", contexto.UsuarioId, json: JsonSerializer.Serialize(atualizado));
            }
        });
        return true;
    });

    public void SalvarAvisos(ContextoDoBanco contexto, IReadOnlyList<AvisoDaApi> avisos) => Usar(contexto, () =>
    {
        _banco.RunInTransaction(() =>
        {
            _banco.Execute("DELETE FROM avisos WHERE UsuarioId = ?", contexto.UsuarioId);
            foreach (var aviso in avisos) _banco.Insert(new AvisoArmazenado
            {
                Id = aviso.Id, UsuarioId = contexto.UsuarioId, RelatoId = aviso.RelatoId, LocalNome = aviso.LocalNome,
                Texto = aviso.Texto, CriadoEm = aviso.CriadoEm, Lida = aviso.Lida
            });
            Marcar("avisos", contexto.UsuarioId);
        });
        return true;
    });

    public CopiaLocal<RespostaDeAvisos>? LerAvisos(ContextoDoBanco contexto) => Usar(contexto, () =>
    {
        var marca = Marca("avisos");
        return marca?.UsuarioId != contexto.UsuarioId ? null : new CopiaLocal<RespostaDeAvisos>(new(
            _banco.Table<AvisoArmazenado>().Where(aviso => aviso.UsuarioId == contexto.UsuarioId).OrderByDescending(aviso => aviso.Id)
            .ToList().Select(aviso => new AvisoDaApi(aviso.Id, aviso.RelatoId, aviso.LocalNome, aviso.Texto, aviso.CriadoEm, aviso.Lida)).ToList()), marca.AtualizadoEm);
    });

    public void MarcarLido(ContextoDoBanco contexto, int id) => Usar(contexto, () =>
    {
        _banco.Execute("UPDATE avisos SET Lida = 1 WHERE Id = ? AND UsuarioId = ?", id, contexto.UsuarioId);
        return true;
    });

    public void Dispose() { lock (_trava) _banco.Dispose(); }
}
