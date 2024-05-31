namespace mpESKD.Functions.mpCrestedLeader;

using System;
using Base.Abstractions;
using Base.Utils;

/// <summary>
/// Логика взаимодействия для CrestedLeaderDoubleClickEditControl.xaml
/// </summary>
public partial class CrestedLeaderDoubleClickEditControl : IDoubleClickEditControl
{
    private CrestedLeader _crestedLeader;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderDoubleClickEditControl"/> class.
    /// </summary>
    public CrestedLeaderDoubleClickEditControl()
    {
        InitializeComponent();
        Resources.SetModPlusResources();
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(CrestedLeader);

    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not CrestedLeader crestedLeader)
            throw new ArgumentException("Wrong type of entity");

        _crestedLeader = crestedLeader;

        TbTopText.Text = _crestedLeader.TopText;
        TbBottomText.Text = _crestedLeader.BottomText;
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _crestedLeader.TopText = TbTopText.Text;
        _crestedLeader.BottomText = TbBottomText.Text;
    }
}