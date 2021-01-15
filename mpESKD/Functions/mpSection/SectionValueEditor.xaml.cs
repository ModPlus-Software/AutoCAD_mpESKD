namespace mpESKD.Functions.mpSection
{
    using System.Windows;
    using Base;

    /// <summary>
    /// Редактор значений разреза
    /// </summary>
    public partial class SectionValueEditor
    {
        private readonly Section _smartEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="SectionValueEditor"/> class.
        /// </summary>
        /// <param name="smartEntity">Редактируемый экземпляр интеллектуального объекта</param>
        public SectionValueEditor(SmartEntity smartEntity)
        {
            _smartEntity = (Section)smartEntity;
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("h79");

            SetValues();
        }

        private void SetValues()
        {
            TbDesignation.Text = _smartEntity.Designation;
            TbDesignationPrefix.Text = _smartEntity.DesignationPrefix;
            TbSheetNumber.Text = _smartEntity.SheetNumber;
            TbDesignation.Focus();
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            OnAccept();
            DialogResult = true;
        }

        private void OnAccept()
        {
            _smartEntity.Designation = TbDesignation.Text;
            _smartEntity.DesignationPrefix = TbDesignationPrefix.Text;
            _smartEntity.SheetNumber = TbSheetNumber.Text;

            if (ChkRestoreTextPosition.IsChecked.HasValue && ChkRestoreTextPosition.IsChecked.Value)
            {
                _smartEntity.AlongBottomShelfTextOffset = double.NaN;
                _smartEntity.AlongTopShelfTextOffset = double.NaN;
                _smartEntity.AcrossBottomShelfTextOffset = double.NaN;
                _smartEntity.AcrossTopShelfTextOffset = double.NaN;
            }
        }
    }
}
