namespace mpESKD.Functions.SearchEntities;

using System;
using System.Windows;
using System.Windows.Controls;
using Base;
using ModPlusAPI;

public partial class SearchEntitiesSettings
{
    public SearchEntitiesSettings()
    {
        InitializeComponent();
        Title = ModPlusAPI.Language.GetItem("h99");
    }

    private void BtCheckAll_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (var item in LbEntities.Items)
        {
            if (item is ListBoxItem listBoxItem && listBoxItem.Content is CheckBox checkBox)
            {
                checkBox.IsChecked = true;
            }
        }
    }

    private void BtUncheckAll_OnClick(object sender, RoutedEventArgs e)
    {
        foreach (var item in LbEntities.Items)
        {
            if (item is ListBoxItem listBoxItem && listBoxItem.Content is CheckBox checkBox)
            {
                checkBox.IsChecked = false;
            }
        }
    }

    private void SearchEntitiesSettings_OnLoaded(object sender, RoutedEventArgs e)
    {
        CbSearchProceedOption.SelectedIndex = int.TryParse(UserConfigFile.GetValue(Invariables.LangItem, "SearchProceedOption"), out var i) ? i : 1;
    }

    private void SearchEntitiesSettings_OnClosed(object sender, EventArgs e)
    {
        UserConfigFile.SetValue(Invariables.LangItem, "SearchProceedOption", CbSearchProceedOption.SelectedIndex.ToString(), true);
    }

    private void BtAccept_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}