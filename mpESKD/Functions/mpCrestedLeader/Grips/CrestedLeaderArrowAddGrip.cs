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
public class CrestedLeaderArrowAddGrip: SmartEntityGripData
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

    /// <summary>
    /// Свойство для определения точки в существующем сегменте
    /// </summary>
    public bool IsOnsegment { get; set; }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripEnd)
        {
            using (CrestedLeader)
            {
                var tempInsPoint = CrestedLeader.InsertionPoint;
                if (!CrestedLeader.ArrowPoints.Contains(CrestedLeader.TempNewArrowPoint))
                {
                    var distFromEndPointToInsPoint = CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint);
                    if (!CrestedLeader.IsLeft)
                    {
                        distFromEndPointToInsPoint = -1 * CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint);
                    }

                    var tempList = new List<double>
                    {
                        distFromEndPointToInsPoint
                    };
                    tempList.AddRange(CrestedLeader.ArrowPoints);
                    var result = tempList.OrderBy(x => x).FirstOrDefault();

                    // когда тянем вправо
                    if (CrestedLeader.TempNewArrowPoint > 0)
                    {
                        // если в списке есть значения и они положительные, то берем последнюю
                        if (result > 0)
                        {
                            result = tempList.OrderBy(x => x).LastOrDefault();

                            // если последняя больше чем текущая
                            if (result > CrestedLeader.TempNewArrowPoint)
                            {
                                // текущую добавляем в список, inspoint не меняем
                                CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                            }
                            else
                            { // если текущая больше чем последняя она должна быть insPoint
                                tempInsPoint = CrestedLeader.EndPoint + ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * CrestedLeader.TempNewArrowPoint);
                                CrestedLeader.ArrowPoints.Add(distFromEndPointToInsPoint);
                            }
                        }
                        else
                        {
                            CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                        }
                    }

                    // когда тянем влево, значения отрицательные
                    else
                    {
                        if (result > CrestedLeader.TempNewArrowPoint && !IsOnsegment)
                        {
                            tempInsPoint = CrestedLeader.EndPoint + ((CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal() * CrestedLeader.TempNewArrowPoint);
                            CrestedLeader.ArrowPoints.Add(-1 * CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint));
                        }
                        else
                        {
                            CrestedLeader.ArrowPoints.Add(CrestedLeader.TempNewArrowPoint);
                        }
                    }
                }

                CrestedLeader.InsertionPoint = tempInsPoint;
                CrestedLeader.TempNewArrowPoint = double.NaN;

                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);
                    _entity.Position = tempInsPoint;
                    using (var resBuf = CrestedLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }
        }

        if (newStatus == Status.GripAbort)
        {
            CrestedLeader.TempNewArrowPoint = double.NaN;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}
