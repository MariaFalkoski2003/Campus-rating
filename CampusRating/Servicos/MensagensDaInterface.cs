using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace CampusRating.Servicos;

// Feedback de apresentação. Não interfere no resultado de uma operação já confirmada.
public static class MensagensDaInterface
{
    public static Task ToastAsync(string mensagem) => MostrarAsync(mensagem, snackbar: false);
    public static Task SnackbarAsync(string mensagem) => MostrarAsync(mensagem, snackbar: true);

    private static Task MostrarAsync(string mensagem, bool snackbar) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
#if WINDOWS
                if (Application.Current?.Windows.FirstOrDefault()?.Page is { } pagina)
                    await pagina.DisplayAlertAsync("Campus Rating", mensagem, "Entendi");
#else
                if (snackbar)
                    await Snackbar.Make(mensagem, action: () => { }, actionButtonText: "Entendi",
                        duration: TimeSpan.FromSeconds(8)).Show();
                else
                    await Toast.Make(mensagem, ToastDuration.Long).Show();
#endif
            }
            catch (OperationCanceledException) { /* Um aviso mais recente pode substituir este. */ }
            catch (Exception erro)
            {
                // Se a plataforma não puder exibir o aviso, preservar a confirmação ao aluno.
                System.Diagnostics.Debug.WriteLine($"Aviso indisponível: {erro.GetType().Name}");
                if (Application.Current?.Windows.FirstOrDefault()?.Page is { } pagina)
                    await pagina.DisplayAlertAsync("Campus Rating", mensagem, "Entendi");
            }
        });
}
