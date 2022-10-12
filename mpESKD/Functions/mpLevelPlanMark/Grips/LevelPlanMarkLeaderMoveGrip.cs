namespace mpESKD.Functions.mpLevelPlanMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;

/// <summary>
/// Ручка вершин
/// </summary>
public class LevelPlanMarkLeaderMoveGrip : SmartEntityGripData
{
    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public LevelPlanMark LevelPlanMark { get; }

    /// <summary>
    /// Новое значение точки вершины
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

    // Временное значение ручки
    private Point3d _gripTmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMarkVertexGrip"/> class.
    /// </summary>
    /// <param name="levelPlanMark">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public LevelPlanMarkLeaderMoveGrip(LevelPlanMark levelPlanMark, int gripIndex)
    {
        LevelPlanMark = levelPlanMark;
        GripIndex = gripIndex;
        GripType = GripType.Point;
    }

    ///// <inheritdoc />
    //public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    //{
    //    try
    //    {
    //        // При начале перемещения запоминаем первоначальное положение ручки
    //        // Запоминаем начальные значения
    //        if (newStatus == Status.GripStart)
    //        {
    //            _gripTmp = GripPoint;
    //        }

    //        // При удачном перемещении ручки записываем новые значения в расширенные данные
    //        // По этим данным я потом получаю экземпляр класса
    //        if (newStatus == Status.GripEnd)
    //        {
    //            using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
    //            {
    //                var blkRef = tr.GetObject(LevelPlanMark.BlockId, OpenMode.ForWrite, true, true);
    //                using (var resBuf = LevelPlanMark.GetDataForXData())
    //                {
    //                    blkRef.XData = resBuf;
    //                }

    //                tr.Commit();
    //            }

    //            LevelPlanMark.Dispose();
    //        }

    //        // При отмене перемещения возвращаем временные значения
    //        if (newStatus == Status.GripAbort)
    //        {
    //            if (_gripTmp != null)
    //            {
    //                if (GripIndex == 0)
    //                {
    //                    LevelPlanMark.InsertionPoint = _gripTmp;
    //                }
    //            }
    //        }

    //        base.OnGripStatusChanged(entityId, newStatus);
    //    }
    //    catch (Exception exception)
    //    {
    //        if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
    //            ExceptionBox.Show(exception);
    //    }
    //}

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripStart)
        {
            AcadUtils.Editor.TurnForcedPickOn();
            AcadUtils.WriteMessageInDebug($"plus {Status.GripStart} - {LevelPlanMark.InsertionPointOCS}");
            //AcadUtils.Editor.PointMonitor += AddNewVertex_EdOnPointMonitor;
            
        }

        var leaderPointsCount = LevelPlanMark.LeaderPoints.Count;
        if (newStatus == Status.GripEnd)
        {
            int i= 0;
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.WriteMessageInDebug($"plus {Status.GripEnd} - {LevelPlanMark.InsertionPoint}" );
            //AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
            using (LevelPlanMark)
            {

                //if (leaderPointsCount == 0)
                //{
                //    leaderPointsCount = -1;
                //}

                Point3d? newInsertionPoint = new Point3d(5,5,0);

                LevelPlanMark.LeaderPoints[GripIndex-1] = NewPoint;
                
                foreach (var leaderPoint in LevelPlanMark.LeaderPoints)
                {
                    AcadUtils.WriteMessageInDebug($"leaderpoints {i}-{leaderPoint} ");
                    i++;
                }

                //AcadUtils.WriteMessageInDebug($"{}");

                LevelPlanMark.UpdateEntities();
                LevelPlanMark.BlockRecord.UpdateAnonymousBlocks();
                //using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                //{
                //    var blkRef = tr.GetObject(LevelPlanMark.BlockId, OpenMode.ForWrite, true, true);
                //    if (newInsertionPoint.HasValue)
                //    {
                //        ((BlockReference)blkRef).Position = newInsertionPoint.Value;
                //    }

                //    using (var resBuf = LevelPlanMark.GetDataForXData())
                //    {
                //        blkRef.XData = resBuf;
                //    }

                //    tr.Commit();
                //}
            }
        }

        if (newStatus == Status.GripAbort)
        {
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.WriteMessageInDebug($"plus {Status.GripAbort}");
            
            //AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }
}