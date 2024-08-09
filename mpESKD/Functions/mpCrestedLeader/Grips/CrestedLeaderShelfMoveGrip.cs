namespace mpESKD.Functions.mpCrestedLeader.Grips;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;

/// <summary>
/// Ручка перетаскивания полки
/// </summary>
public class CrestedLeaderShelfMoveGrip : SmartEntityGripData
{
    // Временное значение точек
    private Point3d _shelfStartPoint;
    private double _shelfLedge;
    private ShelfPosition _shelfPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrestedLeaderShelfMoveGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public CrestedLeaderShelfMoveGrip(CrestedLeader crestedLeader, int gripIndex)
    {
        CrestedLeader = crestedLeader;
        GripIndex = gripIndex;
        GripType = GripType.Point;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpCrestedLeader.CrestedLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Новое значение точки ручки
    /// </summary>
    public Point3d NewPoint { get; set; }

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
        try
        {
            // При начале перемещения запоминаем первоначальное положение ручки
            // Запоминаем начальные значения
            if (newStatus == Status.GripStart)
            {
                _shelfStartPoint = CrestedLeader.ShelfStartPoint;
                _shelfLedge = CrestedLeader.ShelfLedge;
                _shelfPosition = CrestedLeader.ShelfPosition;
            }

            // При удачном перемещении ручки записываем новые значения в расширенные данные
            // По этим данным я потом получаю экземпляр класса
            if (newStatus == Status.GripEnd)
            {
                if (CrestedLeader.IsChangeShelfPosition)
                {
                    var leaderStartPointsSort = CrestedLeader.LeaderStartPointsSorted;

                    List<Point3d> leaderStartPointsTmp = new();
                    leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                    List<Point3d> leaderEndPointsTmp = new();
                    leaderEndPointsTmp.AddRange(CrestedLeader.LeaderEndPoints);

                    if (CrestedLeader.ShelfPosition == ShelfPosition.Right)
                    {
                        CrestedLeader.InsertionPoint = leaderStartPointsSort.Last();
                    }
                    else
                    {
                        CrestedLeader.InsertionPoint = leaderStartPointsSort.First();
                    }

                    var baseLeaderStartPoint = CrestedLeader.LeaderStartPoints.First(p => p.Equals(CrestedLeader.InsertionPoint));
                    var baseIndex = CrestedLeader.LeaderStartPoints.IndexOf(baseLeaderStartPoint);
                    var baseLeaderEndPoint = CrestedLeader.BaseLeaderEndPoint = CrestedLeader.LeaderEndPoints.ElementAt(baseIndex);

                    CrestedLeader.IsStartPointsAssigned = true;

                    CrestedLeader.UpdateEntities();
                    CrestedLeader.BlockRecord.UpdateAnonymousBlocks();

                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);

                        // перемещение точки вставки в точку первой точки полки
                        ((BlockReference)blkRef).Position = CrestedLeader.InsertionPoint;

                        using (var resBuf = CrestedLeader.GetDataForXData())
                        {
                            blkRef.XData = resBuf;
                        }

                        tr.Commit();
                    }

                    CrestedLeader.LeaderStartPoints.Clear();
                    CrestedLeader.LeaderStartPoints.AddRange(leaderStartPointsTmp);

                    CrestedLeader.LeaderEndPoints.Clear();
                    CrestedLeader.LeaderEndPoints.AddRange(leaderEndPointsTmp);

                    CrestedLeader.BaseLeaderEndPoint = baseLeaderEndPoint;
                    
                    CrestedLeader.ShelfStartPoint = CrestedLeader.InsertionPoint;

                    CrestedLeader.ShelfLedge = NewPoint.GetProjectPointToBaseLine(CrestedLeader)
                        .DistanceTo(CrestedLeader.InsertionPoint);

                    CrestedLeader.IsStartPointsAssigned = true;
                }

                CrestedLeader.IsChangeShelfPosition = false;

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

                CrestedLeader.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_shelfStartPoint != null)
                {
                    CrestedLeader.ShelfStartPoint = _shelfStartPoint;
                }

                CrestedLeader.ShelfLedge = _shelfLedge;
                CrestedLeader.ShelfPosition = _shelfPosition;

                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
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