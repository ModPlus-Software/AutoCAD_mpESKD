namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using System.Linq;

/// <summary>
/// Ручка перетаскивания полки
/// </summary>
public class CrestedLeaderShelfMoveGrip : SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTmp;

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
        //RubberBandLineDisabled = true;
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
        Loggerq.WriteRecord("CrestedLeaderShelfMoveGrip: OnGripStatusChanged() => START");
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
                // CrestedLeader.EndPoint = NewPoint;

                // новое значение ShelfPosition(? , ShelfStartPoint, ShelfLedgePoint, ShelfEndPoint

                /*
                var leaderStartPointsSort = CrestedLeader.LeaderStartPoints.OrderBy(p => p.X);
                var leftStartPoint = leaderStartPointsSort.First();
                var rightStartPoint = leaderStartPointsSort.Last();
                var middleStartPoint = GeometryUtils
                    .GetMiddlePoint3d(leaderStartPointsSort.First(), leaderStartPointsSort.Last());

                // Полка справа, курсор вправо
                if (CrestedLeader.ShelfPosition == ShelfPosition.Right && NewPoint.X > rightStartPoint.X)
                {
                    CrestedLeader.ShelfLedge = NewPoint.X - CrestedLeader.ShelfStartPoint.X;
                }
                // Полка справа курсор влево
                else
                {
                }

                */

                // Если меняется положение полки - ShelfPoeition - переносится точка вставки

                Loggerq.WriteRecord($"CrestedLeaderGripPointOverrule: MoveGripPointsAt() => " +
                                    $"IsChangeShelfPosition: {CrestedLeader.IsChangeShelfPosition}");

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