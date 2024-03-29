namespace mpESKD.Functions.mpSecantNodalLeader;

using System;
using Base.Abstractions;
using mpESKD.Base.Utils;

/// <summary>
/// Логика взаимодействия для SecantNodalLeaderDoubleClickEditControl.xaml
/// </summary>
public partial class SecantNodalLeaderDoubleClickEditControl : IDoubleClickEditControl
{
    private SecantNodalLeader _secantNodalLeader;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="SecantNodalLeaderDoubleClickEditControl"/> class.
    /// </summary>
    public SecantNodalLeaderDoubleClickEditControl()
    {
        InitializeComponent();
        Resources.SetModPlusResources();
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(SecantNodalLeader);

    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not SecantNodalLeader secantNodalLeader)
            throw new ArgumentException("Wrong type of entity");

        _secantNodalLeader = secantNodalLeader;

        TbNodeNumber.Text = _secantNodalLeader.NodeNumber;
        TbNodeAddress.Text = _secantNodalLeader.NodeAddress;
        TbSheetNumber.Text = _secantNodalLeader.SheetNumber;
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _secantNodalLeader.NodeNumber = TbNodeNumber.Text;
        _secantNodalLeader.NodeAddress = TbNodeAddress.Text;
        _secantNodalLeader.SheetNumber = TbSheetNumber.Text;
    }
}