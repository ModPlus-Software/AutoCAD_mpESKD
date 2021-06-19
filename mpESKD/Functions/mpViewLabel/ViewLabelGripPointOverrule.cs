namespace mpESKD.Functions.mpViewLabel
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
    public class ViewLabelGripPointOverrule : BaseSmartEntityGripOverrule<mpSection.Section>
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

                    var section = EntityReaderService.Instance.GetFromEntity<ViewLabel>(entity);
                    if (section != null)
                    {
                        // insertion (start) grip
                        var vertexGrip = new ViewLabelVertexGrip(section, 0)
                        {
                            GripPoint = section.InsertionPoint
                        };
                        grips.Add(vertexGrip);

                        // middle points
                        for (var index = 0; index < section.MiddlePoints.Count; index++)
                        {
                            vertexGrip = new ViewLabelVertexGrip(section, index + 1)
                            {
                                GripPoint = section.MiddlePoints[index]
                            };
                            grips.Add(vertexGrip);

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
                        if (gripData is ViewLabelVertexGrip vertexGrip)
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
