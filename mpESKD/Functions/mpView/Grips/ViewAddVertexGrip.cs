//namespace mpESKD.Functions.mpView.Grips
//{
//    using Autodesk.AutoCAD.DatabaseServices;
//    using Autodesk.AutoCAD.EditorInput;
//    using Autodesk.AutoCAD.Geometry;
//    using Base.Enums;
//    using Base.Overrules;
//    using Base.Utils;
//    using ModPlusAPI;
//    using View = mpView.View;

//    /// <summary>
//    /// Ручка добавления вершины
//    /// </summary>
//    public class ViewAddVertexGrip : SmartEntityGripData
//    {
//        public ViewAddVertexGrip(View view, Point3d? leftPoint, Point3d? rightPoint)
//        {
//            View = view;
//            GripLeftPoint = leftPoint;
//            GripRightPoint = rightPoint;
//            GripType = GripType.Plus;
//            RubberBandLineDisabled = true;
//        }

//        /// <summary>
//        /// Экземпляр класса View
//        /// </summary>
//        public View View { get; }

//        /// <summary>
//        /// Левая точка
//        /// </summary>
//        public Point3d? GripLeftPoint { get; }

//        /// <summary>
//        /// Правая точка
//        /// </summary>
//        public Point3d? GripRightPoint { get; }

//        public Point3d NewPoint { get; set; }

//        public override string GetTooltip()
//        {
//            return Language.GetItem("gp4"); // "Добавить вершину";
//        }

//        /// <inheritdoc />
//        public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
//        {
//            if (newStatus == Status.GripStart)
//            {
//                AcadUtils.Editor.TurnForcedPickOn();
//                AcadUtils.Editor.PointMonitor += AddNewVertex_EdOnPointMonitor;
//            }

//            if (newStatus == Status.GripEnd)
//            {
//                AcadUtils.Editor.TurnForcedPickOff();
//                AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
//                using (View)
//                {
//                    Point3d? newInsertionPoint = null;

//                    if (GripLeftPoint == View.InsertionPoint)
//                    {
//                        View.MiddlePoints.Insert(0, NewPoint);
//                    }
//                    else if (GripLeftPoint == null)
//                    {
//                        View.MiddlePoints.Insert(0, View.InsertionPoint);
//                        View.InsertionPoint = NewPoint;
//                        newInsertionPoint = NewPoint;
//                    }
//                    else if (GripRightPoint == null)
//                    {
//                        View.MiddlePoints.Add(View.EndPoint);
//                        View.EndPoint = NewPoint;
//                    }
//                    else
//                    {
//                        View.MiddlePoints.Insert(View.MiddlePoints.IndexOf(GripLeftPoint.Value) + 1, NewPoint);
//                    }

//                    View.UpdateEntities();
//                    View.BlockRecord.UpdateAnonymousBlocks();
//                    using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
//                    {
//                        var blkRef = tr.GetObject(View.BlockId, OpenMode.ForWrite, true, true);
//                        if (newInsertionPoint.HasValue)
//                        {
//                            ((BlockReference)blkRef).Position = newInsertionPoint.Value;
//                        }

//                        using (var resBuf = View.GetDataForXData())
//                        {
//                            blkRef.XData = resBuf;
//                        }

//                        tr.Commit();
//                    }
//                }
//            }

//            if (newStatus == Status.GripAbort)
//            {
//                AcadUtils.Editor.TurnForcedPickOff();
//                AcadUtils.Editor.PointMonitor -= AddNewVertex_EdOnPointMonitor;
//            }

//            base.OnGripStatusChanged(entityId, newStatus);
//        }

//        private void AddNewVertex_EdOnPointMonitor(object sender, PointMonitorEventArgs pointMonitorEventArgs)
//        {
//            try
//            {
//                if (GripLeftPoint.HasValue)
//                {
//                    Line leftLine = new Line(GripLeftPoint.Value, pointMonitorEventArgs.Context.ComputedPoint)
//                    {
//                        ColorIndex = 150
//                    };
//                    pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(leftLine);
//                }

//                if (GripRightPoint.HasValue)
//                {
//                    Line rightLine = new Line(pointMonitorEventArgs.Context.ComputedPoint, GripRightPoint.Value)
//                    {
//                        ColorIndex = 150
//                    };
//                    pointMonitorEventArgs.Context.DrawContext.Geometry.Draw(rightLine);
//                }
//            }
//            catch
//            {
//                // ignored
//            }
//        }
//    }
//}