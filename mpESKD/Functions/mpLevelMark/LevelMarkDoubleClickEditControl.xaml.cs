using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpLevelMark;

using System;
using Base.Abstractions;

/// <summary>
/// Логика взаимодействия для LevelMarkDoubleClickEditControl.xaml
/// </summary>
public partial class LevelMarkDoubleClickEditControl : IDoubleClickEditControl
{
    private LevelMark _levelMark;

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelMarkDoubleClickEditControl"/> class.
    /// </summary>
    public LevelMarkDoubleClickEditControl()
    {
        InitializeComponent();
        ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(LevelMark);

    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (!(smartEntity is LevelMark levelMark))
            throw new ArgumentException("Wrong type of entity");

        _levelMark = levelMark;

        TbOverrideValue.Text = _levelMark.OverrideValue;
        TbNote.Text = _levelMark.Note;
    }

    /// <inheritdoc/>a
    public void OnAccept()
    {
        _levelMark.OverrideValue = TbOverrideValue.Text;
        _levelMark.Note = TbNote.Text;

        if (LevelNumBox.Value == null)
            return;
        var levelValue = _levelMark.InsertionPoint.Y + _levelMark.InsertionPointOCS.Y + LevelNumBox.Value;
        _levelMark.ObjectPoint = new Point3d(_levelMark.ObjectPoint.X, (double)levelValue, _levelMark.ObjectPoint.Z);
        _levelMark.BottomShelfStartPoint = new Point3d(_levelMark.BottomShelfStartPoint.X, (double)levelValue, _levelMark.BottomShelfStartPoint.Z);
        _levelMark.EndPoint = new Point3d(_levelMark.EndPoint.X, (double)levelValue, _levelMark.EndPoint.Z);
        _levelMark.ShelfPoint = new Point3d(_levelMark.ShelfPoint.X, (double)levelValue + _levelMark.DistanceBetweenShelfs, _levelMark.ShelfPoint.Z);
    }
}