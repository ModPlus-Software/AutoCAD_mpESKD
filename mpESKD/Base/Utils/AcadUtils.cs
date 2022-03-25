namespace mpESKD.Base.Utils;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using ModPlusAPI;
using ModPlusAPI.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

/// <summary>
/// Различные утилиты работы с AutoCAD
/// </summary>
public static class AcadUtils
{
    /// <summary>
    /// БД активного документа
    /// </summary>
    public static Database Database => HostApplicationServices.WorkingDatabase;

    /// <summary>
    /// Коллекция документов
    /// </summary>
    public static DocumentCollection Documents => AcApp.DocumentManager;

    /// <summary>
    /// Активный документ
    /// </summary>
    public static Document Document => AcApp.DocumentManager.MdiActiveDocument;

    /// <summary>
    /// Редактор активного документа
    /// </summary>
    public static Editor Editor => AcApp.DocumentManager.MdiActiveDocument.Editor;

    /// <summary>
    /// Возвращает коллекцию аннотационных масштабов текущей БД чертежа
    /// </summary>
    public static ObjectContextCollection ObjectContextCollection
    {
        get
        {
            var ocm = Database.ObjectContextManager;
            return ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
        }
    }

    /// <summary>
    /// Список слоев текущей базы данных
    /// </summary>
    public static List<string> Layers
    {
        get
        {
            var layers = new List<string>();
            using (Document.LockDocument())
            {
                using (var tr = Database.TransactionManager.StartOpenCloseTransaction())
                {
                    if (tr.GetObject(Database.LayerTableId, OpenMode.ForRead) is LayerTable lt)
                    {
                        foreach (var layerId in lt)
                        {
                            try
                            {
                                if (tr.GetObject(layerId, OpenMode.ForRead) is LayerTableRecord { IsErased: false, IsEraseStatusToggled: false } layer)
                                {
                                    layers.Add(layer.Name);
                                }
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }
                }
            }

            return layers;
        }
    }

    /// <summary>
    /// Список масштабов текущего чертежа
    /// </summary>
    public static List<string> Scales
    {
        get
        {
            var scales = new List<string>();
            var ocm = Database.ObjectContextManager;
            if (ocm != null)
            {
                var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                foreach (var objectContext in occ)
                {
                    scales.Add(((AnnotationScale)objectContext).Name);
                }
            }

            return scales;
        }
    }

    /// <summary>
    /// Текущий аннотативный масштаб
    /// </summary>
    public static AnnotationScale GetCurrentScale()
    {
        return ObjectContextCollection.CurrentContext as AnnotationScale;
    }

    /// <summary>
    /// Текстовые стили текущего чертежа
    /// </summary>
    public static List<string> TextStyles
    {
        get
        {
            var textStyles = new List<string>();
            using (Document.LockDocument())
            {
                using (var tr = Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var textStyleTable = (TextStyleTable)tr.GetObject(Database.TextStyleTableId, OpenMode.ForRead);
                    foreach (var objectId in textStyleTable)
                    {
                        var txtStl = (TextStyleTableRecord)tr.GetObject(objectId, OpenMode.ForRead);
                        if (!textStyles.Contains(txtStl.Name))
                        {
                            textStyles.Add(txtStl.Name);
                        }
                    }
                }
            }

            return textStyles;
        }
    }

    /// <summary>
    /// Открыть объект для чтения
    /// </summary>
    /// <typeparam name="T">Тип объекта</typeparam>
    /// <param name="objectId">Идентификатор примитива</param>
    /// <param name="openErased">Открыть с меткой "Стерто"</param>
    /// <param name="forceOpenOnLockedLayer">Открыть объект на заблокированном слое</param>
    public static T Read<T>(this ObjectId objectId, bool openErased = true, bool forceOpenOnLockedLayer = true)
        where T : DBObject
    {
        return objectId.GetObject(0, openErased, forceOpenOnLockedLayer) as T;
    }

    /// <summary>
    /// Открыть объект для записи
    /// </summary>
    /// <typeparam name="T">Тип объекта</typeparam>
    /// <param name="objectId">Идентификатор примитива</param>
    /// <param name="openErased">Открыть с меткой "Стерто"</param>
    /// <param name="forceOpenOnLockedLayer">Открыть объект на заблокированном слое</param>
    public static T Write<T>(this ObjectId objectId, bool openErased = true, bool forceOpenOnLockedLayer = true)
        where T : DBObject
    {
        return objectId.GetObject(OpenMode.ForWrite, openErased, forceOpenOnLockedLayer) as T;
    }

    /// <summary>
    /// Получение аннотативного масштаба по имени из текущего чертежа
    /// </summary>
    /// <param name="name">Имя масштаба</param>
    /// <returns>Аннотативный масштаб с таким именем или текущий масштаб в БД</returns>
    public static AnnotationScale GetAnnotationScaleByName(string name)
    {
        var ocm = Database.ObjectContextManager;
        if (ocm != null && !string.IsNullOrEmpty(name))
        {
            var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
            if (occ != null)
            {
                foreach (var objectContext in occ)
                {
                    var asc = objectContext as AnnotationScale;
                    if (asc?.Name == name)
                    {
                        return asc;
                    }
                }
            }
        }

        return Database.Cannoscale;
    }

    /// <summary>
    /// Возвращает численный масштаб (отношение единиц чертежа к единицам листа)
    /// </summary>
    /// <param name="scale">Аннотативный масштаб</param>
    public static double GetNumericScale(this AnnotationScale scale)
    {
        return scale.DrawingUnits / scale.PaperUnits;
    }

    /// <summary>Применение стиля к блоку согласно стиля</summary>
    /// <param name="blkRefObjectId">ObjectId of block reference</param>
    /// <param name="layerName">Имя слоя</param>
    /// <param name="layerXmlData">Данные слоя</param>
    public static void SetLayerByName(ObjectId blkRefObjectId, string layerName, XElement layerXmlData)
    {
        if (blkRefObjectId == ObjectId.Null)
        {
            return;
        }

        if (!layerName.Equals(Language.GetItem("defl"))) //// "По умолчанию"
        {
            if (LayerUtils.HasLayer(layerName))
            {
                using (Document.LockDocument())
                {
                    using (var tr = Database.TransactionManager.StartTransaction())
                    {
                        var blockReference = tr.GetObject(blkRefObjectId, OpenMode.ForWrite, true, true) as BlockReference;
                        if (blockReference != null)
                        {
                            blockReference.Layer = layerName;
                        }

                        tr.Commit();
                    }
                }
            }
            else
            {
                if (MainSettings.Instance.IfNoLayer == 1)
                {
                    if (LayerUtils.AddLayerFromXElement(layerXmlData))
                    {
                        using (Document.LockDocument())
                        {
                            using (var tr = Database.TransactionManager.StartTransaction())
                            {
                                var blockReference =
                                    tr.GetObject(blkRefObjectId, OpenMode.ForWrite, true, true) as BlockReference;
                                if (blockReference != null)
                                {
                                    blockReference.Layer = layerName;
                                }

                                tr.Commit();
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (Database.Clayer != ObjectId.Null && blkRefObjectId != ObjectId.Null)
            {
                using (var tr = Database.TransactionManager.StartTransaction())
                {
                    var blockReference = tr.GetObject(blkRefObjectId, OpenMode.ForWrite, true, true) as BlockReference;
                    var layer = tr.GetObject(Database.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    if (blockReference != null)
                    {
                        blockReference.Layer = layer?.Name;
                    }

                    tr.Commit();
                }
            }
        }
    }

    /// <summary>
    /// Написать сообщение в командную строку при сборке в Debug
    /// </summary>
    /// <param name="message">Сообщение</param>
    public static void WriteMessageInDebug(string message)
    {
#if DEBUG
        Editor?.WriteMessage($"\n{message}");
#endif
    }

    /// <summary>Получить имя типа линии по ObjectId</summary>
    /// <param name="ltid">ObjectId</param>
    /// <returns></returns>
    public static string GetLineTypeName(ObjectId ltid)
    {
        var lt = "Continuous";
        if (ltid == ObjectId.Null)
        {
            return lt;
        }

        using (Document.LockDocument())
        {
            using (var tr = Database.TransactionManager.StartTransaction())
            {
                var linetype = tr.GetObject(ltid, OpenMode.ForRead) as LinetypeTableRecord;
                lt = linetype?.Name;
                tr.Commit();
            }
        }

        return lt;
    }

    /// <summary>Получить ObjectId типа линии по имени в текущем документе</summary>
    /// <param name="lineTypeName">Имя типа линии</param>
    /// <returns></returns>
    public static ObjectId GetLineTypeObjectId(string lineTypeName)
    {
        var lineTypeObjectId = ObjectId.Null;
        if (string.IsNullOrEmpty(lineTypeName))
        {
            return ObjectId.Null;
        }

        using (Document.LockDocument())
        {
            using (var tr = Database.TransactionManager.StartTransaction())
            {
                var linetypeTable = tr.GetObject(Database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                if (linetypeTable != null)
                {
                    if (linetypeTable.Has(lineTypeName))
                    {
                        lineTypeObjectId = linetypeTable[lineTypeName];
                    }
                    else
                    {
                        const string filename = "acad.lin";
                        try
                        {
                            var path = HostApplicationServices.Current.FindFile(
                                filename, Database, FindFileHint.Default);
                            Database.LoadLineTypeFile(lineTypeName, path);
                            lineTypeObjectId = linetypeTable[lineTypeName];
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception exception)
                        {
                            if (exception.ErrorStatus == ErrorStatus.FilerError)
                            {
                                // Не удалось найти файл
                                MessageBox.Show(
                                    $"{Language.GetItem("err1")}: {filename}",
                                    MessageBoxIcon.Close);
                            }
                            else if (exception.ErrorStatus == ErrorStatus.DuplicateRecordName)
                            {
                                // ignore
                            }
                            else
                            {
                                // Не удалось загрузить тип линий
                                MessageBox.Show(
                                    $"{Language.GetItem("err2")}: {lineTypeName}", 
                                    MessageBoxIcon.Close);
                            }
                        }
                    }
                }

                tr.Commit();
            }
        }

        return lineTypeObjectId;
    }

    /// <summary>
    /// Установка типа линии для блока согласно стиля
    /// </summary>
    /// <param name="blkRefObjectId">Идентификатор блока</param>
    /// <param name="lineTypeName">Имя типа линии</param>
    public static void SetLineType(ObjectId blkRefObjectId, string lineTypeName)
    {
        if (blkRefObjectId == ObjectId.Null)
        {
            return;
        }

        using (Document.LockDocument())
        {
            using (var tr = Document.TransactionManager.StartTransaction())
            {
                var blockReference = tr.GetObject(blkRefObjectId, OpenMode.ForWrite, true, true) as BlockReference;
                if (blockReference != null)
                {
                    if (HasLineType(lineTypeName, tr))
                    {
                        blockReference.Linetype = lineTypeName;
                    }
                    else
                    {
                        if (LoadLineType(lineTypeName))
                        {
                            blockReference.Linetype = lineTypeName;
                        }
                    }
                }

                tr.Commit();
            }
        }
    }

    /// <summary>Проверка наличия типа линии в документе</summary>
    private static bool HasLineType(string lineTypeName, Transaction tr)
    {
        var lineTypeTable = tr.GetObject(Database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
        if (lineTypeTable != null)
        {
            return lineTypeTable.Has(lineTypeName);
        }

        return false;
    }

    /// <summary>Загрузка типа линии в файл</summary>
    /// <param name="lineTypeName">Имя типа линии</param>
    /// <param name="fileNames">Файлы, в которых искать тип линии. Если не указаны, то из стандартного</param>
    public static bool LoadLineType(string lineTypeName, List<string> fileNames = null)
    {
        var loaded = false;
        if (fileNames != null)
        {
            foreach (var fileName in fileNames)
            {
                try
                {
                    Database.LoadLineTypeFile(lineTypeName, fileName);
                    loaded = true;
                    break;
                }
                catch
                {
                    // ignore
                }
            }
        }

        if (!loaded)
        {
            try
            {
                Database.LoadLineTypeFile(lineTypeName, "acad.lin");
                loaded = true;
            }
            catch
            {
                // ignore
            }
        }

        return loaded;
    }

    /// <summary>
    /// Возвращает идентификатор текстового стиля по имени
    /// </summary>
    /// <param name="textStyleName">Имя текстового стиля</param>
    /// <returns><see cref="ObjectId"/></returns>
    public static ObjectId GetTextStyleIdByName(string textStyleName)
    {
        using (Document.LockDocument())
        {
            using (var tr = Database.TransactionManager.StartOpenCloseTransaction())
            {
                var textStyleTable = (TextStyleTable)tr.GetObject(Database.TextStyleTableId, OpenMode.ForRead);
                foreach (var objectId in textStyleTable)
                {
                    var txtStl = (TextStyleTableRecord)tr.GetObject(objectId, OpenMode.ForRead);
                    if (txtStl.Name.Equals(textStyleName))
                    {
                        return objectId;
                    }
                }
            }
        }

        return ObjectId.Null;
    }

    /// <summary>
    /// Найти все блоки в текущем пространстве (модель/лист), представляющие интеллектуальный примитив указанного типа
    /// </summary>
    /// <typeparam name="T">Допустимый тип</typeparam>
    /// <param name="entityType">Тип интеллектуального объекта для поиска</param>
    public static List<T> GetAllIntellectualEntitiesInCurrentSpace<T>(Type entityType) 
        where T : SmartEntity
    {
        var list = new List<T>();
        var appName = $"mp{entityType.Name}";
        using (var tr = Database.TransactionManager.StartOpenCloseTransaction())
        {
            var blockTableRecord = (BlockTableRecord)tr.GetObject(Database.CurrentSpaceId, OpenMode.ForRead);
            foreach (var objectId in blockTableRecord)
            {
                if (objectId == ObjectId.Null)
                {
                    continue;
                }

                if (tr.GetObject(objectId, OpenMode.ForRead) is BlockReference blockReference)
                {
                    var xData = blockReference.GetXDataForApplication(appName);
                    if (xData != null)
                    {
                        list.Add(EntityReaderService.Instance.GetFromEntity<T>(blockReference));
                    }
                }
            }

            tr.Commit();
        }

        return list;
    }

    /// <summary>
    /// Парсинг аннотативного масштаба из строки
    /// </summary>
    /// <param name="str">Строка с масштабом</param>
    public static AnnotationScale AnnotationScaleFromString(string str)
    {
        var defaultScale = new AnnotationScale { Name = "1:1", DrawingUnits = 1.0, PaperUnits = 1.0 };
        var splitted = str.Split(':');
        if (splitted.Length == 2)
        {
            var scale = new AnnotationScale
            {
                Name = str,
                PaperUnits = double.TryParse(splitted[0], out var d) ? d : 1.0,
                DrawingUnits = double.TryParse(splitted[1], out d) ? d : 1.0
            };
            return scale;
        }

        return defaultScale;
    }
}