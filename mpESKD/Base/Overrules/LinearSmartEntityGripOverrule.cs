namespace mpESKD.Base.Overrules
{
    using Abstractions;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI.Windows;
    using Utils;

    /// <inheritdoc />
    public class LinearSmartEntityGripOverrule<TEntity> : BaseSmartEntityGripOverrule<TEntity>
        where TEntity : SmartEntity, ILinearEntity
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

                    var groundLine = EntityReaderService.Instance.GetFromEntity<TEntity>(entity);
                    if (groundLine != null)
                    {
                        foreach (var grip in EntityUtils.GetLinearEntityGeneralGrips(groundLine, curViewUnitSize))
                        {
                            grips.Add(grip);
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
                    EntityUtils.LinearEntityGripPointMoveProcess(
                        entity, grips, offset, () => base.MoveGripPointsAt(entity, grips, offset, bitFlags));
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
