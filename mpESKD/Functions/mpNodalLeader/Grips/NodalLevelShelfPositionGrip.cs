namespace mpESKD.Functions.mpNodalLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка узловой выноски, меняющая положение полки
/// </summary>
public class NodalLevelShelfPositionGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodalLevelShelfPositionGrip"/> class.
    /// </summary>
    /// <param name="nodalLeader">Экземпляр <see cref="mpNodalLeader.NodalLeader"/></param>
    public NodalLevelShelfPositionGrip(NodalLeader nodalLeader)
    {
        NodalLeader = nodalLeader;
        GripType = GripType.TwoArrowsLeftRight;
    }
        
    /// <summary>
    /// Экземпляр <see cref="mpNodalLeader.NodalLeader"/>
    /// </summary>
    public NodalLeader NodalLeader { get; }
        
    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("p78"); // "Положение полки";
    }
        
    /// <inheritdoc />
    public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
    {
        using (NodalLeader)
        {
            NodalLeader.ShelfPosition = NodalLeader.ShelfPosition == ShelfPosition.Left
                ? ShelfPosition.Right
                : ShelfPosition.Left;

            NodalLeader.UpdateEntities();
            NodalLeader.BlockRecord.UpdateAnonymousBlocks();
            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blkRef = tr.GetObject(NodalLeader.BlockId, OpenMode.ForWrite, true, true);
                    
                using (var resBuf = NodalLeader.GetDataForXData())
                {
                    blkRef.XData = resBuf;
                }

                tr.Commit();
            }
        }

        return ReturnValue.GetNewGripPoints;
    }
}