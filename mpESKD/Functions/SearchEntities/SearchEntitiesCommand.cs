namespace mpESKD.Functions.SearchEntities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Enums;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;
using mpESKD.Base.Abstractions;

/// <summary>
/// Поиск интеллектуальных объектов в чертеже
/// </summary>
public static class SearchEntitiesCommand
{
    /// <summary>
    /// Запуск команды поиска интеллектуальных объектов в чертеже
    /// </summary>
    [CommandMethod("ModPlus", "mpESKDSearch", CommandFlags.Modal)]
    public static void Start()
    {
        try
        {
            var types = TypeFactory.Instance.GetEntityTypes();
            var settings = new SearchEntitiesSettings();

            foreach (var entityType in types)
            {
                var checkBox = new CheckBox
                {
                    Content = TypeFactory.Instance.GetDescriptor(entityType).LName,
                    Tag = entityType
                };
                var listBoxItem = new ListBoxItem
                {
                    Content = checkBox
                };
                settings.LbEntities.Items.Add(listBoxItem);
            }

            if (settings.ShowDialog() == false)
            {
                return;
            }

            var searchProceedOption = (SearchProceedOption)settings.CbSearchProceedOption.SelectedIndex;

            var entitiesToProceed = new List<string>();
            foreach (var item in settings.LbEntities.Items)
            {
                if (item is ListBoxItem { Content: CheckBox { IsChecked: true } checkBox })
                {
                    entitiesToProceed.Add($"mp{((Type)checkBox.Tag).Name}");
                }
            }

            if (!entitiesToProceed.Any())
            {
                return;
            }

            using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
            {
                var btr = (BlockTableRecord)tr.GetObject(AcadUtils.Database.CurrentSpaceId, OpenMode.ForRead);

                var blockReferences = GetBlockReferencesOfSmartEntities(entitiesToProceed, tr).ToList();

                if (blockReferences.Any())
                {
                    switch (searchProceedOption)
                    {
                        case SearchProceedOption.Update:
                            foreach (var blockReference in blockReferences)
                            {
                                var entity = EntityReaderService.Instance.GetFromEntity(blockReference);
                                if (entity != null)
                                {
                                    blockReference.UpgradeOpen();
                                    entity.UpdateEntities();
                                    entity.GetBlockTableRecordForUndo(blockReference)?.UpdateAnonymousBlocks();
                                }
                            }
                                
                            AcadUtils.Editor.Regen();
                                
                            break;
                            
                        case SearchProceedOption.Select:
                            AcadUtils.Editor.SetImpliedSelection(blockReferences.Select(b => b.ObjectId).ToArray());
                            break;
                            
                        case SearchProceedOption.RemoveData:
                            foreach (var blockReference in blockReferences)
                            {
                                blockReference.UpgradeOpen();
                                var typedValue = blockReference.XData.AsArray()
                                    .FirstOrDefault(tv => tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName);
                                blockReference.XData = new ResultBuffer(
                                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, typedValue.Value.ToString()));
                            }

                            MessageBox.Show($"{Language.GetItem("msg9")}: {blockReferences.Count}");
                            break;
                            
                        case SearchProceedOption.Explode:
                            btr.UpgradeOpen();
                            foreach (var blockReference in blockReferences)
                            {
                                blockReference.UpgradeOpen();
                                using (var dbObjCol = new DBObjectCollection())
                                {
                                    blockReference.Explode(dbObjCol);
                                    foreach (DBObject dbObj in dbObjCol)
                                    {
                                        var acEnt = dbObj as Entity;

                                        btr.AppendEntity(acEnt);
                                        tr.AddNewlyCreatedDBObject(dbObj, true);
                                    }
                                }

                                blockReference.Erase(true);
                            }

                            MessageBox.Show($"{Language.GetItem("msg9")}: {blockReferences.Count}");
                            break;
                            
                        case SearchProceedOption.Delete:
                            foreach (var blockReference in blockReferences)
                            {
                                blockReference.UpgradeOpen();
                                blockReference.Erase(true);
                            }

                            MessageBox.Show($"{Language.GetItem("msg9")}: {blockReferences.Count}");
                            break;
                    }
                }
                else
                {
                    MessageBox.Show(Language.GetItem("msg10"));
                }

                tr.Commit();
            }
        }
        catch (System.Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }

    /// <summary>
    /// Найти блоки в текущем пространстве, являющиеся интеллектуальными объектами
    /// </summary>
    /// <param name="commandNames">Список командных имен интеллектуальных объектов, которые нужно найти.
    /// Командные имена состоят из префикса mp и имени типа.
    /// Т.е. если тип называется "Axis", то команда должна называться "mpAxis"</param>
    /// <param name="tr">Открытая транзакция</param>
    /// <returns>Коллекция блоков</returns>
    public static IEnumerable<BlockReference> GetBlockReferencesOfSmartEntities(
        ICollection<string> commandNames, Transaction tr)
    {
        var btr = (BlockTableRecord)tr.GetObject(AcadUtils.Database.CurrentSpaceId, OpenMode.ForRead);
        foreach (var objectId in btr)
        {
            if (tr.GetObject(objectId, OpenMode.ForRead) is BlockReference blockReference &&
                blockReference.XData != null)
            {
                var typedValue = blockReference.XData.AsArray()
                    .FirstOrDefault(tv => tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName);
                if (commandNames.Contains(typedValue.Value as string))
                {
                    yield return blockReference;
                }
            }
        }
    }
}