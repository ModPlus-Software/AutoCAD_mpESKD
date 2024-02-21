using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Office2010.ExcelAc;

namespace mpESKD.Base.Utils;

using System.Linq;
using Abstractions;
using Functions.SearchEntities;
using System.Diagnostics;

/// <summary>
/// Утилиты для <see cref="SmartEntity"/>
/// </summary>
public static class SmartEntityUtils
{
    /// <summary>
    /// Возвращает дескриптор <see cref="ISmartEntityDescriptor"/> для типа, унаследованного от <see cref="SmartEntity"/>
    /// </summary>
    /// <typeparam name="T">Тип, унаследованный от <see cref="SmartEntity"/></typeparam>
    public static ISmartEntityDescriptor GetDescriptor<T>()
        where T : SmartEntity
    {
        return TypeFactory.Instance.GetDescriptor(typeof(T));
    }

    /// <summary>
    /// Отправить статистику запуска команды создания интеллектуального объекта определенного типа
    /// </summary>
    /// <typeparam name="T">Тип, унаследованный от <see cref="SmartEntity"/></typeparam>
    public static void SendStatistic<T>()
        where T : SmartEntity
    {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(ModPlusConnector.Instance, GetDescriptor<T>().Name);
#endif
    }

    /// <summary>
    /// Принудительная перерисовка всех интеллектуальных объектов, найденных в текущем пространстве
    /// </summary>
    /// <remarks>
    /// Опция "Только на листах" нужна для решения проблемы, когда переключаются с модели на лист,
    /// но обновление интеллектуальных объектов не происходит
    /// </remarks>
    /// <param name="onlyInLayout">True - только на листах</param>
    public static void UpdateSmartObjects(bool onlyInLayout)
    {
        try
        {
            if (onlyInLayout && AcadUtils.IsInModel())
                return;

            var timer = Stopwatch.StartNew();
            using (var tr = AcadUtils.Document.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var blockReference in SearchEntitiesCommand.GetBlockReferencesOfSmartEntities(
                             TypeFactory.Instance.GetEntityCommandNames(), tr))
                {
                    var smartEntity = EntityReaderService.Instance.GetFromEntity(blockReference);
                    if (smartEntity != null)
                    {
                        blockReference.UpgradeOpen();
                        smartEntity.UpdateEntities();
                        smartEntity.GetBlockTableRecordForUndo(blockReference)?.UpdateAnonymousBlocks();
                    }
                }

                tr.Commit();
            }

            timer.Stop();
            Debug.Print($"Time for update entities: {timer.ElapsedMilliseconds} milliseconds");
        }
        catch (System.Exception exception)
        {
            Debug.Print(exception.Message);
        }
    }

    public static void UpdateTextualSmartObjects(bool onlyInLayout)
    {
        // Список типов интеллектуальных объектов, реализующих ITextValueEntity
        var typesToProceedNames = TypeFactory.Instance.GetEntityTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ITextValueEntity)))
            .Select(t => t.Name)
            .ToList();

        try
        {
            if (onlyInLayout && AcadUtils.IsInModel())
                return;

            using (var tr = AcadUtils.Document.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var blockReference in SearchEntitiesCommand.GetBlockReferencesOfSmartEntities(
                             TypeFactory.Instance.GetEntityCommandNames(), tr))
                {
                    var smartEntity = EntityReaderService.Instance.GetFromEntity(blockReference);
                    if (smartEntity != null)
                    {
                        if (typesToProceedNames.Contains(smartEntity.GetType().Name))
                        {
#if DEBUG
                            AcadUtils.WriteMessageInDebug($"Valid textual object: {smartEntity.GetType().Name}");
#endif
                            blockReference.UpgradeOpen();
                            smartEntity.UpdateEntities();
                            smartEntity.GetBlockTableRecordForUndo(blockReference)?.UpdateAnonymousBlocks();
                        }
                    }
                }

                tr.Commit();
            }
        }
        catch (System.Exception exception)
        {
            Debug.Print(exception.Message);
        }
    }

    private static void WriteToEd(List<string> values, string caption)
    {
        var str = $"{caption}: ";
        foreach (var value in values)
        {
            str += value + ", ";
        }
        AcadUtils.WriteMessageInDebug(str);
    }
}