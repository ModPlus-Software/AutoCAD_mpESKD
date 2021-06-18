namespace mpESKD.Functions.mpViewLabel
{
    using System;
    using Base.Abstractions;

    /// <summary>
    /// Логика взаимодействия для ViewDoubleClickEditControl.xaml
    /// </summary>
    public partial class ViewLabelDoubleClickEditControl : IDoubleClickEditControl
    {
        private ViewLabel _viewLabel;
        public ViewLabelDoubleClickEditControl()
        {
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
        }

        /// <inheritdoc/>
        public Type EntityType => typeof(ViewLabel);
        
        /// <inheritdoc/>
        public void Initialize(IWithDoubleClickEditor smartEntity)
        {
            if (!(smartEntity is ViewLabel section))
                throw new ArgumentException("Wrong type of entity");

            _viewLabel = section;
            
            TbDesignation.Text = _viewLabel.Designation;
            TbDesignationPrefix.Text = _viewLabel.DesignationPrefix;
            TbSheetNumber.Text = _viewLabel.SheetNumber;
            TbDesignation.Focus();
        }

        /// <inheritdoc/>
        public void OnAccept()
        {
            _viewLabel.Designation = TbDesignation.Text;
            _viewLabel.DesignationPrefix = TbDesignationPrefix.Text;
            _viewLabel.SheetNumber = TbSheetNumber.Text;

            if (ChkRestoreTextPosition.IsChecked.HasValue && ChkRestoreTextPosition.IsChecked.Value)
            {
                _viewLabel.AlongBottomShelfTextOffset = double.NaN;
                _viewLabel.AlongTopShelfTextOffset = double.NaN;
                _viewLabel.AcrossBottomShelfTextOffset = double.NaN;
                _viewLabel.AcrossTopShelfTextOffset = double.NaN;
            }
        }
    }
}
