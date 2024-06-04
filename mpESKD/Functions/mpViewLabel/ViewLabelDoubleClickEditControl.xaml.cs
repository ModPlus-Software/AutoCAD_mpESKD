namespace mpESKD.Functions.mpViewLabel;

using System;
using Base.Abstractions;
using mpESKD.Base.Utils;

/// <summary>
/// Логика взаимодействия для ViewDoubleClickEditControl.xaml
/// </summary>
public partial class ViewLabelDoubleClickEditControl : IDoubleClickEditControl
{
    private ViewLabel _viewLabel;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewLabelDoubleClickEditControl"/> class.
    /// </summary>
    public ViewLabelDoubleClickEditControl()
    {
        InitializeComponent();
        Resources.SetModPlusResources();
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(ViewLabel);
        
    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not ViewLabel viewLabel)
            throw new ArgumentException("Wrong type of entity");

        _viewLabel = viewLabel;
            
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
    }
}