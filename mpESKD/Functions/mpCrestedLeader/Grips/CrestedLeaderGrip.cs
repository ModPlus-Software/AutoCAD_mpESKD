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
/// Ручка переноса выносок
/// </summary>
public class CrestedLeaderGrip : SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTmp;

    // Временное значение точек
    private readonly List<Point3d> _leaderStartPointsTmp = new ();
    private Point3d _shelfStartPointTmp;

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
        RubberBandLineDisabled = true;
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
        try
        {
            // При начале перемещения запоминаем первоначальное положение ручки
            // Запоминаем начальные значения
            if (newStatus == Status.GripStart)
            {
                _gripTmp = GripPoint;

                _leaderStartPointsTmp.Clear();
                _leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                _shelfStartPointTmp = CrestedLeader.ShelfStartPoint;
            }

            // При удачном перемещении ручки записываем новые значения в расширенные данные
            // По этим данным я потом получаю экземпляр класса
            if (newStatus == Status.GripEnd)
            {
                List<Point3d> leaderStartPointsTmp = new();
                    leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                    var leaderStartPointsSort = CrestedLeader.LeaderStartPointsSorted;
                    
                    if (CrestedLeader.ScaleFactorX == -1)
                    {
                        leaderStartPointsSort.Reverse();
                    }

                    CrestedLeader.InsertionPoint = CrestedLeader.ShelfPosition == ShelfPosition.Right 
                        ? leaderStartPointsSort.Last() 
                        : leaderStartPointsSort.First();

                    var baseLeaderStartPoint = CrestedLeader.LeaderStartPoints.First(p => p.Equals(CrestedLeader.InsertionPoint));
                    var baseIndex = CrestedLeader.LeaderStartPoints.IndexOf(baseLeaderStartPoint);

                    var baseLeaderEndPoint = CrestedLeader.BaseLeaderEndPoint = CrestedLeader.LeaderEndPoints.ElementAt(baseIndex);

                    CrestedLeader.IsBasePointMovedByOverrule = true;

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
                    CrestedLeader.BaseLeaderEndPoint = baseLeaderEndPoint;

                    CrestedLeader.IsBasePointMovedByOverrule = true;

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
                if (_gripTmp != null)
                {
                    if (GripIndex == 0)
                    {
                        CrestedLeader.InsertionPoint = _gripTmp;
                    }
                }

                if (_gripTmp != null && _shelfStartPointTmp != null)
                {
                    CrestedLeader.LeaderStartPoints.Clear();
                    CrestedLeader.LeaderStartPoints.AddRange(_leaderStartPointsTmp);

                    CrestedLeader.ShelfStartPoint = _shelfStartPointTmp;
                }

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