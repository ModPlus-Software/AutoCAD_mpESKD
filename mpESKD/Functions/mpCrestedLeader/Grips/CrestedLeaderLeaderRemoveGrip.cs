namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка вершин
/// </summary>
public class CrestedLeaderLeaderRemoveGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderLeaderRemoveGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public CrestedLeaderLeaderRemoveGrip(CrestedLeader crestedLeader, int gripIndex)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Minus;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

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
        using (CrestedLeader)
        {
            CrestedLeader.LeaderStartPoints.RemoveAt(GripIndex);
            CrestedLeader.LeaderEndPoints.RemoveAt(GripIndex);

            CrestedLeader.UpdateEntities();
            CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);

                using (var resBuf = CrestedLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }

        return ReturnValue.GetNewGripPoints;
    }
}