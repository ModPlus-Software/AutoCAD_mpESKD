using Autodesk.AutoCAD.GraphicsInterface;
using DocumentFormat.OpenXml.Drawing;

namespace mpESKD.Functions.mpRevisionMark.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ручка вершин
/// </summary>
public class RevisionMarkFrameStretchGrip : SmartEntityGripData
{
    private readonly Point2d[] _points;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkFrameStretchGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public RevisionMarkFrameStretchGrip(RevisionMark revisionMark, int gripIndex)
    {
        RevisionMark = revisionMark;
        GripIndex = gripIndex;
        GripType = GripType.Point;
        RubberBandLineDisabled = true;

        /*
        var insertionPoint = RevisionMark.InsertionPoint.ToPoint2d();
        _points = new[]
        {
            new Point2d(
                insertionPoint.X + RevisionMark.FrameRevisionTextPoints[0].X,
                insertionPoint.Y + RevisionMark.FrameRevisionTextPoints[0].Y),
            new Point2d(
                insertionPoint.X + RevisionMark.FrameRevisionTextPoints[1].X,
                insertionPoint.Y + RevisionMark.FrameRevisionTextPoints[1].Y),
            new Point2d(
                insertionPoint.X + RevisionMark.FrameRevisionTextPoints[2].X,
                insertionPoint.Y + RevisionMark.FrameRevisionTextPoints[2].Y),
            new Point2d(
                insertionPoint.X + RevisionMark.FrameRevisionTextPoints[3].X,
                insertionPoint.Y + RevisionMark.FrameRevisionTextPoints[3].Y),
        };*/
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

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
        return Language.GetItem("gp1"); // stretch
    }

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        if (newStatus == Status.GripStart)
        {
            AcadUtils.Editor.TurnForcedPickOn();
            AcadUtils.Editor.PointMonitor += AddNewVertex_EdOnPointMonitor;
        }

        if (newStatus == Status.GripEnd)
        {
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;

            using (RevisionMark)
            {
                // Точка ручки растяжения, относительно точки выноски, 
                RevisionMark.RevisionFrameStretchPoints[GripIndex] = NewPoint;

                RevisionMark.UpdateEntities();
                RevisionMark.BlockRecord.UpdateAnonymousBlocks();

                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(RevisionMark.BlockId, OpenMode.ForWrite, true, true);

                    using (var resBuf = RevisionMark.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }
        }

        if (newStatus == Status.GripAbort)
        {
            AcadUtils.Editor.TurnForcedPickOff();
            AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
        }

        base.OnGripStatusChanged(entityId, newStatus);
    }

    private void AddNewVertex_EdOnPointMonitor(object sender, PointMonitorEventArgs pointMonitorEventArgs)
    {
        try
        {
            /*
            var line = new Line(RevisionMark.LeaderPoints[GripIndex], pointMonitorEventArgs.Context.ComputedPoint)
            {
                ColorIndex = 120
            };*/
         
            /*
            pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(new Circle(
                line.StartPoint,
                Vector3d.YAxis, 
                line.Length)
            {
                
                ColorIndex = 150
            });*/

           // pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(line);

            /*      
            var tmpRevisionFrame = new Point3d(
                RevisionMark.LeaderPoints[GripIndex],
                RevisionMark.LeaderPoints[GripIndex],
                pointMonitorEventArgs.Context.ComputedPoint,
                RevisionFrameType.Rectangular,

            );*/

            List<Polyline> revisionFramesAsPolylines = new ();
            List<Circle> revisionFramesAsCircles = new ();

            var frameType = (RevisionFrameType)Enum.GetValues(typeof(RevisionFrameType))
                .GetValue(RevisionMark.RevisionFrameTypes[GripIndex]);

            RevisionMark.CreateRevisionFrame(
                RevisionMark.LeaderPoints[GripIndex],
                RevisionMark.LeaderPoints[GripIndex],
                pointMonitorEventArgs.Context.ComputedPoint,
                frameType,
                revisionFramesAsPolylines,
                revisionFramesAsCircles,
                RevisionMark.GetFullScale()
                );

            foreach (var polyline in revisionFramesAsPolylines)
            {
                polyline.ColorIndex = 150;

                Point3dCollection points = new Point3dCollection();
                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    points.Add(polyline.GetPoint2dAt(i).ToPoint3d());
                }

                pointMonitorEventArgs.Context.DrawContext.Geometry.PolylineEye(points);
                //pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(polyline);
            }

            foreach (var circle in revisionFramesAsCircles)
            {
                circle.ColorIndex = 150;
                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(circle);
            }
        }
        catch
        {
            // ignored
        }
    }
}