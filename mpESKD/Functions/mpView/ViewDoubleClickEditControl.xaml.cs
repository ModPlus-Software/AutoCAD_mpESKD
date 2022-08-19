namespace mpESKD.Functions.mpView;

using System;
using Base.Abstractions;

/// <summary>
/// Логика взаимодействия для ViewDoubleClickEditControl.xaml
/// </summary>
public partial class ViewDoubleClickEditControl : IDoubleClickEditControl
{
    private View _view;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewDoubleClickEditControl"/> class.
    /// </summary>
    public ViewDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(View);
        
    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not View view)
            throw new ArgumentException("Wrong type of entity");

        _view = view;
            
        TbDesignation.Text = _view.Designation;
        TbDesignationPrefix.Text = _view.DesignationPrefix;
        TbSheetNumber.Text = _view.SheetNumber;
        TbDesignation.Focus();
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _view.Designation = TbDesignation.Text;
        _view.DesignationPrefix = TbDesignationPrefix.Text;
        _view.SheetNumber = TbSheetNumber.Text;

        if (ChkRestoreTextPosition.IsChecked.HasValue && ChkRestoreTextPosition.IsChecked.Value)
        {
            _view.AlongTopShelfTextOffset = double.NaN;
            _view.AcrossTopShelfTextOffset = double.NaN;
        }
    }
}