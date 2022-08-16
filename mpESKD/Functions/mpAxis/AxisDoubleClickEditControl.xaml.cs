namespace mpESKD.Functions.mpAxis;

using System;
using System.Windows;
using Base.Abstractions;
using Base.Enums;

/// <summary>
/// Логика взаимодействия для AxisDoubleClickEditControl.xaml
/// </summary>
public partial class AxisDoubleClickEditControl : IDoubleClickEditControl
{
    private Axis _axis;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxisDoubleClickEditControl"/> class.
    /// </summary>
    public AxisDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc />
    public Type EntityType => typeof(Axis);

    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (!(smartEntity is Axis axis))
            throw new ArgumentException("Wrong type of entity");
            
        _axis = axis;

        // visibility
        ChangeOrientVisibility();
        if (_axis.MarkersCount > 1)
        {
            ChangeSecondVisibility(true);
            ChangeThirdVisibility(_axis.MarkersCount > 2);
        }
        else
        {
            ChangeSecondVisibility(false);
            ChangeThirdVisibility(false);
        }

        // values
        TbFirstPrefix.Text = _axis.FirstTextPrefix;
        TbFirstText.Text = _axis.FirstText;
        TbFirstSuffix.Text = _axis.FirstTextSuffix;

        TbSecondPrefix.Text = _axis.SecondTextPrefix;
        TbSecondText.Text = _axis.SecondText;
        TbSecondSuffix.Text = _axis.SecondTextSuffix;

        TbThirdPrefix.Text = _axis.ThirdTextPrefix;
        TbThirdText.Text = _axis.ThirdText;
        TbThirdSuffix.Text = _axis.ThirdTextSuffix;

        TbBottomOrientText.Text = _axis.BottomOrientText;
        TbTopOrientText.Text = _axis.TopOrientText;

        // markers position
        CbMarkersPosition.SelectedItem = _axis.MarkersPosition;

        // focus
        TbFirstText.Focus();
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        // values
        _axis.FirstTextPrefix = TbFirstPrefix.Text;
        _axis.FirstText = TbFirstText.Text;
        _axis.FirstTextSuffix = TbFirstSuffix.Text;

        _axis.SecondTextPrefix = TbSecondPrefix.Text;
        _axis.SecondText = TbSecondText.Text;
        _axis.SecondTextSuffix = TbSecondSuffix.Text;

        _axis.ThirdTextPrefix = TbThirdPrefix.Text;
        _axis.ThirdText = TbThirdText.Text;
        _axis.ThirdTextSuffix = TbThirdSuffix.Text;

        _axis.BottomOrientText = TbBottomOrientText.Text;
        _axis.TopOrientText = TbTopOrientText.Text;

        // markers position
        _axis.MarkersPosition = (AxisMarkersPosition)CbMarkersPosition.SelectedItem;
    }

    #region Visibility

    private void ChangeOrientVisibility()
    {
        if (_axis.MarkersPosition == AxisMarkersPosition.Both || _axis.MarkersPosition == AxisMarkersPosition.Top)
            TbTopOrientText.Visibility = _axis.TopOrientMarkerVisible ? Visibility.Visible : Visibility.Collapsed;
        else
            TbTopOrientText.Visibility = Visibility.Collapsed;

        if (_axis.MarkersPosition == AxisMarkersPosition.Both || _axis.MarkersPosition == AxisMarkersPosition.Bottom)
            TbBottomOrientText.Visibility = _axis.BottomOrientMarkerVisible ? Visibility.Visible : Visibility.Collapsed;
        else
            TbBottomOrientText.Visibility = Visibility.Collapsed;
    }

    private void ChangeSecondVisibility(bool show)
    {
        if (show)
            TbSecondPrefix.Visibility = TbSecondText.Visibility = TbSecondSuffix.Visibility = Visibility.Visible;
        else
            TbSecondPrefix.Visibility = TbSecondText.Visibility = TbSecondSuffix.Visibility = Visibility.Collapsed;
    }

    private void ChangeThirdVisibility(bool show)
    {
        if (show)
            TbThirdPrefix.Visibility = TbThirdText.Visibility = TbThirdSuffix.Visibility = Visibility.Visible;
        else
            TbThirdPrefix.Visibility = TbThirdText.Visibility = TbThirdSuffix.Visibility = Visibility.Collapsed;
    }

    #endregion
}