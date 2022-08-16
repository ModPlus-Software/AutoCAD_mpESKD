using System.Diagnostics;

namespace mpESKD.Functions.mpView
{
    using System.Linq;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Enums;
    using Base.Overrules;
    using Base.Utils;
    using Grips;
    using ModPlusAPI.Windows;

    /// <inheritdoc />
    public class ViewGripPointOverrule : BaseSmartEntityGripOverrule<mpView.View>
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

                    var view = EntityReaderService.Instance.GetFromEntity<mpView.View>(entity);
                    if (view != null)
                    {
                        // insertion (start) grip
                        var vertexGrip = new ViewVertexGrip(view, 0)
                        {
                            GripPoint = view.InsertionPoint
                        };
                        grips.Add(vertexGrip);

                        #region Text grips

                        if (view.TextDesignationPoint != Point3d.Origin && view.HasTextValue())
                        {
                            var textGrip = new ViewTextGrip(view)
                            {
                                GripPoint = view.TextDesignationPoint,
                                Name = TextGripName.TopText
                            };
                            grips.Add(textGrip);
                        }

                        vertexGrip = new ViewVertexGrip(view, GripName.EndGrip)
                        {
                            GripPoint = view.TopShelfEndPoint
                        };
                        grips.Add(vertexGrip);

                        #endregion
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
                        if (gripData is ViewVertexGrip vertexGrip)
                        {
                            var view = vertexGrip.View;

                            if (vertexGrip.GripName == GripName.StartGrip)
                            {
                                ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                            }
                            else if (vertexGrip.GripName == GripName.EndGrip)
                            {
                                view.EndPoint = vertexGrip.GripPoint + offset;
                            }

                            // Вот тут происходит перерисовка примитивов внутри блока
                            view.UpdateEntities();
                            view.BlockRecord.UpdateAnonymousBlocks();
                        }
                        else if (gripData is ViewTextGrip textGrip)
                        {
                            var view = textGrip.View;
                            var topShelfVector = (view.InsertionPoint - view.EndPoint).GetNormal().Negate();
                            var topStrokeVector = topShelfVector.GetPerpendicularVector();
                            
                            var deltaY = topStrokeVector.DotProduct(offset) / view.BlockTransform.GetScale();
                            var deltaX = topShelfVector.DotProduct(offset) / view.BlockTransform.GetScale();

                            if (double.IsNaN(textGrip.CachedAlongTopShelfTextOffset))
                            {
                                view.AlongTopShelfTextOffset = deltaX;
                            }
                            else
                            {
                                view.AlongTopShelfTextOffset = textGrip.CachedAlongTopShelfTextOffset + deltaX;
                            }

                            if (double.IsNaN(textGrip.CachedAcrossTopShelfTextOffset))
                            {
                                view.AcrossTopShelfTextOffset = deltaY;
                            }
                            else
                            {
                                view.AcrossTopShelfTextOffset = textGrip.CachedAcrossTopShelfTextOffset + deltaY;
                            }

                            view.UpdateEntities();
                            view.BlockRecord.UpdateAnonymousBlocks();
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
