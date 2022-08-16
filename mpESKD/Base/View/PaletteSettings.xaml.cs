namespace mpESKD.Base.View;

using System.Windows;

/// <summary>
/// Настройки палитры свойств интеллектуальных объектов
/// </summary>
public partial class PaletteSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaletteSettings"/> class.
    /// </summary>
    public PaletteSettings()
    {
        InitializeComponent();
        Title = ModPlusAPI.Language.GetItem("h1");
    }

    private void BtClose_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}