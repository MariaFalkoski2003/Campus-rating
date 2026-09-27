namespace CampusRating.Servicos;

public static class ConfiguracaoDaApi
{
    public const int Porta = 3000;

    public static string EnderecoBase => DeviceInfo.Platform == DevicePlatform.Android
        ? $"http://10.0.2.2:{Porta}"
        : $"http://localhost:{Porta}";
}
