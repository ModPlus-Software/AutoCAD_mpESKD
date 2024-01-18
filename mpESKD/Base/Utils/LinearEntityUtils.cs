namespace mpESKD.Base.Utils;

using System;
using System.Linq;
using Abstractions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Enums;
using ModPlusAPI;
using ModPlusAPI.Windows;
using Styles;

/// <summary>
/// Вспомогательные и обобщающие методы для работы с линейными интеллектуальными объектами
/// </summary>
public static class LinearEntityUtils
{
    /// <summary>
    /// Динамическая вставка линейного интеллектуального объекта
    /// </summary>
    /// <typeparam name="T">Тип линейного интеллектуального объекта</typeparam>
    /// <param name="linearEntity">Экземпляр линейного интеллектуального объекта</param>
    /// <param name="blockReference">Блок</param>
    public static void InsertWithJig<T>(T linearEntity, BlockReference blockReference)
        where T : SmartEntity, ILinearEntity
    {
        var nextPointPrompt = Language.GetItem("msg5");
        var entityJig = new DefaultEntityJig(
            linearEntity,
            blockReference,
            new Point3d(20, 0, 0));
        
        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;
            if (status == PromptStatus.OK)
            {
                entityJig.JigState = JigState.PromptNextPoint;
                entityJig.PromptForNextPoint = nextPointPrompt;
                if (entityJig.PreviousPoint == null)
                {
                    entityJig.PreviousPoint = linearEntity.MiddlePoints.Any()
                        ? linearEntity.MiddlePoints.Last()
                        : linearEntity.InsertionPoint;
                }
                else
                {
                    linearEntity.IsLightCreation = true;
                    linearEntity.RebasePoints();
                    entityJig.PreviousPoint = linearEntity.MiddlePoints.Last();
                }
            }
            else
            {
                linearEntity.IsLightCreation = false;
                if (linearEntity.MiddlePoints.Any())
                {
                    linearEntity.EndPoint = linearEntity.MiddlePoints.Last();
                    linearEntity.MiddlePoints.RemoveAt(linearEntity.MiddlePoints.Count - 1);
                    linearEntity.UpdateEntities();
                    linearEntity.BlockRecord.UpdateAnonymousBlocks();
                }
                else
                {
                    // if no middle points - remove entity
                    EntityUtils.Erase(blockReference.Id);
                }

                break;
            }
        }
        while (true);

        if (!linearEntity.BlockId.IsErased)
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(linearEntity.BlockId, OpenMode.ForWrite, true, true);
                ent.XData = linearEntity.GetDataForXData();
                tr.Commit();
            }
        }
    }

    /// <summary>
    /// Конвертирование выбранной полилинии в линейный интеллектуальный объект
    /// </summary>
    /// <typeparam name="T">Тип линейного интеллектуального объекта</typeparam>
    public static void CreateFromPolyline<T>()
        where T : SmartEntity, ILinearEntity, new()
    {
        try
        {
            var peo = new PromptEntityOptions($"\n{Language.GetItem("msg6")}")
            {
                AllowNone = false,
                AllowObjectOnLockedLayer = true
            };
            peo.SetRejectMessage($"\n{Language.GetItem("wrong")}");
            peo.AddAllowedClass(typeof(Polyline), true);

            var per = AcadUtils.Editor.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                return;
            }
                
            var style = StyleManager.GetCurrentStyle(typeof(T));
            var groundLine = new T();
                
            MainFunction.CreateBlock(groundLine);
            groundLine.ApplyStyle(style, true);

            var plineId = per.ObjectId;

            using (AcadUtils.Document.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, true))
            {
                using (var tr = AcadUtils.Document.TransactionManager.StartOpenCloseTransaction())
                {
                    var dbObj = tr.GetObject(plineId, OpenMode.ForRead);
                    if (dbObj is Polyline pline)
                    {
                        for (int i = 0; i < pline.NumberOfVertices; i++)
                        {
                            if (i == 0)
                            {
                                groundLine.InsertionPoint = pline.GetPoint3dAt(i);
                            }
                            else if (i == pline.NumberOfVertices - 1)
                            {
                                groundLine.EndPoint = pline.GetPoint3dAt(i);
                            }
                            else
                            {
                                groundLine.MiddlePoints.Add(pline.GetPoint3dAt(i));
                            }
                        }

                        groundLine.UpdateEntities();
                        groundLine.BlockRecord.UpdateAnonymousBlocks();

                        var ent = (BlockReference)tr.GetObject(groundLine.BlockId, OpenMode.ForWrite, true, true);
                        ent.Position = pline.GetPoint3dAt(0);
                        ent.XData = groundLine.GetDataForXData();
                    }

                    tr.Commit();
                }

                AcadUtils.Document.TransactionManager.QueueForGraphicsFlush();
                AcadUtils.Document.TransactionManager.FlushGraphics();

                // "Удалить исходную полилинию?"
                if (MessageBox.ShowYesNo(Language.GetItem("msg7"), MessageBoxIcon.Question))
                {
                    EntityUtils.Erase(plineId);
                }
            }
        }
        catch (Exception exception)
        {
            ExceptionBox.Show(exception);
        }
    }
}