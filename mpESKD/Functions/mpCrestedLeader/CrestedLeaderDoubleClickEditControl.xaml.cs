namespace mpESKD.Functions.mpCrestedLeader;

using System;
using Base.Abstractions;

/// <summary>
/// Логика взаимодействия для CrestedLeaderDoubleClickEditControl.xaml
/// </summary>
public partial class CrestedLeaderDoubleClickEditControl : IDoubleClickEditControl
{
    private CrestedLeader _crestedLeader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderDoubleClickEditControl"/> class.
    /// </summary>
    public CrestedLeaderDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(CrestedLeader);

    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not CrestedLeader chainLeader)
            throw new ArgumentException("Wrong type of entity");

        _crestedLeader = chainLeader;

        TbNodeNumber.Text = _crestedLeader.LeaderTextValue;
        TbNodeAddress.Text = _crestedLeader.LeaderTextComment;
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _crestedLeader.LeaderTextValue = TbNodeNumber.Text;
        _crestedLeader.LeaderTextComment = TbNodeAddress.Text;
    }
}