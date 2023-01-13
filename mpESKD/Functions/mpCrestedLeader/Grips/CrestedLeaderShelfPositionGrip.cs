namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка выноски, меняющая положение полки
/// </summary>
public class CrestedLeaderShelfPositionGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderShelfPositionGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр <see cref="mpChainLeader.ChainLeader"/></param>
    public CrestedLeaderShelfPositionGrip(CrestedLeader crestedLeader)
    {
        CrestedLeader = crestedLeader;
    }

    public CrestedLeader CrestedLeader { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("p78"); // "Положение полки";
    }
        
    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (CrestedLeader)
        {
            CrestedLeader.ShelfPosition = CrestedLeader.ShelfPosition == ShelfPosition.Left
                ? ShelfPosition.Right
                : ShelfPosition.Left;

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
