﻿#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using mpESKD.Base.Properties;
using mpESKD.Base.Styles;

namespace mpESKD.Base.Helpers
{
    public static class AcadHelpers
    {
        /// <summary>БД активного документа</summary>
        public static Database Database => HostApplicationServices.WorkingDatabase;
        /// <summary>Коллекция документов</summary>
        public static DocumentCollection Documents => AcApp.DocumentManager;
        /// <summary>Активный документ</summary>
        public static Document Document => AcApp.DocumentManager.MdiActiveDocument;
        /// <summary>Редактор активного документа</summary>
        public static Editor Editor => AcApp.DocumentManager.MdiActiveDocument.Editor;
        /// <summary>Список слоёв текущей базы данных</summary>
        public static List<string> Layers
        {
            get
            {
                var layers = new List<string>();
                using (Document.LockDocument())
                {
                    using (OpenCloseTransaction tr = Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        LayerTable lt = tr.GetObject(Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                        if (lt != null)
                            foreach (ObjectId layerId in lt)
                            {
                                var layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                                if (layer != null)
                                    layers.Add(layer.Name);
                            }
                    }
                }
                return layers;
            }
        }
        /// <summary>Список масштабов текущего чертежа</summary>
        public static List<string> Scales
        {
            get
            {
                var scales = new List<string>();
                var ocm = Database.ObjectContextManager;
                if (ocm != null)
                {
                    var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                    foreach (ObjectContext objectContext in occ)
                    {
                        scales.Add(((AnnotationScale)objectContext).Name);
                    }
                }
                return scales;
            }
        }

        /// <summary>
        /// Открыть объект для чтения
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectId"></param>
        /// <param name="openErased"></param>
        /// <param name="forceOpenOnLockedLayer"></param>
        /// <returns></returns>
        public static T Read<T>(this ObjectId objectId, bool openErased = false, bool forceOpenOnLockedLayer = true)
            where T : DBObject
        {
            return (T)(objectId.GetObject(0, openErased, forceOpenOnLockedLayer) as T);
        }
        /// <summary>
        /// Открыть объект для записи
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectId"></param>
        /// <param name="openErased"></param>
        /// <param name="forceOpenOnLockedLayer"></param>
        /// <returns></returns>
        public static T Write<T>(this ObjectId objectId, bool openErased = false, bool forceOpenOnLockedLayer = true)
            where T : DBObject
        {
            return (T)(objectId.GetObject(OpenMode.ForWrite, openErased, forceOpenOnLockedLayer) as T);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="enities"></param>
        /// <returns></returns>
        public static ObjectId AddBlock(Point3d point, params Entity[] enities)
        {
            ObjectId objectId;
            BlockTableRecord blockTableRecord = new BlockTableRecord
            {
                Name = "*U"
            };

            Entity[] entityArray = enities;
            for (int i = 0; i < (int)entityArray.Length; i++)
            {
                blockTableRecord.AppendEntity(entityArray[i]);
            }
            using (Document.LockDocument())
            {
                using (Transaction tr = Database.TransactionManager.StartTransaction())
                {
                    using (BlockTable blockTable = Database.BlockTableId.Write<BlockTable>(false, true))
                    {
                        using (BlockReference blockReference = new BlockReference(point, blockTable.Add(blockTableRecord)))
                        {
                            ObjectId item = blockTable[BlockTableRecord.ModelSpace];//&&&&&&&&?????????????????????????????????????????????????paperspace
                            using (BlockTableRecord btr = item.Write<BlockTableRecord>(false, true))
                            {
                                objectId = btr.AppendEntity(blockReference);
                            }
                            tr.AddNewlyCreatedDBObject(blockReference, true);
                        }
                        tr.AddNewlyCreatedDBObject(blockTableRecord, true);
                    }
                    tr.Commit();
                }
            }
            return objectId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="enities"></param>
        /// <returns></returns>
        public static BlockReference GetBlockReference(Point3d point, IEnumerable<Entity> enities)
        {
            BlockTableRecord blockTableRecord = new BlockTableRecord
            {
                Name = "*U"
            };
            foreach (Entity enity in enities)
            {
                blockTableRecord.AppendEntity(enity);
            }
            return new BlockReference(point, blockTableRecord.ObjectId);
        }
        /// <summary>Получение аннотативного масштаба по имени из текущего чертежа</summary>
        /// <param name="name">Имя масштаба</param>
        /// <returns>Аннотативный масштаб с таким именем или текущий масштаб в БД</returns>
        public static AnnotationScale GetAnnotationScaleByName(string name)
        {
            var ocm = Database.ObjectContextManager;
            if (ocm != null)
            {
                var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                if (occ != null)
                    foreach (var objectContext in occ)
                    {
                        var asc = objectContext as AnnotationScale;
                        if (asc?.Name == name) return asc;
                    }
            }
            return Database.Cannoscale;
        }

        /// <summary>Применение стиля к блоку согласно стиля</summary>
        /// <param name="blkRefObjectId">ObjectId of block reference</param>
        /// <param name="layerName">Имя слоя</param>
        /// <param name="layerXmlData">Данные слоя</param>
        public static void SetLayerByName(ObjectId blkRefObjectId, string layerName, XElement layerXmlData)
        {
            var mainSettings = new MainSettings();
            if (mainSettings.UseLayerFromStyle)
            {
                if (!layerName.Equals("По умолчанию"))
                {
                    if (LayerHelper.HasLayer(layerName))
                    {
                        using (Document.LockDocument())
                        {
                            using (Transaction tr = Database.TransactionManager.StartTransaction())
                            {
                                var blockReference = tr.GetObject(blkRefObjectId, OpenMode.ForWrite) as BlockReference;
                                if (blockReference != null) blockReference.Layer = layerName;
                                tr.Commit();
                            }
                        }
                    }
                    else
                    {
                        if (mainSettings.IfNoLayer == 1)
                        {
                            if(LayerHelper.AddLayerFromXelement(layerXmlData))
                                using (Document.LockDocument())
                                {
                                    using (Transaction tr = Database.TransactionManager.StartTransaction())
                                    {
                                        var blockReference = tr.GetObject(blkRefObjectId, OpenMode.ForWrite) as BlockReference;
                                        if (blockReference != null) blockReference.Layer = layerName;
                                        tr.Commit();
                                    }
                                }
                        }
                    }
                }
            }
        }

        public static void WriteMessageInDebug(string message)
        {
#if DEBUG
            Editor.WriteMessage("\n" + message);
#endif
        }
    }
    /// <summary>Вспомогательные методы работы с расширенными данными
    /// Есть аналогичные в MpCadHelpers. Некоторые будут совпадать
    /// но все-равно делаю отдельно</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class ExtendedDataHelpers
    {
        /// <summary>
        /// Добавление регистрации приложения в соответсвующую таблицу чертежа
        /// </summary>
        public static void AddRegAppTableRecord(string appName)
        {
            using (var tr = AcadHelpers.Document.TransactionManager.StartTransaction())
            {
                RegAppTable rat = (RegAppTable)tr.GetObject(AcadHelpers.Database.RegAppTableId, OpenMode.ForRead, false);
                if (!rat.Has(appName))
                {
                    rat.UpgradeOpen();
                    RegAppTableRecord ratr = new RegAppTableRecord
                    {
                        Name = appName
                    };
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// Проверка поддерживаемости примитива для Overrule
        /// </summary>
        /// <param name="rxObject"></param>
        /// <param name="appName"></param>
        /// <param name="forPalette">Проверка для палитры</param>
        /// <returns></returns>
        public static bool IsApplicable(RXObject rxObject, string appName, bool forPalette = false)
        {
            DBObject dbObject = rxObject as DBObject;
            if (dbObject == null) return false;
            if (dbObject.IsAProxy || 
                dbObject.IsErased ||
                dbObject.IsEraseStatusToggled ||
                dbObject.IsUndoing) return false;
            // Если проверка для палитры, то проверяем по наличию расширенных данных
            // Для Overrule это не нужно
            return !forPalette || IsMPCOentity(dbObject, appName);
        }

        /// <summary>
        /// Проверка по XData вхождения блока, что он является любым СПДС примитивом
        /// </summary>
        /// <param name="blkRef">Вхождение блока</param>
        /// <param name="appName"></param>
        /// <returns></returns>
        public static bool IsMPCOentity(Entity blkRef, string appName)
        {
            ResultBuffer rb = blkRef.GetXDataForApplication(appName);
            return rb != null;
        }
        public static bool IsMPCOentity(DBObject dbObject, string appName)
        {
            ResultBuffer rb = dbObject.GetXDataForApplication(appName);
            return rb != null;
        }
    }
}
