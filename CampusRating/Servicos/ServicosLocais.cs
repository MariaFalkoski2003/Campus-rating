namespace CampusRating.Servicos;

public static class ServicosLocais
{
    private static readonly Lazy<BancoLocal> _banco = new(() =>
        new BancoLocal(Path.Combine(FileSystem.AppDataDirectory, "campusrating-local.sqlite")));
    private static readonly Lazy<DadosDoCampus> _dados = new(() => new(Banco));
    public static BancoLocal Banco
    {
        get
        {
            try { return _banco.Value; }
            catch (Exception erro) when (erro is SQLite.SQLiteException or IOException or UnauthorizedAccessException)
            {
                throw new ExcecaoDaApi("banco_local", "Não foi possível abrir o banco no dispositivo. Verifique o espaço disponível e reinicie o aplicativo.", origem: erro);
            }
        }
    }
    public static DadosDoCampus Dados => _dados.Value;
}
