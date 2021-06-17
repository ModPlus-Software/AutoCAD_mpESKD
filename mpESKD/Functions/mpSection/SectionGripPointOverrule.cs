namespace mpESKD.Functions.mpSection
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
    public class SectionGripPointOverrule : BaseSmartEntityGripOverrule<mpSection.Section>
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

                    var section = EntityReaderService.Instance.GetFromEntity<mpSection.Section>(entity);
                    if (section != null)
                    {
                        // insertion (start) grip
                        var vertexGrip = new SectionVertexGrip(section, 0)
                        {
                            GripPoint = section.InsertionPoint
                        };
                        grips.Add(vertexGrip);

                        // middle points
                        for (var index = 0; index < section.MiddlePoints.Count; index++)
                        {
                            vertexGrip = new SectionVertexGrip(section, index + 1)
                            {
                                GripPoint = section.MiddlePoints[index]
                            };
                            grips.Add(vertexGrip);

                            var removeVertexGrip = new SectionRemoveVertexGrip(section, index + 1)
                            {
                                GripPoint = section.MiddlePoints[index] - (Vector3d.YAxis * 20 * curViewUnitSize)
                            };
                            grips.Add(removeVertexGrip);
                        }

                        // end point
                        vertexGrip = new SectionVertexGrip(section, section.MiddlePoints.Count + 1)
                        {
                            GripPoint = section.EndPoint
                        };
                        grips.Add(vertexGrip);

                        #region AddVertex grips

                        // add vertex grips
                        for (var i = 0; i < section.MiddlePoints.Count; i++)
                        {
                            if (i == 0)
                            {
                                var addVertexGrip = new SectionAddVertexGrip(
                                    section,
                                    section.InsertionPoint, section.MiddlePoints[i])
                                {
                                    GripPoint = GeometryUtils.GetMiddlePoint3d(section.InsertionPoint, section.MiddlePoints[i])
                                };
                                grips.Add(addVertexGrip);
                            }
                            else
                            {
                                var addVertexGrip = new SectionAddVertexGrip(
                                    section,
                                    section.MiddlePoints[i - 1], section.MiddlePoints[i])
                                {
                                    GripPoint = GeometryUtils.GetMiddlePoint3d(section.MiddlePoints[i - 1], section.MiddlePoints[i])
                                };
                                grips.Add(addVertexGrip);
                            }

                            // last segment
                            if (i == section.MiddlePoints.Count - 1)
                            {
                                var addVertexGrip = new SectionAddVertexGrip(
                                    section,
                                    section.MiddlePoints[i], section.EndPoint)
                                {
                                    GripPoint = GeometryUtils.GetMiddlePoint3d(section.MiddlePoints[i], section.EndPoint)
                                };
                                grips.Add(addVertexGrip);
                            }
                        }

                        if (!section.MiddlePoints.Any())
                        {
                            var addVertexGrip = new SectionAddVertexGrip(section, section.InsertionPoint, section.EndPoint)
                            {
                                GripPoint = GeometryUtils.GetMiddlePoint3d(section.InsertionPoint, section.EndPoint)
                            };
                            grips.Add(addVertexGrip);
                        }

                        #endregion

                        #region Reverse Grips

                        var reverseGrip = new SectionReverseGrip(section)
                        {
                            GripPoint = section.EntityDirection == EntityDirection.LeftToRight
                                ? section.TopShelfEndPoint - (Vector3d.XAxis * 20 * curViewUnitSize)
                                : section.TopShelfEndPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                        };
                        grips.Add(reverseGrip);
                        reverseGrip = new SectionReverseGrip(section)
                        {
                            GripPoint = section.EntityDirection == EntityDirection.LeftToRight
                                ? section.BottomShelfEndPoint - (Vector3d.XAxis * 20 * curViewUnitSize)
                                : section.BottomShelfEndPoint + (Vector3d.XAxis * 20 * curViewUnitSize)
                        };
                        grips.Add(reverseGrip);

                        #endregion

                        #region Text grips

                        if (section.TopDesignationPoint != Point3d.Origin && section.HasTextValue())
                        {
                            var textGrip = new SectionTextGrip(section)
                            {
                                GripPoint = section.TopDesignationPoint,
                                Name = TextGripName.TopText
                            };
                            grips.Add(textGrip);
                        }

                        if (section.BottomDesignationPoint != Point3d.Origin && section.HasTextValue())
                        {
                            var textGrip = new SectionTextGrip(section)
                            {
                                GripPoint = section.BottomDesignationPoint,
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
                        if (gripData is SectionVertexGrip vertexGrip)
                        {
                            var section = vertexGrip.Section;

                            if (vertexGrip.GripIndex == 0)
                            {
                                ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                                section.InsertionPoint = vertexGrip.GripPoint + offset;
                            }
                            else if (vertexGrip.GripIndex == section.MiddlePoints.Count + 1)
                            {
                                section.EndPoint = vertexGrip.GripPoint + offset;
                            }
                            else
                            {
                                section.MiddlePoints[vertexGrip.GripIndex - 1] =
                                    vertexGrip.GripPoint + offset;
                            }

                            // Вот тут происходит перерисовка примитивов внутри блока
                            section.UpdateEntities();
                            section.BlockRecord.UpdateAnonymousBlocks();
                        }
                        else if (gripData is SectionTextGrip textGrip)
                        {
                            var section = textGrip.Section;
                            if (textGrip.Name == TextGripName.TopText)
                            {
                                var topStrokeVector = section.MiddlePoints.Any()
                                    ? (section.InsertionPoint - section.MiddlePoints.First()).GetNormal()
                                    : (section.InsertionPoint - section.EndPoint).GetNormal();
                                var topShelfVector = topStrokeVector.GetPerpendicularVector().Negate();
                                var deltaY = topStrokeVector.DotProduct(offset) / section.BlockTransform.GetScale();
                                var deltaX = topShelfVector.DotProduct(offset) / section.BlockTransform.GetScale();
                                if (double.IsNaN(textGrip.CachedAlongTopShelfTextOffset))
                                {
                                    section.AlongTopShelfTextOffset = deltaX;
                                }
                                else
                                {
                                    section.AlongTopShelfTextOffset = textGrip.CachedAlongTopShelfTextOffset + deltaX;
                                }

                                if (double.IsNaN(textGrip.CachedAcrossTopShelfTextOffset))
                                {
                                    section.AcrossTopShelfTextOffset = deltaY;
                                }
                                else
                                {
                                    section.AcrossTopShelfTextOffset = textGrip.CachedAcrossTopShelfTextOffset + deltaY;
                                }

                                if (MainSettings.Instance.SectionDependentTextMovement)
                                {
                                    if (double.IsNaN(textGrip.CachedAlongBottomShelfTextOffset))
                                    {
                                        section.AlongBottomShelfTextOffset = deltaX;
                                    }
                                    else
                                    {
                                        section.AlongBottomShelfTextOffset = textGrip.CachedAlongBottomShelfTextOffset + deltaX;
                                    }

                                    if (double.IsNaN(textGrip.CachedAcrossBottomShelfTextOffset))
                                    {
                                        section.AcrossBottomShelfTextOffset = deltaY;
                                    }
                                    else
                                    {
                                        section.AcrossBottomShelfTextOffset = textGrip.CachedAcrossBottomShelfTextOffset + deltaY;
                                    }
                                }
                            }

                            if (textGrip.Name == TextGripName.BottomText)
                            {
                                var bottomStrokeVector = section.MiddlePoints.Any()
                                    ? (section.EndPoint - section.MiddlePoints.Last()).GetNormal()
                                    : (section.EndPoint - section.InsertionPoint).GetNormal();
                                var bottomShelfVector = bottomStrokeVector.GetPerpendicularVector();
                                var deltaY = bottomStrokeVector.DotProduct(offset) / section.BlockTransform.GetScale();
                                var deltaX = bottomShelfVector.DotProduct(offset) / section.BlockTransform.GetScale();

                                if (double.IsNaN(textGrip.CachedAlongBottomShelfTextOffset))
                                {
                                    section.AlongBottomShelfTextOffset = deltaX;
                                }
                                else
                                {
                                    section.AlongBottomShelfTextOffset = textGrip.CachedAlongBottomShelfTextOffset + deltaX;
                                }

                                if (double.IsNaN(textGrip.CachedAcrossBottomShelfTextOffset))
                                {
                                    section.AcrossBottomShelfTextOffset = deltaY;
                                }
                                else
                                {
                                    section.AcrossBottomShelfTextOffset = textGrip.CachedAcrossBottomShelfTextOffset + deltaY;
                                }

                                if (MainSettings.Instance.SectionDependentTextMovement)
                                {
                                    if (double.IsNaN(textGrip.CachedAlongTopShelfTextOffset))
                                    {
                                        section.AlongTopShelfTextOffset = deltaX;
                                    }
                                    else
                                    {
                                        section.AlongTopShelfTextOffset = textGrip.CachedAlongTopShelfTextOffset + deltaX;
                                    }

                                    if (double.IsNaN(textGrip.CachedAcrossTopShelfTextOffset))
                                    {
                                        section.AcrossTopShelfTextOffset = deltaY;
                                    }
                                    else
                                    {
                                        section.AcrossTopShelfTextOffset = textGrip.CachedAcrossTopShelfTextOffset + deltaY;
                                    }
                                }
                            }

                            section.UpdateEntities();
                            section.BlockRecord.UpdateAnonymousBlocks();
                        }
                        else if (gripData is SectionAddVertexGrip addVertexGrip)
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
