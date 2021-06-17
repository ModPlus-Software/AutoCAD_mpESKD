namespace mpESKD.Functions.mpSecantNodalLeader.Grips
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;

    /// <summary>
    /// Ручка узловой выноски, меняющая положение полки
    /// </summary>
    public class SecantNodalLevelShelfPositionGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecantNodalLevelShelfPositionGrip"/> class.
        /// </summary>
        /// <param name="secantNodalLeader">Экземпляр <see cref="mpNodalLeader.NodalLeader"/></param>
        public SecantNodalLevelShelfPositionGrip(SecantNodalLeader secantNodalLeader)
        {
            SecantNodalLeader = secantNodalLeader;
        }
        
        /// <summary>
        /// Экземпляр <see cref="mpSecantNodalLeader.SecantNodalLeader"/>
        /// </summary>
        public SecantNodalLeader SecantNodalLeader { get; }
        
        /// <inheritdoc />
        public override string GetTooltip()
        {
            return Language.GetItem("p78"); // "Положение полки";
        }
        
        /// <inheritdoc />
        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            using (SecantNodalLeader)
            {
                SecantNodalLeader.ShelfPosition = SecantNodalLeader.ShelfPosition == ShelfPosition.Left
                    ? ShelfPosition.Right
                    : ShelfPosition.Left;

                SecantNodalLeader.UpdateEntities();
                SecantNodalLeader.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(SecantNodalLeader.BlockId, OpenMode.ForWrite, true, true);
                    
                    using (var resBuf = SecantNodalLeader.GetDataForXData())
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
