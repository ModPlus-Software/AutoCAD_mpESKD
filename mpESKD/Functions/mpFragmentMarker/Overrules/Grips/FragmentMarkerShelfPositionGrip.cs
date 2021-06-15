namespace mpESKD.Functions.mpFragmentMarker.Overrules.Grips
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using ModPlusAPI;

    /// <summary>
    /// Ручка узловой выноски, меняющая положение полки
    /// </summary>
    public class FragmentMarkerShelfPositionGrip : SmartEntityGripData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NodalLevelShelfPositionGrip"/> class.
        /// </summary>
        /// <param name="fragmentMarker">Экземпляр <see cref="mpNodalLeader.NodalLeader"/></param>
        public FragmentMarkerShelfPositionGrip(FragmentMarker fragmentMarker)
        {
            FragmentMarker = fragmentMarker;
        }
        
        /// <summary>
        /// Экземпляр <see cref="mpNodalLeader.NodalLeader"/>
        /// </summary>
        public FragmentMarker FragmentMarker { get; }
        
        /// <inheritdoc />
        public override string GetTooltip()
        {
            return Language.GetItem("p78"); // "Положение полки";
        }
        
        /// <inheritdoc />
        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            using (FragmentMarker)
            {
                FragmentMarker.ShelfPosition = FragmentMarker.ShelfPosition == ShelfPosition.Left
                    ? ShelfPosition.Right
                    : ShelfPosition.Left;

                FragmentMarker.UpdateEntities();
                FragmentMarker.BlockRecord.UpdateAnonymousBlocks();
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(FragmentMarker.BlockId, OpenMode.ForWrite, true, true);
                    
                    using (var resBuf = FragmentMarker.GetDataForXData())
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
