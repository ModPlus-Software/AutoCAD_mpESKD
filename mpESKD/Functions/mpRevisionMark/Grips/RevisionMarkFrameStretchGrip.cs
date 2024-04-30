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

/// <summary>
/// Ручка растяжения рамки
/// </summary>
public class RevisionMarkFrameStretchGrip : SmartEntityGripData
{
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
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

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
                RevisionMark.GetFullScale());

            if (revisionFramesAsPolylines[0] != null)
            {
                var polyline = revisionFramesAsPolylines[0];
                polyline.ColorIndex = 150;
                pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(polyline);
            }
            else if (revisionFramesAsCircles[0] != null)
            {
                var circle = revisionFramesAsCircles[0];
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