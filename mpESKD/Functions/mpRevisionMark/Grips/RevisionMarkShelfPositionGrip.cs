namespace mpESKD.Functions.mpRevisionMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка марки изменения, меняющая положение полки
/// </summary>
public class RevisionMarkShelfPositionGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkShelfPositionGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр <see cref="mpRevisionMark.RevisionMark"/></param>
    public RevisionMarkShelfPositionGrip(RevisionMark revisionMark)
    {
        RevisionMark = revisionMark;
        GripType = GripType.TwoArrowsLeftRight;
    }

    /// <summary>
    /// Экземпляр <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("p78"); // "Положение полки";
    }

    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (RevisionMark)
        {
            RevisionMark.MarkPosition = RevisionMark.MarkPosition == MarkPosition.Left
                ? MarkPosition.Right
                : MarkPosition.Left;

            RevisionMark.UpdateEntities();
            RevisionMark.BlockRecord.UpdateAnonymousBlocks();
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(RevisionMark.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = RevisionMark.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }

        return ReturnValue.GetNewGripPoints;
    }
}