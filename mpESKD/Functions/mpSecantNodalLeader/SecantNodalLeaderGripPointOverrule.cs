namespace mpESKD.Functions.mpSecantNodalLeader
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Enums;
    using Base.Overrules;
    using Grips;
    using ModPlusAPI.Windows;
    using Exception = Autodesk.AutoCAD.Runtime.Exception;

    /// <inheritdoc />
    public class SecantNodalLeaderGripPointOverrule : BaseSmartEntityGripOverrule<SecantNodalLeader>
    {
        /// <inheritdoc />
        public override void GetGripPoints(
            Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            try
            {
                if (IsApplicable(entity))
                {
                    // Удаляю все ручки - это удалит ручку вставки блока
                    grips.Clear();

                    var nodalLeader = EntityReaderService.Instance.GetFromEntity<SecantNodalLeader>(entity);
                    if (nodalLeader != null)
                    {
                        grips.Add(new SecantNodalLeaderGrip(
                            nodalLeader, GripType.BasePoint, GripName.InsertionPoint, nodalLeader.InsertionPoint));
                        grips.Add(new SecantNodalLeaderGrip(
                            nodalLeader, GripType.Point, GripName.LeaderPoint, nodalLeader.EndPoint));

                        grips.Add(new SecantNodalLevelShelfPositionGrip(nodalLeader)
                        {
                            GripPoint = nodalLeader.EndPoint +
                                        (Vector3d.YAxis * ((nodalLeader.MainTextHeight + nodalLeader.TextVerticalOffset) * nodalLeader.GetFullScale())),
                            GripType = GripType.TwoArrowsLeftRight
                        });
                    }
                }
            }
            catch (Exception exception)
            {
                if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                    ExceptionBox.Show(exception);
            }
        }

        /// <inheritdoc />
        public override void MoveGripPointsAt(
            Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            try
            {
                if (IsApplicable(entity))
                {
                    foreach (var gripData in grips)
                    {
                        if (gripData is SecantNodalLeaderGrip levelMarkGrip)
                        {
                            var gripPoint = levelMarkGrip.GripPoint;
                            var secantNodalLeader = levelMarkGrip.SecantNodalLeader;

                            if (levelMarkGrip.GripName == GripName.InsertionPoint)
                            {
                                ((BlockReference)entity).Position = gripPoint + offset;
                                secantNodalLeader.InsertionPoint = gripPoint + offset;
                            }
                            else if (levelMarkGrip.GripName == GripName.LeaderPoint)
                            {
                                secantNodalLeader.EndPoint = gripPoint + offset;
                            }

                            // Вот тут происходит перерисовка примитивов внутри блока
                            secantNodalLeader.UpdateEntities();
                            secantNodalLeader.BlockRecord.UpdateAnonymousBlocks();
                        }
                        else
                        {
                            base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                        }
                    }
                }
                else
                {
                    base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                }
            }
            catch (Exception exception)
            {
                if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                    ExceptionBox.Show(exception);
            }
        }
    }
}
