namespace mpESKD.Functions.mpSection.Overrules.Grips
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Base;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;
    using Section = mpSection.Section;

    /// <summary>
    /// Ручка добавления вершины
    /// </summary>
    public class SectionAddVertexGrip : SmartEntityGripData
    {
        public SectionAddVertexGrip(Section section, Point3d? leftPoint, Point3d? rightPoint)
        {
            Section = section;
            GripLeftPoint = leftPoint;
            GripRightPoint = rightPoint;
            GripType = GripType.Plus;
            RubberBandLineDisabled = true;
        }

        /// <summary>
        /// Экземпляр класса Section
        /// </summary>
        public Section Section { get; }

        /// <summary>
        /// Левая точка
        /// </summary>
        public Point3d? GripLeftPoint { get; }

        /// <summary>
        /// Правая точка
        /// </summary>
        public Point3d? GripRightPoint { get; }

        public Point3d NewPoint { get; set; }

        public override string GetTooltip()
        {
            return Language.GetItem(Invariables.LangItem, "gp4"); // "Добавить вершину";
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
                using (Section)
                {
                    Point3d? newInsertionPoint = null;

                    if (GripLeftPoint == Section.InsertionPoint)
                    {
                        Section.MiddlePoints.Insert(0, NewPoint);
                    }
                    else if (GripLeftPoint == null)
                    {
                        Section.MiddlePoints.Insert(0, Section.InsertionPoint);
                        Section.InsertionPoint = NewPoint;
                        newInsertionPoint = NewPoint;
                    }
                    else if (GripRightPoint == null)
                    {
                        Section.MiddlePoints.Add(Section.EndPoint);
                        Section.EndPoint = NewPoint;
                    }
                    else
                    {
                        Section.MiddlePoints.Insert(Section.MiddlePoints.IndexOf(GripLeftPoint.Value) + 1, NewPoint);
                    }

                    Section.UpdateEntities();
                    Section.BlockRecord.UpdateAnonymousBlocks();
                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        var blkRef = tr.GetObject(Section.BlockId, OpenMode.ForWrite, true, true);
                        if (newInsertionPoint.HasValue)
                        {
                            ((BlockReference)blkRef).Position = newInsertionPoint.Value;
                        }

                        using (var resBuf = Section.GetDataForXData())
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
                if (GripLeftPoint.HasValue)
                {
                    Line leftLine = new Line(GripLeftPoint.Value, pointMonitorEventArgs.Context.ComputedPoint)
                    {
                        ColorIndex = 150
                    };
                    pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(leftLine);
                }

                if (GripRightPoint.HasValue)
                {
                    Line rightLine = new Line(pointMonitorEventArgs.Context.ComputedPoint, GripRightPoint.Value)
                    {
                        ColorIndex = 150
                    };
                    pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(rightLine);
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}