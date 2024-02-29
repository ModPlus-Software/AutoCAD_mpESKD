namespace mpESKD.Functions.mpNodeLabel.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;

/// <summary>
/// Ручка вершин
/// </summary>
internal class NodeLabelVertexGrip : SmartEntityGripData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeLabelVertexGrip"/> class.
    /// </summary>
    /// <param name="nodeLabel">Экземпляр класса <see cref="mpNodeLabel.NodeLabel"/></param>
    /// <param name="index">Индекс ручки</param>
    public NodeLabelVertexGrip(NodeLabel nodeLabel, int index)
    {
        NodeLabel = nodeLabel;
        GripIndex = index;
        GripType = GripType.Point;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpNodeLabel.NodeLabel"/>
    /// </summary>
    public NodeLabel NodeLabel { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

    // Временное значение ручки
    private Point3d _gripTmp;

    /// <inheritdoc />
    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            // При начале перемещения запоминаем первоначальное положение ручки
            // Запоминаем начальные значения
            if (newStatus == Status.GripStart)
            {
                _gripTmp = GripPoint;
            }

            // При удачном перемещении ручки записываем новые значения в расширенные данные
            // По этим данным я потом получаю экземпляр класса section
            if (newStatus == Status.GripEnd)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(NodeLabel.BlockId, OpenMode.ForWrite, true, true);
                    using (var resBuf = NodeLabel.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }

                NodeLabel.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_gripTmp != null)
                {
                    if (GripIndex == 0)
                    {
                        NodeLabel.InsertionPoint = _gripTmp;
                    }
                }
            }

            base.OnGripStatusChanged(entityId, newStatus);
        }
        catch (Exception exception)
        {
            if (exception.ErrorStatus != ErrorStatus.NotAllowedForThisProxy)
                ExceptionBox.Show(exception);
        }
    }
}