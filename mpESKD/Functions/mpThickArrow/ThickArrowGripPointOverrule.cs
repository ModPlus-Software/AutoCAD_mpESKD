namespace mpESKD.Functions.mpThickArrow;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;

/// <inheritdoc />
public class ThickArrowGripPointOverrule : BaseSmartEntityGripOverrule<mpThickArrow.ThickArrow>
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

                var thickArrow = EntityReaderService.Instance.GetFromEntity<mpThickArrow.ThickArrow>(entity);
                if (thickArrow != null)
                {
                    //// insertion (start) grip
                    //var vertexGrip = new ThickArrowVertexGrip(thickArrow, 0)
                    //{
                    //    GripPoint = thickArrow.InsertionPoint
                    //};
                    //grips.Add(vertexGrip);



                    //vertexGrip = new ThickArrowVertexGrip(thickArrow, 1)
                    //{
                    //    GripPoint = thickArrow.TopShelfEndPoint
                    //};
                    //grips.Add(vertexGrip);

                    // Получаем первую ручку (совпадает с точкой вставки блока)
                    var gp = new ThickArrowVertexGrip(thickArrow, GripName.StartGrip)
                    {
                        GripPoint = thickArrow.InsertionPoint
                    };
                    grips.Add(gp);

                    // получаем среднюю ручку
                    gp = new ThickArrowVertexGrip(thickArrow, GripName.MiddleGrip)
                    {
                        GripPoint = thickArrow.MiddlePoint
                    };
                    grips.Add(gp);

                    // получаем конечную ручку
                    gp = new ThickArrowVertexGrip(thickArrow, GripName.EndGrip)
                    {
                        GripPoint = thickArrow.EndPoint
                    };
                    grips.Add(gp);


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
                    if (gripData is ThickArrowVertexGrip vertexGrip)
                    {
                        var thickArrow = vertexGrip.ThickArrow;

                        if (vertexGrip.GripIndex == 0)
                        {
                            ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                        }
                        else if (vertexGrip.GripIndex == 1)
                        {
                            thickArrow.EndPoint = vertexGrip.GripPoint + offset;
                        }

                        // Вот тут происходит перерисовка примитивов внутри блока
                        thickArrow.UpdateEntities();
                        thickArrow.BlockRecord.UpdateAnonymousBlocks();
                    }
                    /*else if (gripData is ViewTextGrip textGrip)
                    {
                        var view = textGrip.View;
                        var topShelfVector = (view.InsertionPoint - view.EndPoint).GetNormal();
                        var topStrokeVector = topShelfVector.GetPerpendicularVector().Negate();

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
                    }*/
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