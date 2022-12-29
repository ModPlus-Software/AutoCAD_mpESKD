namespace mpESKD.Functions.mpSecantNodalLeader;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Base.Overrules.Grips;
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

                    if (!(!string.IsNullOrEmpty(nodalLeader.NodeNumber) |
                          !string.IsNullOrEmpty(nodalLeader.SheetNumber)))
                        return;
                    grips.Add(new SecantNodalLeaderGrip(
                        nodalLeader, GripType.Point, GripName.LeaderPoint, nodalLeader.EndPoint));

                    var shelfLength = nodalLeader.TopShelfLineLength;

                    if (nodalLeader.ShelfPosition == ShelfPosition.Left)
                    {
                        shelfLength = -shelfLength;
                    }

                    if (nodalLeader.ScaleFactorX < 0)
                    {
                        shelfLength = -shelfLength;
                    }

                    var shelfPointGrip = nodalLeader.EndPoint +
                                         (Vector3d.YAxis *
                                          ((nodalLeader.MainTextHeight + nodalLeader.TextVerticalOffset) *
                                           nodalLeader.GetFullScale()));
                    var alignGripPoint = shelfPointGrip + Vector3d.XAxis * shelfLength;
                    if (nodalLeader.IsRotated & !nodalLeader.IsTextAlwaysHorizontal)
                    {
                        shelfPointGrip = shelfPointGrip.RotateBy(nodalLeader.Rotation, Vector3d.ZAxis, nodalLeader.EndPoint);
                        alignGripPoint = alignGripPoint.RotateBy(nodalLeader.Rotation, Vector3d.ZAxis, nodalLeader.EndPoint);
                    }

                    grips.Add(new SecantNodalLevelShelfPositionGrip(nodalLeader)
                    {
                        GripPoint = shelfPointGrip,
                        GripType = GripType.TwoArrowsLeftRight
                    });

                    if ((!string.IsNullOrEmpty(nodalLeader.NodeAddress)) & 
                        ((!string.IsNullOrEmpty(nodalLeader.NodeNumber) | !string.IsNullOrEmpty(nodalLeader.SheetNumber)) || 
                         (!string.IsNullOrEmpty(nodalLeader.NodeNumber) & !string.IsNullOrEmpty(nodalLeader.SheetNumber))))
                    {
                        grips.Add(new EntityTextAlignGrip(nodalLeader,
                            () => nodalLeader.ValueHorizontalAlignment,
                            (setAlignEntity) => nodalLeader.ValueHorizontalAlignment = setAlignEntity)
                        {
                            GripPoint = alignGripPoint
                        });
                    }
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