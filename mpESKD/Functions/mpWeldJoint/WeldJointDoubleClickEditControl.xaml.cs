namespace mpESKD.Functions.mpWeldJoint;

using System;
using Base.Abstractions;

/// <summary>
/// Логика взаимодействия для WeldJointDoubleClickEditControl.xaml
/// </summary>
public partial class WeldJointDoubleClickEditControl : IDoubleClickEditControl
{
    private WeldJoint _weldJoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeldJointDoubleClickEditControl"/> class.
    /// </summary>
    public WeldJointDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc />
    public Type EntityType => typeof(WeldJoint);

    /// <inheritdoc />
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not WeldJoint weldJoint)
            throw new ArgumentException("Wrong type of entity");

        _weldJoint = weldJoint;
        CbWeldJointType.SelectedIndex = (int)weldJoint.WeldJointType;
    }

    /// <inheritdoc />
    public void OnAccept()
    {
        _weldJoint.WeldJointType = (WeldJointType)CbWeldJointType.SelectedIndex;
    }
}