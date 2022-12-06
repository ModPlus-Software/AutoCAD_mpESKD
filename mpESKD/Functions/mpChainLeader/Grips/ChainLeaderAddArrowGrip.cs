namespace mpESKD.Functions.mpChainLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка выбора типа рамки, меняющая тип рамки
/// </summary>
public class ChainLeaderAddArrowGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderAddArrowGrip"/> class.
    /// </summary>
    /// <param name="chainLeader">Экземпляр <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    public ChainLeaderAddArrowGrip(ChainLeader chainLeader, BlockReference entity)
    {
        ChainLeader = chainLeader;
        GripType = GripType.Plus;
        RubberBandLineDisabled = true;
        Entity = entity;
    }

    public BlockReference Entity { get; }
    /// <summary>
    /// Экземпляр <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public ChainLeader ChainLeader { get; }

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
            using (ChainLeader)
            {
                var tempInsPoint = new Point3d();
                if (!ChainLeader.ArrowPoints.Contains(ChainLeader.TempNewArrowPoint))
                {
                    var mainNormal = (ChainLeader.EndPoint - ChainLeader.InsertionPoint).GetNormal();
                    var distFromEndPointToInsPoint = ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                    if (ChainLeader.IsLeft)
                    {
                        distFromEndPointToInsPoint = -1 * ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint);
                    }

                    double result;
                    var tempList = new List<double>();
                    tempList.Add(distFromEndPointToInsPoint);
                    tempList.AddRange(ChainLeader.ArrowPoints);
                    //когда тянем вправо
                    if (ChainLeader.TempNewArrowPoint > 0)
                    {
                        result = tempList.OrderBy(x => x).FirstOrDefault();
                        // если в списке есть значения и они положительные, то берем последнюю
                        if (result > 0)
                        {
                            result = tempList.OrderBy(x => x).LastOrDefault();
                            // если последняя больше чем текущая
                            if (result > ChainLeader.TempNewArrowPoint)
                            {
                                // текущую добавлеям в список, inspoint не меняем
                                ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
                                //ChainLeader.ArrowPoints.Remove(result);
                                tempInsPoint = ChainLeader.InsertionPoint;
                            }
                            else
                            { // если текущая больше чем последняя она должна быть insPoint
                                tempInsPoint = ChainLeader.EndPoint + (mainNormal * ChainLeader.TempNewArrowPoint);

                                ChainLeader.ArrowPoints.Add(distFromEndPointToInsPoint);
                            }
                        }
                        else
                        {
                            ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
                            tempInsPoint = ChainLeader.InsertionPoint;
                        }
                    }
                    else // когда тянем влево, значения отрицательные
                    {
                        // ищем первую
                        result = tempList.OrderBy(x => x).FirstOrDefault();

                        //если первая положительная, значит слева нет точек
                        if (result > 0)
                        {
                            // тогда 

                            tempInsPoint = ChainLeader.InsertionPoint;
                            result = ChainLeader.ArrowPoints.OrderBy(x => x).LastOrDefault();
                            ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
                        }
                        else if (result > ChainLeader.TempNewArrowPoint)
                        {
                            tempInsPoint = ChainLeader.EndPoint + (mainNormal * ChainLeader.TempNewArrowPoint);
                            ChainLeader.ArrowPoints.Add(-1 * ChainLeader.EndPoint.DistanceTo(ChainLeader.InsertionPoint));
                        }
                        else
                        {
                            tempInsPoint = ChainLeader.InsertionPoint;
                            ChainLeader.ArrowPoints.Add(ChainLeader.TempNewArrowPoint);
                        }
                    }
                }
                else
                {
                    tempInsPoint = ChainLeader.InsertionPoint;
                }

                ChainLeader.InsertionPoint = tempInsPoint;
                ChainLeader.TempNewArrowPoint = double.NaN;
                
                ChainLeader.UpdateEntities();
                ChainLeader.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(ChainLeader.BlockId, OpenMode.ForWrite, true, true);
                    Entity.Position = tempInsPoint;
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