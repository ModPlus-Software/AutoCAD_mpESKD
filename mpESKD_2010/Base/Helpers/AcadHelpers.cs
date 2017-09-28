﻿#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace mpESKD.Base.Helpers
{
    public static class AcadHelpers
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
        /// <summary>
        /// Получение аннотативного масштаба по имени из текущего чертежа
        /// </summary>
        /// <param name="name">Имя масштаба</param>
        /// <returns>Аннотативный масштаб с таким именем или текущий масштаб в БД</returns>
        public static AnnotationScale GetAnnotationScaleByName(string name)
        {
            var ocm = Database.ObjectContextManager;
            if (ocm != null)
            {
                var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                if(occ != null)
                foreach (var objectContext in occ)
                {
                    var asc = objectContext as AnnotationScale;
                    if (asc?.Name == name) return asc;
                }
            }
            return Database.Cannoscale;
        }
    }
    /// <summary>
    /// Вспомогательные методы работы с расширенными данными
    /// Есть аналогичные в MpCadHelpers. Некоторые будут совпадать
    /// но все-равно делаю отдельно
    /// </summary>
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
        /// <returns></returns>
        public static bool IsApplicable(RXObject rxObject, string appName)
        {
            DBObject dbObject = rxObject as DBObject;
            if (dbObject == null) return false;
            return IsMPCOentity(dbObject, appName);
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
