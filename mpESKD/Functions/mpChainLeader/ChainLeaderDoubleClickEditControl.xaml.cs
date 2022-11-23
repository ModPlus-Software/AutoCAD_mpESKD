namespace mpESKD.Functions.mpChainLeader;

using System;
using Base.Abstractions;

/// <summary>
/// Логика взаимодействия для ChainLeaderDoubleClickEditControl.xaml
/// </summary>
public partial class ChainLeaderDoubleClickEditControl : IDoubleClickEditControl
{
    private ChainLeader _chainLeader;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderDoubleClickEditControl"/> class.
    /// </summary>
    public ChainLeaderDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(ChainLeader);
        
    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not ChainLeader chainLeader)
            throw new ArgumentException("Wrong type of entity");

        _chainLeader = chainLeader;
        
        TbNodeNumber.Text = _chainLeader.NodeNumber;
        TbNodeAddress.Text = _chainLeader.NodeAddress;
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _chainLeader.NodeNumber = TbNodeNumber.Text;
        _chainLeader.NodeAddress = TbNodeAddress.Text;
    }
}