namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class ChainLeaderAddArrowGrip : SmartEntityGripData
{
    private Point2d[] _points;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderAddArrowGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    public ChainLeaderAddArrowGrip(ChainLeader chainLeader) 
    {
        ChainLeader = chainLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
    }

    /// <summary>
    /// Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

    /// <summary>
    /// Новое значение точки вершины
    /// </summary>
    public double NewPoint { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Добавить выноску
        return Language.GetItem("gp5");
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripStart)
        {
            ChainLeader.TempNewArrowPoint = NewPoint;
            ChainLeader.UpdateEntities();
        }

        if (newStatus == Status.Move)
        {
            ChainLeader.TempNewArrowPoint = NewPoint;
            ChainLeader.UpdateEntities();
            AcadUtils.WriteMessageInDebug($"OnGripStatusChanged if (newStatus == Status.Move) TempNewArrowPoint {ChainLeader.TempNewArrowPoint} \n");
        }

        if (newStatus == Status.GripEnd)
        {
            using (ChainLeader)
            {
                ChainLeader.ArrowPoints.Add(NewPoint);
                ChainLeader.TempNewArrowPoint = double.NaN;
                AcadUtils.WriteMessageInDebug($"NewPoint {NewPoint} -  ChainLeader.TempNewArrowPoint {ChainLeader.TempNewArrowPoint}\n");
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
        }

        if (newStatus == Status.GripAbort)
        {
            ChainLeader.TempNewArrowPoint = double.NaN;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}