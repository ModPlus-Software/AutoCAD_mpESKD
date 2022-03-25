namespace mpESKD.Functions.mpLevelMark;

using System;
using System.Windows;
using Base;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Логика взаимодействия для LevelMarkAlignSetup.xaml
/// </summary>
public partial class LevelMarkAlignSetup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LevelMarkAlignSetup"/> class.
    /// </summary>
    public LevelMarkAlignSetup()
    {
        InitializeComponent();
        Title = ModPlusAPI.Language.GetItem("h110");
        Closed += OnClosed;
        ChkAlignArrowPoints.IsChecked =
            !bool.TryParse(
                UserConfigFile.GetValue(SmartEntityUtils.GetDescriptor<LevelMark>().Name, ChkAlignArrowPoints.Name), out var b) || b;
        ChkAlignBasePoints.IsChecked =
            !bool.TryParse(
                UserConfigFile.GetValue(SmartEntityUtils.GetDescriptor<LevelMark>().Name, ChkAlignBasePoints.Name), out b) || b;
    }

    private void OnClosed(object sender, EventArgs e)
    {
        UserConfigFile.SetValue(
            SmartEntityUtils.GetDescriptor<LevelMark>().Name,
            ChkAlignArrowPoints.Name,
            ChkAlignArrowPoints.IsChecked.ToString(),
            true);
        UserConfigFile.SetValue(
            SmartEntityUtils.GetDescriptor<LevelMark>().Name,
            ChkAlignBasePoints.Name,
            ChkAlignBasePoints.IsChecked.ToString(),
            true);
    }

    private void BtAccept_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void ChkAlignOption_OnChecked(object sender, RoutedEventArgs e)
    {
        ChangeButtonEnabledState();
    }

    private void ChkAlignOption_OnUnchecked(object sender, RoutedEventArgs e)
    {
        ChangeButtonEnabledState();
    }

    private void ChangeButtonEnabledState()
    {
        if (ChkAlignBasePoints.IsChecked == false && ChkAlignArrowPoints.IsChecked == false)
            BtAccept.IsEnabled = false;
        else 
            BtAccept.IsEnabled = true;
    }
}