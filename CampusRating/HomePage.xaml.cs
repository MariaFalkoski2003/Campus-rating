using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace CampusRating;

public record CampusPlace(string Name, string Category, string Location,
                          string Description, string Rating, string Image);

[QueryProperty(nameof(Nickname), "nickname")]
public partial class HomePage : ContentPage
{
    private readonly CampusPlace[] _allPlaces =
    [
        new("Biblioteca Central", "Biblioteca", "Prédio Principal, Térreo",
            "Acervo completo, salas de estudo e computadores disponíveis.", "4,8", "campus_library.jpg"),
        new("Bloco B - Banheiros", "Banheiros", "Bloco B, Térreo",
            "Banheiros masculinos, femininos e acessíveis.", "4,6", "campus_bathrooms.jpg"),
        new("Cantina", "Cantina", "Prédio de Convivência",
            "Refeições, lanches e bebidas durante o expediente.", "4,5", "campus_canteen.jpg"),
        new("Laboratório 3", "Salas", "Bloco A, 2º Andar",
            "Laboratório de informática e pesquisas.", "4,7", "campus_laboratory.jpg")
    ];
    private string _category = "Todos";

    public ObservableCollection<CampusPlace> Places { get; } = new();

    public string Nickname
    {
        set => NicknameLabel.Text = string.IsNullOrWhiteSpace(value) ? "Seu perfil anônimo" : value;
    }

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;
        UpdatePlaces();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => UpdatePlaces();

    private void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button selected) return;
        _category = selected.CommandParameter?.ToString() ?? "Todos";
        foreach (var button in CategoryButtons.Children.OfType<Button>())
        {
            var active = button == selected;
            button.BackgroundColor = Color.FromArgb(active ? "#F1EDFF" : "#FFFFFF");
            button.TextColor = Color.FromArgb(active ? "#8065F1" : "#7D7B85");
            button.BorderColor = Color.FromArgb(active ? "#CEC0FF" : "#E5E3EA");
        }
        UpdatePlaces();
    }

    private void UpdatePlaces()
    {
        var query = Normalize(SearchEntry.Text?.Trim() ?? "");
        Places.Clear();
        foreach (var place in _allPlaces)
        {
            if (_category != "Todos" && place.Category != _category) continue;
            if (!Normalize($"{place.Name} {place.Location}").Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
            Places.Add(place);
        }
    }

    private static string Normalize(string text)
    {
        var chars = text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    }

    private async void OnPlaceTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: CampusPlace place })
            await DisplayAlertAsync(place.Name,
                $"{place.Location}\n\n{place.Description}\n\nNota de exemplo: {place.Rating}", "Voltar");
    }

    private void OnPlacesTabTapped(object? sender, TappedEventArgs e) => SelectTab("places");
    private void OnProfileTabTapped(object? sender, TappedEventArgs e) => SelectTab("profile");
    private void OnNoticesTabTapped(object? sender, TappedEventArgs e) => SelectTab("notices");

    private void SelectTab(string tab)
    {
        PlacesPanel.IsVisible = tab == "places";
        ProfilePanel.IsVisible = tab == "profile";
        NoticesPanel.IsVisible = tab == "notices";
        PlacesTabIcon.Source = tab == "places" ? "location_purple.png" : "location_gray.png";
        ProfileTabIcon.Source = tab == "profile" ? "profile_purple.png" : "profile_gray.png";
        NoticesTabIcon.Source = tab == "notices" ? "bell_purple.png" : "bell_gray.png";
        PlacesTabLabel.TextColor = Color.FromArgb(tab == "places" ? "#8065F1" : "#898794");
        ProfileTabLabel.TextColor = Color.FromArgb(tab == "profile" ? "#8065F1" : "#898794");
        NoticesTabLabel.TextColor = Color.FromArgb(tab == "notices" ? "#8065F1" : "#898794");
    }

    private async void OnBackToRegistrationClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
