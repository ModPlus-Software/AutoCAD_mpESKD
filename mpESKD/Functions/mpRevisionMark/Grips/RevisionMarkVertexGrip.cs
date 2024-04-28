namespace mpESKD.Functions.mpRevisionMark.Grips;

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
public class RevisionMarkVertexGrip : SmartEntityGripData
{
    // Временное значение ручки
    private Point3d _gripTmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevisionMarkVertexGrip"/> class.
    /// </summary>
    /// <param name="revisionMark">Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public RevisionMarkVertexGrip(RevisionMark revisionMark, int gripIndex)
    {
        RevisionMark = revisionMark;
        GripIndex = gripIndex;
        GripType = GripType.Point;
    }

    /// <summary>
    /// Экземпляр класса <see cref="mpRevisionMark.RevisionMark"/>
    /// </summary>
    public RevisionMark RevisionMark { get; }

    /// <summary>
    /// Индекс ручки
    /// </summary>
    public int GripIndex { get; }

    /// <inheritdoc />
    public override string GetTooltip()
    {
        return Language.GetItem("gp2"); // move
    }

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
            // По этим данным я потом получаю экземпляр класса
            if (newStatus == Status.GripEnd)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(RevisionMark.BlockId, OpenMode.ForWrite, true, true);
                    using (var resBuf = RevisionMark.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }

                RevisionMark.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_gripTmp != null)
                {
                    if (GripIndex == 0)
                    {
                        RevisionMark.InsertionPoint = _gripTmp;
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