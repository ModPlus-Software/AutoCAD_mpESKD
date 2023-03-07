namespace mpESKD.Functions.mpCrestedLeader.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using mpESKD.Functions.mpChainLeader.Grips;

/// <summary>
/// Ручка растягивания
/// </summary>
public class CrestedLeaderStretchGrip : SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTmpIns;
    private Point3d _gripTmpEnd;

    // Экземпляр анонимного блока
    private readonly BlockReference _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainLeaderVertexGrip"/> class.
    /// </summary>
    /// <param name="crestedLeader">Экземпляр класса <see cref="mpChainLeader.ChainLeader"/></param>
    /// <param name="gripName">Имя ручки</param>
    /// <param name="entity">Экземпляр анонимного блока/></param>
    public CrestedLeaderStretchGrip(CrestedLeader crestedLeader, GripName gripName, BlockReference entity)
    {
        CrestedLeader = crestedLeader;
        GripName = gripName;
        GripType = GripType.Stretch;
        _entity = entity;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpChainLeader.ChainLeader"/>
    /// </summary>
    public CrestedLeader CrestedLeader { get; }

    /// <summary>
    /// Имя ручки
    /// </summary>
    public GripName GripName { get; }

    public double NewPoint { get; set; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, GripData.Status newStatus)
    {
        try
        {
            if (newStatus == GripData.Status.GripStart)
            {
                _gripTmpIns = CrestedLeader.InsertionPoint;
                _gripTmpEnd = CrestedLeader.EndPoint;
            }

            if (newStatus == GripData.Status.GripEnd)
            {
                using (CrestedLeader)
                {
                    var mainNormal = (CrestedLeader.EndPoint - CrestedLeader.InsertionPoint).GetNormal();
                    var tmpEndPoint = new Point3d(CrestedLeader.EndPoint.X, CrestedLeader.InsertionPoint.Y, 0);
                    var distFromEndPointToInsPoint = CrestedLeader.EndPoint.DistanceTo(CrestedLeader.InsertionPoint);

                    var tempMainLine = new Line(
                        new Point3d(CrestedLeader.InsertionPoint.X, CrestedLeader.InsertionPoint.Y + NewPoint, 0),
                        new Point3d(tmpEndPoint.X, tmpEndPoint.Y + NewPoint, 0));
                    var leaderNormal = (CrestedLeader.FirstArrowSecondPoint - CrestedLeader.FirstArrowFirstPoint).GetNormal();

                    var firstPoint = CrestedLeader.ArrowPoints[0];

                    CrestedLeader.InsertionPoint = GetPointOnPolyline(firstPoint, tempMainLine, leaderNormal);
                    CrestedLeader.EndPoint = CrestedLeader.InsertionPoint + (mainNormal * distFromEndPointToInsPoint);
                }

                CrestedLeader.TempNewStretchPoint = new Point3d(double.NaN, double.NaN, double.NaN);
                CrestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);
                CrestedLeader.UpdateEntities();
                CrestedLeader.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);
                    _entity.Position = CrestedLeader.InsertionPoint;
                    using (var resBuf = CrestedLeader.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }

                CrestedLeader.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == GripData.Status.GripAbort)
            {
                if (_gripTmpIns != null)
                {
                    CrestedLeader.InsertionPoint = _gripTmpIns;
                    CrestedLeader.EndPoint = _gripTmpEnd;

                    CrestedLeader.TempNewStretchPoint = new Point3d(double.NaN, double.NaN, double.NaN);
                    CrestedLeader.TempNewArrowPoint = new Point3d(double.NaN, double.NaN, double.NaN);
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

    private Point3d GetPointOnPolyline(Point3d point, Line line, Vector3d mainNormal)
    {
        var templine = new Line(point, point + mainNormal);
        var pts = new Point3dCollection();

        line.IntersectWith(templine, Intersect.ExtendBoth, pts, 0, 0);
        var pointOnPolyline = new Point3d();

        if (pts.Count > 0)
        {
            pointOnPolyline = pts[0];
        }

        return pointOnPolyline;
    }
}
