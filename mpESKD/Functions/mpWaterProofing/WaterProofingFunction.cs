namespace mpESKD.Functions.mpWaterProofing
{
    using System.Linq;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Enums;
    using Base.Styles;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Overrules;

    /// <inheritdoc />
    public class WaterProofingFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingGripPointOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingOsnapOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingObjectOverrule.Instance(), true);
        }

        /// <inheritdoc />
        public void Terminate()
        {
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingGripPointOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingOsnapOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingObjectOverrule.Instance());
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
#if !DEBUG
            Statistic.SendCommandStarting(WaterProofingDescriptor.Instance.Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif

            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(WaterProofingDescriptor.Instance.Name);

                var waterProofing = new WaterProofing();
                var blockReference = MainFunction.CreateBlock(waterProofing);

                waterProofing.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                InsertWaterProofingWithJig(waterProofing, blockReference);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                Overrule.Overruling = true;
            }
        }

        /// <summary>
        /// Команда создания линии грунта
        /// </summary>
        [CommandMethod("ModPlus", "mpWaterProofing", CommandFlags.Modal)]
        public void CreateWaterProofingCommand()
        {
            CreateWaterProofing();
        }

        /// <summary>
        /// Команда создания линия грунта из полилинии
        /// </summary>
        [CommandMethod("ModPlus", "mpWaterProofingFromPolyline", CommandFlags.Modal)]
        public void CreateWaterProofingFromPolylineCommand()
        {
            CreateWaterProofingFromPolyline();
        }

        private void CreateWaterProofing()
        {
#if !DEBUG
            Statistic.SendCommandStarting(WaterProofingDescriptor.Instance.Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(WaterProofingDescriptor.Instance.Name);

                var style = StyleManager.GetCurrentStyle(typeof(WaterProofing));
                var waterProofing = new WaterProofing();

                var blockReference = MainFunction.CreateBlock(waterProofing);
                waterProofing.ApplyStyle(style, true);

                InsertWaterProofingWithJig(waterProofing, blockReference);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                Overrule.Overruling = true;
            }
        }

        private static void InsertWaterProofingWithJig(WaterProofing waterProofing, BlockReference blockReference)
        {
            var nextPointPrompt = Language.GetItem(Invariables.LangItem, "msg5");
            var entityJig = new DefaultEntityJig(
                waterProofing,
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
                        entityJig.PreviousPoint = waterProofing.MiddlePoints.Any()
                            ? waterProofing.MiddlePoints.Last()
                            : waterProofing.InsertionPoint;
                    }
                    else
                    {
                        waterProofing.RebasePoints();
                        entityJig.PreviousPoint = waterProofing.MiddlePoints.Last();
                    }
                }
                else
                {
                    if (waterProofing.MiddlePoints.Any())
                    {
                        waterProofing.EndPoint = waterProofing.MiddlePoints.Last();
                        waterProofing.MiddlePoints.RemoveAt(waterProofing.MiddlePoints.Count - 1);
                        waterProofing.UpdateEntities();
                        waterProofing.BlockRecord.UpdateAnonymousBlocks();
                    }
                    else
                    {
                        // if no middle points - remove entity
                        using (AcadUtils.Document.LockDocument())
                        {
                            using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
                            {
                                var obj = (BlockReference)tr.GetObject(blockReference.Id, OpenMode.ForWrite, true, true);
                                obj.Erase(true);
                                tr.Commit();
                            }
                        }
                    }

                    break;
                }
            }
            while (true);

            if (!waterProofing.BlockId.IsErased)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(waterProofing.BlockId, OpenMode.ForWrite, true, true);
                    ent.XData = waterProofing.GetDataForXData();
                    tr.Commit();
                }
            }
        }

        private void CreateWaterProofingFromPolyline()
        {
#if !DEBUG
            Statistic.SendCommandStarting("mpWaterProofingFromPolyline", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                var peo = new PromptEntityOptions($"\n{Language.GetItem(Invariables.LangItem, "msg6")}")
                {
                    AllowNone = false,
                    AllowObjectOnLockedLayer = true
                };
                peo.SetRejectMessage($"\n{Language.GetItem(Invariables.LangItem, "wrong")}");
                peo.AddAllowedClass(typeof(Polyline), true);

                var per = AcadUtils.Editor.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                {
                    return;
                }

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(WaterProofingDescriptor.Instance.Name);

                // style
                var style = StyleManager.GetCurrentStyle(typeof(WaterProofing));
                var waterProofing = new WaterProofing();

                MainFunction.CreateBlock(waterProofing);
                waterProofing.ApplyStyle(style, true);

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
                                    waterProofing.InsertionPoint = pline.GetPoint3dAt(i);
                                }
                                else if (i == pline.NumberOfVertices - 1)
                                {
                                    waterProofing.EndPoint = pline.GetPoint3dAt(i);
                                }
                                else
                                {
                                    waterProofing.MiddlePoints.Add(pline.GetPoint3dAt(i));
                                }
                            }

                            waterProofing.UpdateEntities();
                            waterProofing.BlockRecord.UpdateAnonymousBlocks();

                            var ent = (BlockReference)tr.GetObject(waterProofing.BlockId, OpenMode.ForWrite, true, true);
                            ent.Position = pline.GetPoint3dAt(0);
                            ent.XData = waterProofing.GetDataForXData();
                        }

                        tr.Commit();
                    }

                    AcadUtils.Document.TransactionManager.QueueForGraphicsFlush();
                    AcadUtils.Document.TransactionManager.FlushGraphics();

                    // "Удалить исходную полилинию?"
                    if (MessageBox.ShowYesNo(Language.GetItem(Invariables.LangItem, "msg7"), MessageBoxIcon.Question))
                    {
                        using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
                        {
                            var dbObj = tr.GetObject(plineId, OpenMode.ForWrite, true, true);
                            dbObj.Erase(true);
                            tr.Commit();
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
    }
}
