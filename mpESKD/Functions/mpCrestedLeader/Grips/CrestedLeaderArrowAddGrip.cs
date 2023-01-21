using System;
using Autodesk.AutoCAD.Geometry;

namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class CrestedLeaderArrowAddGrip : SmartEntityGripData
{
    private readonly BlockReference _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderArrowAddGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public CrestedLeaderArrowAddGrip(CrestedLeader crestedLeader, BlockReference entity)
    {
        CrestedLeader = crestedLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        // Добавить выноску
        return Language.GetItem("gp5");
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                if (!CrestedLeader.ArrowPoints.Contains(CrestedLeader.TempNewArrowPoint))
                {
                   CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                }

                CrestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);

                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);
                    //_entity.Position = tempInsPoint;
                    using (var resBuf = CrestedLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}
