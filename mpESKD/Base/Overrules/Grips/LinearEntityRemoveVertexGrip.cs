namespace mpESKD.Base.Overrules.Grips
{
    using System.Linq;
    using Abstractions;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Base;
    using Enums;
    using ModPlusAPI;
    using Overrules;
    using Utils;

    /// <summary>
    /// Ручка удаления вершины линейного интеллектуального объекта
    /// </summary>
    public class LinearEntityRemoveVertexGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearEntityRemoveVertexGrip"/> class.
        /// </summary>
        /// <param name="smartEntity">Instance of <see cref="Base.SmartEntity"/> that implement <see cref="ILinearEntity"/></param>
        /// <param name="index">Grip index</param>
        public LinearEntityRemoveVertexGrip(SmartEntity smartEntity, int index)
        {
            SmartEntity = smartEntity;
            GripIndex = index;
            GripType = GripType.Minus;
        }

        /// <summary>
        /// Экземпляр интеллектуального объекта
        /// </summary>
        public SmartEntity SmartEntity { get; }

        /// <summary>
        /// Индекс точки
        /// </summary>
        public int GripIndex { get; }

        /// <inheritdoc />
        public override string GetTooltip()
        {
            return Language.GetItem(Invariables.LangItem, "gp3"); // "Удалить вершину";
        }

        /// <inheritdoc />
        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            using (SmartEntity)
            {
                Point3d? newInsertionPoint = null;

                var linearEntity = (ILinearEntity)SmartEntity;

                if (GripIndex == 0)
                {
                    SmartEntity.InsertionPoint = linearEntity.MiddlePoints[0];
                    newInsertionPoint = linearEntity.MiddlePoints[0];
                    linearEntity.MiddlePoints.RemoveAt(0);
                }
                else if (GripIndex == linearEntity.MiddlePoints.Count + 1)
                {
                    SmartEntity.EndPoint = linearEntity.MiddlePoints.Last();
                    linearEntity.MiddlePoints.RemoveAt(linearEntity.MiddlePoints.Count - 1);
                }
                else
                {
                    linearEntity.MiddlePoints.RemoveAt(GripIndex - 1);
                }

                SmartEntity.UpdateEntities();
                SmartEntity.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(SmartEntity.BlockId, OpenMode.ForWrite, true, true);
                    if (newInsertionPoint.HasValue)
                    {
                        ((BlockReference)blkRef).Position = newInsertionPoint.Value;
                    }

                    using (var resBuf = SmartEntity.GetDataForXData())
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