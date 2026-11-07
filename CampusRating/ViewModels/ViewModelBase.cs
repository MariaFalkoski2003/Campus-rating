using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CampusRating.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Definir<T>(ref T campo, T valor, [CallerMemberName] string? nome = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor)) return;
        campo = valor;
        Notificar(nome);
    }
    protected void Notificar([CallerMemberName] string? nome = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}
