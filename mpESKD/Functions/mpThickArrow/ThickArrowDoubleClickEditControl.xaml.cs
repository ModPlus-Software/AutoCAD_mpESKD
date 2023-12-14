namespace mpESKD.Functions.mpThickArrow;

using Base.Abstractions;
using System;

/// <summary>
/// Interaction logic for ThickArrowDoubleClickEditControl.xaml
/// </summary>
public partial class ThickArrowDoubleClickEditControl : IDoubleClickEditControl
{
    private ThickArrow _thickArrow;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThickArrowDoubleClickEditControl"/> class.
    /// </summary>
    public ThickArrowDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(ThickArrow);

    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not ThickArrow thickArrow)
            throw new ArgumentException("Wrong type of entity");

        _thickArrow = thickArrow;

        CbArrowCount.SelectedItem = _thickArrow.ArrowCount;
        ShelfWidthNumBox.Value = _thickArrow.LineWidth;
        ArrowLengthNumBox.Value = _thickArrow.ArrowLength;
        ArrowWidthNumBox.Value = _thickArrow.ArrowWidth;
        CbArrowCount.Focus();
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _thickArrow.ArrowCount = (ThickArrowCount)CbArrowCount.SelectedItem;

        _thickArrow.LineWidth = ShelfWidthNumBox.Value!.Value < _thickArrow.ArrowWidth ? 
            ShelfWidthNumBox.Value!.Value : _thickArrow.ArrowWidth;

        _thickArrow.ArrowLength = ArrowLengthNumBox.Value!.Value;

        _thickArrow.ArrowWidth = ArrowWidthNumBox.Value!.Value > _thickArrow.LineWidth ? 
            ArrowWidthNumBox.Value!.Value : _thickArrow.LineWidth;
    }
}