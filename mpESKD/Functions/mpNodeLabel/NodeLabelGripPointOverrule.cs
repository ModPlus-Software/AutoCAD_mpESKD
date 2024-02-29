namespace mpESKD.Functions.mpNodeLabel;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Grips;
using ModPlusAPI.Windows;
using mpESKD.Functions.mpNodeLabel.Grips;
using mpESKD.Functions.mpViewLabel.Grips;

public class NodeLabelGripPointOverrule : BaseSmartEntityGripOverrule<mpNodeLabel.NodeLabel>
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

                var nodeLabel = EntityReaderService.Instance.GetFromEntity<mpNodeLabel.NodeLabel>(entity);
                if (nodeLabel != null)
                {
                    // insertion (start) grip
                    var vertexGrip = new NodeLabelVertexGrip(nodeLabel, 0)
                    {
                        GripPoint = nodeLabel.InsertionPoint
                    };
                    grips.Add(vertexGrip);
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
                    if (!(gripData is NodeLabelVertexGrip vertexGrip))
                        continue;
                    var nodeLabel = vertexGrip.NodeLabel;

                    if (vertexGrip.GripIndex == 0)
                    {
                        ((BlockReference)entity).Position = vertexGrip.GripPoint + offset;
                        nodeLabel.InsertionPoint = vertexGrip.GripPoint + offset;
                    }

                    // Вот тут происходит перерисовка примитивов внутри блока
                    nodeLabel.UpdateEntities();
                    nodeLabel.BlockRecord.UpdateAnonymousBlocks();
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