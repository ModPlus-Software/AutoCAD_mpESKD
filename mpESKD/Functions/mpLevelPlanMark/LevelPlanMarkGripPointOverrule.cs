namespace mpESKD.Functions.mpLevelPlanMark;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;

/// <inheritdoc />
public class LevelPlanMarkGripPointOverrule : BaseSmartEntityGripOverrule<LevelPlanMark>
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
                //grips.Clear();
                var blkRef = (BlockReference)entity;
                GripData toRemove = null;

                foreach (var gd in grips)
                {
                    if (gd.GripPoint == blkRef.Position)
                    {
                        toRemove = gd;
                        break;
                    }
                }
                if (toRemove != null)
                {
                    grips.Remove(toRemove);
                }

                var levelPlanMark = EntityReaderService.Instance.GetFromEntity<LevelPlanMark>(entity);
                if (levelPlanMark != null)
                {
                    
                    // insertion (start) grip
                    var vertexGrip = new LevelPlanMarkVertexGrip(levelPlanMark, 0)
                    {
                        GripPoint = levelPlanMark.InsertionPoint
                    };
                    grips.Add(vertexGrip);

                    // получаем ручку типа рамки
                    grips.Add(new LevelPlanMarkTypeGrip(levelPlanMark)
                    {

                        GripPoint = new Point3d(
                            (levelPlanMark.InsertionPoint.X + levelPlanMark.BorderWidth / 2 * levelPlanMark.GetFullScale()),
                            (levelPlanMark.InsertionPoint.Y - levelPlanMark.BorderHeight / 2 * levelPlanMark.GetFullScale()),
                            levelPlanMark.InsertionPoint.Z)
                    });//insertionPoint.X + borderHalfLength, insertionPoint.Y - borderHalfHeight
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
                    if (!(gripData is LevelPlanMarkVertexGrip vertexGrip))
                        continue;
                    var levelPlanMark = vertexGrip.LevelPlanMark;

                    if (vertexGrip.GripIndex == 0)
                    {
                        ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                        levelPlanMark.InsertionPoint = vertexGrip.GripPoint + offset;
                    }

                    // Вот тут происходит перерисовка примитивов внутри блока
                    levelPlanMark.UpdateEntities();
                    levelPlanMark.BlockRecord.UpdateAnonymousBlocks();
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