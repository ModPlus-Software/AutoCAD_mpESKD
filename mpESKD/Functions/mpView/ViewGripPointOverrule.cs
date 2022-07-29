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
                        var vertexGrip = new ViewVertexGrip(view, GripName.StartGrip)
                        {
                            GripPoint = view.InsertionPoint
                        };
                        grips.Add(vertexGrip);

                        #region Text grips

                        if (view.TopDesignationPoint != Point3d.Origin && view.HasTextValue())
                        {
                            var textGrip = new ViewTextGrip(view)
                            {
                                GripPoint = view.TopDesignationPoint,
                                Name = TextGripName.TopText
                            };
                            grips.Add(textGrip);
                        }

                        vertexGrip = new ViewVertexGrip(view, GripName.EndGrip)
                        {
                            GripPoint = view.TopShelfEndPoint
                        };
                        grips.Add(vertexGrip);

                        //if (view.BottomDesignationPoint != Point3d.Origin && view.HasTextValue())
                        //{
                        //    var textGrip = new ViewTextGrip(view)
                        //    {
                        //        GripPoint = view.BottomDesignationPoint,
                        //        Name = TextGripName.BottomText
                        //    };
                        //    grips.Add(textGrip);
                        //}

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

                                //view.InsertionPoint = vertexGrip.GripPoint + offset;
                            }

                            // Вот тут происходит перерисовка примитивов внутри блока
                            view.UpdateEntities();
                            view.BlockRecord.UpdateAnonymousBlocks();
                        }
                        else if (gripData is ViewTextGrip textGrip)
                        {
                            var view = textGrip.View;
                            if (textGrip.Name == TextGripName.TopText)
                            {
                                base.MoveGripPointsAt(entity, grips, offset, bitFlags);
                                Debug.Print("textGrip.Name == TextGripName.TopText");
                                AcadUtils.Editor.WriteMessage("textGrip.Name == TextGripName.TopText");
                            }

                            view.UpdateEntities();
                            view.BlockRecord.UpdateAnonymousBlocks();
                        }
                        //else if (gripData is ViewAddVertexGrip addVertexGrip)
                        //{
                        //    addVertexGrip.NewPoint = addVertexGrip.GripPoint + offset;
                        //}
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
