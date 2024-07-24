namespace mpESKD.Functions.mpCrestedLeader.Grips;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using CSharpFunctionalExtensions;
using ModPlusAPI;
using ModPlusAPI.Windows;


//public delegate void OnGripStatusChangedDelegate(ObjectId entityId, GripData.Status newStatus);

/// <summary>
/// Ручка переноса выносок
/// </summary>
public class CrestedLeaderGrip : SmartEntityGripData
{
    //public event OnGripStatusChangedDelegate OnGripStatusChangedEvent;

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

        Loggerq.WriteRecord($"CrestedLeaderGrip: CrestedLeaderGrip() => crestedLeader.IsChangeShelfPosition: {crestedLeader.IsChangeShelfPosition.ToString()}");

        if (crestedLeader.IsChangeShelfPosition)
        {
            //crestedLeader.ShelfPosChangeEvent += this.OnGripStatusChanged;
            //this.OnGripStatusChanged(crestedLeader.BlockId, Status.Move);
        }
        
    }

    public void OnGripStatusChangedInvoke()
    {
        //OnGripStatusChangedEvent?.Invoke(CrestedLeader.BlockId, Status.GripEnd);
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
        Loggerq.WriteRecord("CrestedLeaderGrip: OnGripStatusChanged() => *\n**");
        Loggerq.WriteRecord("CrestedLeaderGrip: OnGripStatusChanged() => START");
        Loggerq.WriteRecord($"CrestedLeaderGrip: OnGripStatusChanged() =>        Status grip: {newStatus.ToString()}");

        try
        {
            //if (newStatus == Status.Move)
            //{
            //    Loggerq.WriteRecord("CrestedLeaderGrip: OnGripStatusChanged() => Status.Move");
                
            //}

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
                List<Point3d> leaderStartPointsTmp = new ();
                leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                var leaderStartPointsSort = CrestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();

                if (CrestedLeader.ShelfPosition == ShelfPosition.Right)
                    CrestedLeader.InsertionPoint = leaderStartPointsSort.Last();
                else
                    CrestedLeader.InsertionPoint = leaderStartPointsSort.First();

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



                //CrestedLeader.BoundStartPoint = 
                CrestedLeader.IsBasePointMovedByGrip = true;



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
            }

            base.OnGripStatusChanged(entityId, newStatus);
        }
        catch (Exception exception)
        {
            // todo
            //if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
            //    ExceptionBox.Show(exception);
            Loggerq.WriteRecord("CrestedLeaderGrip: OnGripStatusChanged() => ERROR");
        }
    }

    public  void OnGripStatusChangedMy()
    {
        Loggerq.WriteRecord("CrestedLeaderGrip: OnGripStatusChangedMy() => START");

        try
        {
           
                List<Point3d> leaderStartPointsTmp = new();
                leaderStartPointsTmp.AddRange(CrestedLeader.LeaderStartPoints);

                var leaderStartPointsSort = CrestedLeader.LeaderStartPoints.OrderBy(p => p.X).ToList();

                if (CrestedLeader.ShelfPosition == ShelfPosition.Right)
                    CrestedLeader.InsertionPoint = leaderStartPointsSort.Last();
                else
                    CrestedLeader.InsertionPoint = leaderStartPointsSort.First();

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

                CrestedLeader.IsBasePointMovedByGrip = true;

                //CrestedLeader.CrestedLeaderGrip = this;
                //CrestedLeader.ObjectIdForGrip = entityId;
                //CrestedLeader.GripDataStatus = newStatus;

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
        catch (Exception exception)
        {
            // todo
            //if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
            //    ExceptionBox.Show(exception);
            Loggerq.WriteRecord("CrestedLeaderGrip: OnGripStatusChangedMy() => ERROR");
        }
    }

}