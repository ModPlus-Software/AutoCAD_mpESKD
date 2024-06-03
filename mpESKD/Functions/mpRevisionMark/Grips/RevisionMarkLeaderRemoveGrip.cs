namespace mpESKD.Functions.mpRevisionMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка вершин
/// </summary>
public class RevisionMarkLeaderRemoveGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkLeaderRemoveGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public RevisionMarkLeaderRemoveGrip(RevisionMark revisionMark, int gripIndex)
    {
        RevisionMark = revisionMark;
        GripIndex = gripIndex;
        GripType = GripType.Minus;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp6"); // Удалить выноску
    }

    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (RevisionMark)
        {
            RevisionMark.LeaderPoints.RemoveAt(GripIndex);
            RevisionMark.RevisionFrameTypes.RemoveAt(GripIndex);
            RevisionMark.RevisionFrameStretchPoints.RemoveAt(GripIndex);
            //RevisionMark.RemoveFrameEntities(GripIndex);

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