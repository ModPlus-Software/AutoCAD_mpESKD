namespace mpESKD.Functions.SearchEntitiesByValues
{
    using System.Windows.Controls;
    using Autodesk.AutoCAD.DatabaseServices;
    using Base;

    /// <summary>
    /// Логика взаимодействия для SearchEntitiesByValuesWindow.xaml
    /// </summary>
    public partial class SearchEntitiesByValuesWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchEntitiesByValuesWindow"/> class.
        /// </summary>
        public SearchEntitiesByValuesWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("tab12");
        }

        private void BlocksList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedIndex != -1 && lb.SelectedItem is BlockReference block)
            {
                ModPlus.Helpers.AutocadHelpers.ZoomToEntities(new[] { block.ObjectId });
                Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
            }
        }
    }
}
