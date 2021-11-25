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

                        // middle points
                        for (var index = 0; index < view.MiddlePoints.Count; index++)
                        {
                            vertexGrip = new ViewVertexGrip(view, index + 1)
                            {
                                GripPoint = view.MiddlePoints[index]
                            };
                            grips.Add(vertexGrip);

                            var removeVertexGrip = new ViewRemoveVertexGrip(view, index + 1)
                            {
                                GripPoint = view.MiddlePoints[index] - (Vector3d.YAxis * 20 * curViewUnitSize)
                            };
                            grips.Add(removeVertexGrip);
                        }

                        // end point
                        vertexGrip = new ViewVertexGrip(view, view.MiddlePoints.Count + 1)
                        {
                            GripPoint = view.EndPoint
                        };
                        grips.Add(vertexGrip);

                        #region AddVertex grips

                        // add vertex grips
                        for (var i = 0; i < view.MiddlePoints.Count; i++)
                        {
                            if (i == 0)
                            {
                                var addVertexGrip = new ViewAddVertexGrip(
                                    view,
                                    view.InsertionPoint, view.MiddlePoints[i])
                                {
                                    GripPoint = GeometryUtils.GetMiddlePoint3d(view.InsertionPoint, view.MiddlePoints[i])
                                };
                                grips.Add(addVertexGrip);
                            }
                            else
                            {
                                var addVertexGrip = new ViewAddVertexGrip(
                                    view,
                                    view.MiddlePoints[i - 1], view.MiddlePoints[i])
                                {
                                    GripPoint = GeometryUtils.GetMiddlePoint3d(view.MiddlePoints[i - 1], view.MiddlePoints[i])
                                };
                                grips.Add(addVertexGrip);
                            }

                            // last segment
                            if (i == view.MiddlePoints.Count - 1)
                            {
                                var addVertexGrip = new ViewAddVertexGrip(
                                    view,
                                    view.MiddlePoints[i], view.EndPoint)
                                {
                                    GripPoint = GeometryUtils.GetMiddlePoint3d(view.MiddlePoints[i], view.EndPoint)
                                };
                                grips.Add(addVertexGrip);
                            }
                        }

                        if (!view.MiddlePoints.Any())
                        {
                            var addVertexGrip = new ViewAddVertexGrip(view, view.InsertionPoint, view.EndPoint)
                            {
                                GripPoint = GeometryUtils.GetMiddlePoint3d(view.InsertionPoint, view.EndPoint)
                            };
                            grips.Add(addVertexGrip);
                        }

                        #endregion

                        #region Reverse Grips

                        var reverseGrip = new ViewReverseGrip(view)
                        {
                            GripPoint = view.EntityDirection == EntityDirection.LeftToRight
                                ? view.TopShelfEndPoint - (Vector3d.XAxis * 20 * curViewUnitSize)
                                : view.TopShelfEndPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                        };
                        grips.Add(reverseGrip);
                        reverseGrip = new ViewReverseGrip(view)
                        {
                            GripPoint = view.EntityDirection == EntityDirection.LeftToRight
                                ? view.BottomShelfEndPoint - (Vector3d.XAxis * 20 * curViewUnitSize)
                                : view.BottomShelfEndPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                        };
                        grips.Add(reverseGrip);

                        #endregion

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

                        if (view.BottomDesignationPoint != Point3d.Origin && view.HasTextValue())
                        {
                            var textGrip = new ViewTextGrip(view)
                            {
                                GripPoint = view.BottomDesignationPoint,
                                Name = TextGripName.BottomText
                            };
                            grips.Add(textGrip);
                        }

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
                            var View = vertexGrip.View;

                            if (vertexGrip.GripIndex == 0)
                            {
                                ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                                View.InsertionPoint = vertexGrip.GripPoint + offset;
                            }
                            else if (vertexGrip.GripIndex == View.MiddlePoints.Count + 1)
                            {
                                View.EndPoint = vertexGrip.GripPoint + offset;
                            }
                            else
                            {
                                View.MiddlePoints[vertexGrip.GripIndex - 1] =
                                    vertexGrip.GripPoint + offset;
                            }

                            // Вот тут происходит перерисовка примитивов внутри блока
                            View.UpdateEntities();
                            View.BlockRecord.UpdateAnonymousBlocks();
                        }
                        else if (gripData is ViewTextGrip textGrip)
                        {
                            var View = textGrip.View;
                            if (textGrip.Name == TextGripName.TopText)
                            {
                                var topStrokeVector = View.MiddlePoints.Any()
                                    ? (View.InsertionPoint - View.MiddlePoints.First()).GetNormal()
                                    : (View.InsertionPoint - View.EndPoint).GetNormal();
                                var topShelfVector = topStrokeVector.GetPerpendicularVector().Negate();
                                var deltaY = topStrokeVector.DotProduct(offset) / View.BlockTransform.GetScale();
                                var deltaX = topShelfVector.DotProduct(offset) / View.BlockTransform.GetScale();
                                if (double.IsNaN(textGrip.CachedAlongTopShelfTextOffset))
                                {
                                    View.AlongTopShelfTextOffset = deltaX;
                                }
                                else
                                {
                                    View.AlongTopShelfTextOffset = textGrip.CachedAlongTopShelfTextOffset + deltaX;
                                }

                                if (double.IsNaN(textGrip.CachedAcrossTopShelfTextOffset))
                                {
                                    View.AcrossTopShelfTextOffset = deltaY;
                                }
                                else
                                {
                                    View.AcrossTopShelfTextOffset = textGrip.CachedAcrossTopShelfTextOffset + deltaY;
                                }

                                if (MainSettings.Instance.ViewDependentTextMovement)
                                {
                                    if (double.IsNaN(textGrip.CachedAlongBottomShelfTextOffset))
                                    {
                                        View.AlongBottomShelfTextOffset = deltaX;
                                    }
                                    else
                                    {
                                        View.AlongBottomShelfTextOffset = textGrip.CachedAlongBottomShelfTextOffset + deltaX;
                                    }

                                    if (double.IsNaN(textGrip.CachedAcrossBottomShelfTextOffset))
                                    {
                                        View.AcrossBottomShelfTextOffset = deltaY;
                                    }
                                    else
                                    {
                                        View.AcrossBottomShelfTextOffset = textGrip.CachedAcrossBottomShelfTextOffset + deltaY;
                                    }
                                }
                            }

                            if (textGrip.Name == TextGripName.BottomText)
                            {
                                var bottomStrokeVector = View.MiddlePoints.Any()
                                    ? (View.EndPoint - View.MiddlePoints.Last()).GetNormal()
                                    : (View.EndPoint - View.InsertionPoint).GetNormal();
                                var bottomShelfVector = bottomStrokeVector.GetPerpendicularVector();
                                var deltaY = bottomStrokeVector.DotProduct(offset) / View.BlockTransform.GetScale();
                                var deltaX = bottomShelfVector.DotProduct(offset) / View.BlockTransform.GetScale();

                                if (double.IsNaN(textGrip.CachedAlongBottomShelfTextOffset))
                                {
                                    View.AlongBottomShelfTextOffset = deltaX;
                                }
                                else
                                {
                                    View.AlongBottomShelfTextOffset = textGrip.CachedAlongBottomShelfTextOffset + deltaX;
                                }

                                if (double.IsNaN(textGrip.CachedAcrossBottomShelfTextOffset))
                                {
                                    View.AcrossBottomShelfTextOffset = deltaY;
                                }
                                else
                                {
                                    View.AcrossBottomShelfTextOffset = textGrip.CachedAcrossBottomShelfTextOffset + deltaY;
                                }

                                if (MainSettings.Instance.ViewDependentTextMovement)
                                {
                                    if (double.IsNaN(textGrip.CachedAlongTopShelfTextOffset))
                                    {
                                        View.AlongTopShelfTextOffset = deltaX;
                                    }
                                    else
                                    {
                                        View.AlongTopShelfTextOffset = textGrip.CachedAlongTopShelfTextOffset + deltaX;
                                    }

                                    if (double.IsNaN(textGrip.CachedAcrossTopShelfTextOffset))
                                    {
                                        View.AcrossTopShelfTextOffset = deltaY;
                                    }
                                    else
                                    {
                                        View.AcrossTopShelfTextOffset = textGrip.CachedAcrossTopShelfTextOffset + deltaY;
                                    }
                                }
                            }

                            View.UpdateEntities();
                            View.BlockRecord.UpdateAnonymousBlocks();
                        }
                        else if (gripData is ViewAddVertexGrip addVertexGrip)
                        {
                            addVertexGrip.NewPoint = addVertexGrip.GripPoint + offset;
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
