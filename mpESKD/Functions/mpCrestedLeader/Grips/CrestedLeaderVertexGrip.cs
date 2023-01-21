namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка вершин
/// </summary>
public class CrestedLeaderVertexGrip: SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTmp;

    // Экземпляр анонимного блока
    private readonly BlockReference _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpChainLeader.ChainLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public CrestedLeaderVertexGrip(CrestedLeader crestedLeader, int gripIndex, BlockReference entity)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    public List<Point3d> TempPoint3ds { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            if (newStatus == Status.GripStart)
            {
                if (GripIndex == 0)
                {
                    _gripTmp = CrestedLeader.InsertionPoint;
                }

                if (GripIndex == 1)
                {
                    _gripTmp = CrestedLeader.EndPoint;
                }
            }

            if (newStatus == Status.GripEnd)
            {
                var tempInsPoint = CrestedLeader.InsertionPoint;
                using (CrestedLeader)
                {
                    if (GripIndex == 0)
                    {
                        var mainNormal = (CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal();
                        var result = CrestedLeader.ArrowPoints.OrderBy(x => x).FirstOrDefault();

                        var distFromEndPointToInsPoint = -1 * CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint);

                        //if (result < distFromEndPointToInsPoint)
                        //{
                        //    tempInsPoint = CrestedLeader.EndPoint + (mainNormal * result);
                        //    CrestedLeader.ArrowPoints.Remove(result);
                        //    CrestedLeader.ArrowPoints.Add(distFromEndPointToInsPoint);
                        //}

                        CrestedLeader.InsertionPoint = tempInsPoint;
                    }

                    CrestedLeader.UpdateEntities();
                    CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
                }

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

                CrestedLeader.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_gripTmp != null)
                {
                    if (GripIndex == 0)
                    {
                        CrestedLeader.InsertionPoint = _gripTmp;
                    }

                    if (GripIndex == 1)
                    {
                        CrestedLeader.EndPoint = _gripTmp;
                    }
                }
            }

            base.OnGripStatusChanged(entityId, newStatus);
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }
}