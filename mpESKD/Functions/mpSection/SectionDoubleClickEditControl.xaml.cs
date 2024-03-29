namespace mpESKD.Functions.mpSection;

using System;
using Base.Abstractions;
using mpESKD.Base.Utils;

/// <summary>
/// Логика взаимодействия для SectionDoubleClickEditControl.xaml
/// </summary>
public partial class SectionDoubleClickEditControl : IDoubleClickEditControl
{
    private Section _section;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="SectionDoubleClickEditControl"/> class.
    /// </summary>
    public SectionDoubleClickEditControl()
    {
        InitializeComponent();
        Resources.SetModPlusResources();
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(Section);
        
    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not Section section)
            throw new ArgumentException("Wrong type of entity");

        _section = section;
            
        TbDesignation.Text = _section.Designation;
        TbDesignationPrefix.Text = _section.DesignationPrefix;
        TbSheetNumber.Text = _section.SheetNumber;
        TbDesignation.Focus();
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _section.Designation = TbDesignation.Text;
        _section.DesignationPrefix = TbDesignationPrefix.Text;
        _section.SheetNumber = TbSheetNumber.Text;

        if (ChkRestoreTextPosition.IsChecked.HasValue && ChkRestoreTextPosition.IsChecked.Value)
        {
            _section.AlongBottomShelfTextOffset = double.NaN;
            _section.AlongTopShelfTextOffset = double.NaN;
            _section.AcrossBottomShelfTextOffset = double.NaN;
            _section.AcrossTopShelfTextOffset = double.NaN;
        }
    }
}