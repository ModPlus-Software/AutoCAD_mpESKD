namespace mpESKD.Functions.mpView.Grips;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Base.Enums;
using Base.Overrules;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using View = mpView.View;

/// <summary>
/// Ручка позиции текста
/// </summary>
public class ViewTextGrip : SmartEntityGripData
{
    public ViewTextGrip(View view)
    {
        View = view;
        GripType = GripType.Point;
        CachedAlongTopShelfTextOffset = view.AlongTopShelfTextOffset;
        CachedAcrossTopShelfTextOffset = view.AcrossTopShelfTextOffset;
    }

    /// <summary>
    /// Экземпляр класса View
    /// </summary>
    public View View { get; }

    /// <summary>
    /// Имя ручки, чтобы определить от какого она текста
    /// </summary>
    public string TextGripName { get; set; }

    public double CachedAlongTopShelfTextOffset { get; }

    public double CachedAcrossTopShelfTextOffset { get; }

    public override string GetTooltip()
    {
        return Language.GetItem("gp1"); // stretch
    }

    public override void OnGripStatusChanged(ObjectId entityId, Status newStatus)
    {
        try
        {
            // При удачном перемещении ручки записываем новые значения в расширенные данные
            // По этим данным я потом получаю экземпляр класса view
            if (newStatus == Status.GripEnd)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blkRef = tr.GetObject(View.BlockId, OpenMode.ForWrite, true, true);
                    using (var resBuf = View.GetDataForXData())
                    {
                        blkRef.XData = resBuf;
                    }

                    tr.Commit();
                }

                View.Dispose();
            }

            // При отмене перемещения возвращаем временные значения
            if (newStatus == Status.GripAbort)
            {
                View.AlongTopShelfTextOffset = CachedAlongTopShelfTextOffset;
                View.AcrossTopShelfTextOffset = CachedAcrossTopShelfTextOffset;
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