namespace mpESKD.Functions.mpView.Grips
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using View = mpView.View;

    /// <summary>
    /// Ручка реверса разреза
    /// </summary>
    public class ViewReverseGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewReverseGrip"/> class.
        /// </summary>
        /// <param name="view">Экземпляр класса <see cref="mpView.View"/></param>
        public ViewReverseGrip(View view)
        {
            View = view;
            GripType = GripType.BasePoint;
        }

        /// <summary>
        /// Экземпляр класса <see cref="mpView.View"/>
        /// </summary>
        public View View { get; }

        /// <inheritdoc/>
        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            using (View)
            {
                Point3d newInsertionPoint = View.EndPoint;
                View.EndPoint = View.InsertionPoint;
                View.InsertionPoint = newInsertionPoint;
                

                // swap direction
                View.EntityDirection = View.EntityDirection == EntityDirection.LeftToRight
                    ? EntityDirection.RightToLeft
                    : EntityDirection.LeftToRight;
                View.BlockTransform = View.BlockTransform.Inverse();

                // swap text offsets
                
                View.UpdateEntities();
                View.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(View.BlockId, OpenMode.ForWrite, true, true);
                    ((BlockReference)blkRef).Position = newInsertionPoint;
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