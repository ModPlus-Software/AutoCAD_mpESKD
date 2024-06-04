namespace mpESKD.Functions.mpChainLeader;

using System;
using Base.Abstractions;
using mpESKD.Base.Utils;

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
        Resources.SetModPlusResources();
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(ChainLeader);
        
    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not ChainLeader chainLeader)
            throw new ArgumentException("Wrong type of entity");

        _chainLeader = chainLeader;
        
        TbNodeNumber.Text = _chainLeader.LeaderTextValue;
        TbNodeAddress.Text = _chainLeader.LeaderTextComment;
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _chainLeader.LeaderTextValue = TbNodeNumber.Text;
        _chainLeader.LeaderTextComment = TbNodeAddress.Text;
    }
}