using System.Linq;

namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using CSharpFunctionalExtensions;
using ModPlusAPI;
using ModPlusAPI.Windows;

/// <summary>
/// Ручка переноса выносок
/// </summary>
public class CrestedLeaderGrip : SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public CrestedLeaderGrip(CrestedLeader crestedLeader, int gripIndex)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
    }

    /// <summary>
    /// Новое значение точки ручки
    /// </summary>
    public Point3d NewPoint { get; set; }

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
        return Language.GetItem("gp2"); // move
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        Loggerq.WriteRecord($"OnGripStatusChanged start");

        try
        {
            // При начале перемещения запоминаем первоначальное положение ручки
            // Запоминаем начальные значения
            if (newStatus == Status.GripStart)
            {
                _gripTmp = GripPoint;
            }

            // При удачном перемещении ручки записываем новые значения в расширенные данные
            // По этим данным я потом получаю экземпляр класса
            if (newStatus == Status.GripEnd)
            {
                var offset = NewPoint - _gripTmp;
              //  var tempLeaderEndPoints = CrestedLeader.LeaderEndPoints;
               // var tempShelfStartPoint = CrestedLeader.ShelfStartPoint;
              //  var tempShelfLedgePoint = CrestedLeader.ShelfLedgePoint;

                CrestedLeader.ShelfLedgePointPreviousForGripMove += offset;
                CrestedLeader.ShelfEndPointPreviousForGripMove += offset;

                //CrestedLeader.LeaderPointsPreviousForGripMove = CrestedLeader.LeaderPointsPreviousForGripMove
                //    .Select(x => x + offset)
                //    .ToList();

                //CrestedLeader.InsertionPoint = CrestedLeader.LeaderStartPoints.Last();

                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);

                    // перемещение точки вставки в точку первой точки полки
                    // ((BlockReference)blkRef).Position = CrestedLeader.InsertionPoint;

                    /*
                    Loggerq.WriteRecord($"OnGripStatusChanged: ((BlockReference)blkRef).Position: {((BlockReference)blkRef).Position.ToString()}");
                    Loggerq.WriteRecord($"OnGripStatusChanged: CrestedLeader.InsertionPoint: {CrestedLeader.InsertionPoint.ToString()}");
                    Loggerq.WriteRecord($"OnGripStatusChanged: CrestedLeader.LeaderStartPoints.Last(): {CrestedLeader.LeaderStartPoints.Last().ToString()}");
                    */

                    //CrestedLeader.InsertionPoint = CrestedLeader.LeaderStartPoints.Last();
                    //((BlockReference)blkRef).Position = CrestedLeader.LeaderStartPoints.Last();

                    using (var resBuf = CrestedLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
                /*
                CrestedLeader.LeaderEndPoints = tempLeaderEndPoints;
                CrestedLeader.ShelfStartPoint = tempShelfStartPoint;
                CrestedLeader.ShelfLedgePoint = tempShelfLedgePoint;

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
                */

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
                }
            }

            base.OnGripStatusChanged(entityId, newStatus);
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }

        Loggerq.WriteRecord($"OnGripStatusChanged end");
    }
}