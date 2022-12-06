namespace mpESKD.Functions.mpChainLeader.Grips;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;

public class ChainLeaderArrowBaseGrip : SmartEntityGripData
{
    public ChainLeaderArrowBaseGrip(ChainLeader chainLeader, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
        Entity = entity;
    }

    public BlockReference Entity { get; }
    /// <summary>
    /// Экземпляр <see cref="mpChainLeader.ChainLeaderArrowBaseGrip"/>
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

    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        //if (newStatus == Status.GripStart)
        //{
        //    ChainLeader.TempNewArrowPoint = NewPoint;
        //    ChainLeader.UpdateEntities();
        //}

        //if (newStatus == Status.Move)
        //{
        //    ChainLeader.TempNewArrowPoint = NewPoint;
        //    ChainLeader.UpdateEntities();
        //    AcadUtils.WriteMessageInDebug($"OnGripStatusChanged if (newStatus == Status.Move) TempNewArrowPoint {ChainLeader.TempNewArrowPoint} \n");
        //}

        if (newStatus == Status.GripEnd)
        {
            using (ChainLeader)
            {
            }
        }

        if (newStatus == Status.GripAbort)
        {
            ChainLeader.TempNewArrowPoint = double.NaN;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}