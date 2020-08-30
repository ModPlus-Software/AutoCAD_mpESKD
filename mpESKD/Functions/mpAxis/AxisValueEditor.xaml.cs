namespace mpESKD.Functions.mpAxis
{
    using System.Windows;
    using Base;
    using Base.Enums;

    /// <summary>
    /// Редактор значений прямой оси
    /// </summary>
    public partial class AxisValueEditor
    {
        private readonly Axis _smartEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisValueEditor"/> class.
        /// </summary>
        /// <param name="smartEntity">Редактируемый экземпляр интеллектуального объекта</param>
        public AxisValueEditor(SmartEntity smartEntity)
        {
            _smartEntity = (Axis)smartEntity;
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(Invariables.LangItem, "h41");

            SetValues();
        }

        private void SetValues()
        {
            // visibility
            ChangeOrientVisibility();
            if (_smartEntity.MarkersCount > 1)
            {
                ChangeSecondVisibility(true);
                ChangeThirdVisibility(_smartEntity.MarkersCount > 2);
            }
            else
            {
                ChangeSecondVisibility(false);
                ChangeThirdVisibility(false);
            }

            // values
            TbFirstPrefix.Text = _smartEntity.FirstTextPrefix;
            TbFirstText.Text = _smartEntity.FirstText;
            TbFirstSuffix.Text = _smartEntity.FirstTextSuffix;

            TbSecondPrefix.Text = _smartEntity.SecondTextPrefix;
            TbSecondText.Text = _smartEntity.SecondText;
            TbSecondSuffix.Text = _smartEntity.SecondTextSuffix;

            TbThirdPrefix.Text = _smartEntity.ThirdTextPrefix;
            TbThirdText.Text = _smartEntity.ThirdText;
            TbThirdSuffix.Text = _smartEntity.ThirdTextSuffix;

            TbBottomOrientText.Text = _smartEntity.BottomOrientText;
            TbTopOrientText.Text = _smartEntity.TopOrientText;

            // markers position
            CbMarkersPosition.SelectedItem = _smartEntity.MarkersPosition;

            // focus
            TbFirstText.Focus();
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            OnAccept();
            DialogResult = true;
        }

        private void OnAccept()
        {
            // values
            _smartEntity.FirstTextPrefix = TbFirstPrefix.Text;
            _smartEntity.FirstText = TbFirstText.Text;
            _smartEntity.FirstTextSuffix = TbFirstSuffix.Text;

            _smartEntity.SecondTextPrefix = TbSecondPrefix.Text;
            _smartEntity.SecondText = TbSecondText.Text;
            _smartEntity.SecondTextSuffix = TbSecondSuffix.Text;

            _smartEntity.ThirdTextPrefix = TbThirdPrefix.Text;
            _smartEntity.ThirdText = TbThirdText.Text;
            _smartEntity.ThirdTextSuffix = TbThirdSuffix.Text;

            _smartEntity.BottomOrientText = TbBottomOrientText.Text;
            _smartEntity.TopOrientText = TbTopOrientText.Text;

            // markers position
            _smartEntity.MarkersPosition = (AxisMarkersPosition)CbMarkersPosition.SelectedItem;
        }

        #region Visibility

        private void ChangeOrientVisibility()
        {
            if (_smartEntity.MarkersPosition == AxisMarkersPosition.Both || _smartEntity.MarkersPosition == AxisMarkersPosition.Top)
            {
                TbTopOrientText.Visibility = _smartEntity.TopOrientMarkerVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                TbTopOrientText.Visibility = Visibility.Collapsed;
            }

            if (_smartEntity.MarkersPosition == AxisMarkersPosition.Both || _smartEntity.MarkersPosition == AxisMarkersPosition.Bottom)
            {
                TbBottomOrientText.Visibility =
                    _smartEntity.BottomOrientMarkerVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                TbBottomOrientText.Visibility = Visibility.Collapsed;
            }
        }

        private void ChangeSecondVisibility(bool show)
        {
            if (show)
            {
                TbSecondPrefix.Visibility = TbSecondText.Visibility = TbSecondSuffix.Visibility = Visibility.Visible;
            }
            else
            {
                TbSecondPrefix.Visibility = TbSecondText.Visibility = TbSecondSuffix.Visibility = Visibility.Collapsed;
            }
        }

        private void ChangeThirdVisibility(bool show)
        {
            if (show)
            {
                TbThirdPrefix.Visibility = TbThirdText.Visibility = TbThirdSuffix.Visibility = Visibility.Visible;
            }
            else
            {
                TbThirdPrefix.Visibility = TbThirdText.Visibility = TbThirdSuffix.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}
