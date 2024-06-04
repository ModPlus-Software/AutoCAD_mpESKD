namespace mpESKD.Functions.mpLetterLine;

using System;
using Base.Abstractions;
using mpESKD.Base.Utils;

/// <summary>
/// Interaction logic for LetterLineDoubleClickEditControl.xaml
/// </summary>
public partial class LetterLineDoubleClickEditControl : IDoubleClickEditControl
{
    private LetterLine _letterLine;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="LetterLineDoubleClickEditControl"/> class.
    /// </summary>
    public LetterLineDoubleClickEditControl()
    {
        InitializeComponent();
        Resources.SetModPlusResources();
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(LetterLine);

    /// <inheritdoc />
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not LetterLine letterLine)
            throw new ArgumentException("Wrong type of entity");

        _letterLine = letterLine;
            
        TbDesignation.Text = _letterLine.MainText;
        TbDesignationPrefix.Text = _letterLine.SmallText;
        TbDesignation.Focus();
    }

    /// <inheritdoc />
    public void OnAccept()
    {
        _letterLine.MainText = TbDesignation.Text;
        _letterLine.SmallText = TbDesignationPrefix.Text;
    }
}