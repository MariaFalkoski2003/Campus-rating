namespace CampusRating.Componentes;

public sealed class CabecalhoDoCampus : HorizontalStackLayout
{
    public CabecalhoDoCampus(string titulo = "Campus Rating")
    {
        Spacing = 10;
        VerticalOptions = LayoutOptions.Center;
        Children.Add(new Image { Source = "campus_rating_logo.png", WidthRequest = 26, HeightRequest = 26 });
        Children.Add(new Label
        {
            Text = titulo, FontFamily = "OpenSansRegular", FontSize = 17,
            TextColor = Color.FromArgb("#242039"), VerticalOptions = LayoutOptions.Center
        });
    }
}
