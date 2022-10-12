namespace mpESKD.Functions.mpLevelPlanMark.Grips;

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
public class LevelPlanMarkLeaderMoveGrip : SmartEntityGripData
{
    /// <summary>
    /// Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/>
    /// </summary>
    public LevelPlanMark LevelPlanMark { get; }

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

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelPlanMarkVertexGrip"/> class.
    /// </summary>
    /// <param name="levelPlanMark">Экземпляр класса <see cref="mpLevelPlanMark.LevelPlanMark"/></param>
    /// <param name="gripIndex">Индекс ручки</param>
    public LevelPlanMarkLeaderMoveGrip(LevelPlanMark levelPlanMark, int gripIndex)
    {
        LevelPlanMark = levelPlanMark;
        GripIndex = gripIndex;
        GripType = GripType.Point;
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
                    var blkRef = tr.GetObject(LevelPlanMark.BlockId, OpenMode.ForWrite, true, true);
                    using (var resBuf = LevelPlanMark.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }

                LevelPlanMark.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                if (_gripTmp != null)
                {
                    if (GripIndex == 0)
                    {
                        LevelPlanMark.InsertionPoint = _gripTmp;
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