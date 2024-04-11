namespace mpESKD.Functions.mpRevisionMark;

using System;
using Base.Abstractions;
using Base.Utils;

/// <summary>
/// Логика взаимодействия для RevisionMarkDoubleClickEditControl.xaml
/// </summary>
public partial class RevisionMarkDoubleClickEditControl : IDoubleClickEditControl
{
    private RevisionMark _revisionMark;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkDoubleClickEditControl"/> class.
    /// </summary>
    public RevisionMarkDoubleClickEditControl()
    {
        InitializeComponent();
        Resources.SetModPlusResources();
    }

    /// <inheritdoc/>
    public Type EntityType => typeof(RevisionMark);

    /// <inheritdoc/>
    public void Initialize(IWithDoubleClickEditor smartEntity)
    {
        if (smartEntity is not RevisionMark revisionMark)
            throw new ArgumentException("Wrong type of entity");

        _revisionMark = revisionMark;

        TbRevisionNumber.Text = _revisionMark.RevisionNumber;
        TbNote.Text = _revisionMark.Note;
    }

    /// <inheritdoc/>
    public void OnAccept()
    {
        _revisionMark.RevisionNumber = TbRevisionNumber.Text;
        _revisionMark.Note = TbNote.Text;
    }
}