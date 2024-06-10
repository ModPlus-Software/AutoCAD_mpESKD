namespace mpESKD.Functions.mpNodeLabel;

using System;
using Base.Abstractions;

/// <summary>
/// Логика взаимодействия для NodeLabelDoubleClickEditControl.xaml
/// </summary>
public partial class NodeLabelDoubleClickEditControl : IDoubleClickEditControl
{
    private NodeLabel _nodeLabel;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeLabelDoubleClickEditControl"/> class.
    /// </summary>
    public NodeLabelDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(NodeLabel);

    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (!(smartEntity is NodeLabel nodeLabel))
            throw new ArgumentException("Wrong type of entity");

        _nodeLabel = nodeLabel;

        TbNodeNumber.Text = _nodeLabel.NodeNumber;
        TbSheetNumber.Text = _nodeLabel.SheetNumber;
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _nodeLabel.NodeNumber = TbNodeNumber.Text;
        _nodeLabel.SheetNumber = TbSheetNumber.Text;
    }
}