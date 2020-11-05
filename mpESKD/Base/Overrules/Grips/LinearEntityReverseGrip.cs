namespace mpESKD.Base.Overrules.Grips
{
    using Abstractions;
    using Autodesk.AutoCAD.DatabaseServices;
    using Enums;
    using Utils;

    /// <summary>
    /// Ручка реверса линейного интеллектуального объекта
    /// </summary>
    public class LinearEntityReverseGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearEntityReverseGrip"/> class.
        /// </summary>
        /// <param name="smartEntity">Instance of <see cref="SmartEntity"/> that implement <see cref="ILinearEntity"/></param>
        public LinearEntityReverseGrip(SmartEntity smartEntity)
        {
            SmartEntity = smartEntity;
            GripType = GripType.Mirror;
        }

        /// <summary>
        /// Экземпляр интеллектуального объекта
        /// </summary>
        public SmartEntity SmartEntity { get; }

        /// <inheritdoc />
        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            using (SmartEntity)
            {
                var newInsertionPoint = SmartEntity.EndPoint;
                SmartEntity.EndPoint = SmartEntity.InsertionPoint;
                SmartEntity.InsertionPoint = newInsertionPoint;
                ((ILinearEntity)SmartEntity).MiddlePoints.Reverse();
                SmartEntity.BlockTransform = SmartEntity.BlockTransform.Inverse();

                SmartEntity.UpdateEntities();
                SmartEntity.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(SmartEntity.BlockId, OpenMode.ForWrite, true, true);
                    ((BlockReference)blkRef).Position = newInsertionPoint;
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
