namespace mpESKD.Functions.mpLevelMark;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using System;
using Exception = Autodesk.AutoCAD.Runtime.Exception;

/// <inheritdoc />
public class LevelMarkGripPointOverrule : BaseSmartEntityGripOverrule<LevelMark>
{
    private double _cachedBottomShelfLength;

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

                var levelMark = EntityReaderService.Instance.GetFromEntity<LevelMark>(entity);
                if (levelMark != null)
                {
                    grips.Add(new LevelMarkGrip(
                        levelMark, GripType.BasePoint, GripName.BasePoint, levelMark.InsertionPoint));
                    grips.Add(new LevelMarkGrip(
                        levelMark, GripType.Point, GripName.ObjectPoint, levelMark.ObjectPoint));
                    grips.Add(new LevelMarkGrip(
                        levelMark, GripType.Point, GripName.BottomShelfStartPoint, levelMark.BottomShelfStartPoint));
                    grips.Add(new LevelMarkGrip(
                        levelMark, GripType.Point, GripName.ArrowPoint, levelMark.EndPoint));
                    grips.Add(new LevelMarkGrip(
                        levelMark, GripType.Point, GripName.TopShelfPoint, levelMark.ShelfPoint));

                    _cachedBottomShelfLength = Math.Abs(levelMark.EndPoint.X - levelMark.BottomShelfStartPoint.X);
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
                    if (gripData is LevelMarkGrip levelMarkGrip)
                    {
                        var gripPoint = levelMarkGrip.GripPoint;
                        var levelMark = levelMarkGrip.LevelMark;
                        var scale = levelMark.GetFullScale();
                        var horV = (levelMark.EndPoint - levelMark.ObjectPoint).GetNormal();

                        if (levelMarkGrip.GripName == GripName.BasePoint)
                        {
                            ((BlockReference)entity).Position = gripPoint + offset;
                            levelMark.InsertionPoint = gripPoint + offset;
                        }
                        else if (levelMarkGrip.GripName == GripName.ObjectPoint)
                        {
                            levelMark.ObjectPoint = gripPoint + offset;
                            levelMark.BottomShelfStartPoint = new Point3d(
                                levelMark.BottomShelfStartPoint.X,
                                levelMark.ObjectPoint.Y,
                                levelMark.BottomShelfStartPoint.Z);
                            levelMark.EndPoint = new Point3d(
                                levelMark.EndPoint.X,
                                levelMark.ObjectPoint.Y,
                                levelMark.EndPoint.Z);
                        }
                        else if (levelMarkGrip.GripName == GripName.BottomShelfStartPoint)
                        {
                            levelMark.BottomShelfStartPoint = gripPoint + offset;
                            if (levelMark.ObjectLine)
                            {
                                levelMark.ObjectPoint =
                                    levelMark.BottomShelfStartPoint - (horV * levelMark.ObjectLineOffset * scale);

                                levelMark.EndPoint =
                                    levelMark.BottomShelfStartPoint + (horV * _cachedBottomShelfLength);
                            }
                            else
                            {
                                levelMark.ObjectPoint = new Point3d(
                                    levelMark.ObjectPoint.X,
                                    levelMark.BottomShelfStartPoint.Y,
                                    levelMark.ObjectPoint.Z);
                                levelMark.EndPoint =
                                    levelMark.BottomShelfStartPoint + (horV * levelMark.BottomShelfLength * scale);
                            }
                        }
                        else if (levelMarkGrip.GripName == GripName.ArrowPoint)
                        {
                            levelMark.SetArrowPoint(gripPoint + offset);
                        }
                        else if (levelMarkGrip.GripName == GripName.TopShelfPoint)
                        {
                            levelMark.ShelfPoint = gripPoint + offset;
                        }

                        // Вот тут происходит перерисовка примитивов внутри блока
                        levelMark.UpdateEntities();
                        levelMark.BlockRecord.UpdateAnonymousBlocks();
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