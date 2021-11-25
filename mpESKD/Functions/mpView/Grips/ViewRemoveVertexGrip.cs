namespace mpESKD.Functions.mpView.Grips
{
    using System.Linq;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;
    using View = mpView.View;

    /// <summary>
    /// Ручка удаления вершины
    /// </summary>
    public class ViewRemoveVertexGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewRemoveVertexGrip"/> class.
        /// </summary>
        /// <param name="View">Экземпляр класса <see cref="mpView.View"/></param>
        /// <param name="index">Индекс ручки</param>
        public ViewRemoveVertexGrip(View view, int index)
        {
            View = view;
            GripIndex = index;
            GripType = GripType.Minus;
        }

        /// <summary>
        /// Экземпляр класса <see cref="mpView.View"/>
        /// </summary>
        public View View{ get; }

        /// <summary>
        /// Индекс ручки
        /// </summary>
        public int GripIndex { get; }

        /// <inheritdoc />
        public override string GetTooltip()
        {
            return Language.GetItem("gp3"); // "Удалить вершину";
        }

        /// <inheritdoc />
        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            using (View)
            {
                Point3d? newInsertionPoint = null;

                if (GripIndex == 0)
                {
                    View.InsertionPoint = View.MiddlePoints[0];
                    newInsertionPoint = View.MiddlePoints[0];
                    View.MiddlePoints.RemoveAt(0);
                }
                else if (GripIndex == View.MiddlePoints.Count + 1)
                {
                    View.EndPoint = View.MiddlePoints.Last();
                    View.MiddlePoints.RemoveAt(View.MiddlePoints.Count - 1);
                }
                else
                {
                    View.MiddlePoints.RemoveAt(GripIndex - 1);
                }

                View.UpdateEntities();
                View.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(View.BlockId, OpenMode.ForWrite, true, true);
                    if (newInsertionPoint.HasValue)
                    {
                        ((BlockReference)blkRef).Position = newInsertionPoint.Value;
                    }

                    using (var resBuf = View.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }
            }

            return ReturnValue.GetNewGripPoints;
        }
    }
}