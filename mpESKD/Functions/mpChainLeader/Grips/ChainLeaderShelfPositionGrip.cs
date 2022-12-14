namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка выноски, меняющая положение полки
/// </summary>
public class ChainLeaderShelfPositionGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderShelfPositionGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр <see cref="mpChainLeader.ChainLeader"/></param>
    public ChainLeaderShelfPositionGrip(ChainLeader chainLeader)
    {
        ChainLeader = chainLeader;
    }

    public ChainLeader ChainLeader { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("p78"); // "Положение полки";
    }
        
    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (ChainLeader)
        {
            ChainLeader.ShelfPosition = ChainLeader.ShelfPosition == ShelfPosition.Left
                ? ShelfPosition.Right
                : ShelfPosition.Left;

            ChainLeader.UpdateEntities();
            ChainLeader.BlockRecord.UpdateAnonymousBlocks();
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(ChainLeader.BlockId, OpenMode.ForWrite, true, true);
                    
                using (var resBuf = ChainLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }

        return ReturnValue.GetNewGripPoints;
    }
}