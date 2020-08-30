namespace mpESKD.Functions.mpLevelMark
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
    using Exception = Autodesk.AutoCAD.Runtime.Exception;

    /// <inheritdoc />
    public class LevelMarkFunction : ISmartEntityFunction
    {
        /// <inheritdoc/>
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), LevelMarkGripPointOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), LevelMarkOsnapOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), LevelMarkObjectOverrule.Instance(), true);
        }

        /// <inheritdoc/>
        public void Terminate()
        {
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), LevelMarkGripPointOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), LevelMarkOsnapOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), LevelMarkObjectOverrule.Instance());
        }

        /// <inheritdoc/>
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
#if !DEBUG
            Statistic.SendCommandStarting(LevelMarkDescriptor.Instance.Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(LevelMarkDescriptor.Instance.Name);
                var levelMark = new LevelMark();

                var blockReference = MainFunction.CreateBlock(levelMark);

                levelMark.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                InsertLevelMarkWithJig(levelMark, blockReference);
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
        /// Команда создания отметки уровня
        /// </summary>
        [CommandMethod("ModPlus", "mpLevelMark", CommandFlags.Modal)]
        public void CreateLevelMarkCommand()
        {
            CreateLevelMark();
        }

        /// <summary>
        /// Команда выравнивания отметок уровня
        /// </summary>
        [CommandMethod("ModPlus", "mpLevelMarkAlign", CommandFlags.Modal)]
        public void AlignLevelMarks()
        {
#if !DEBUG
            Statistic.SendCommandStarting("mpLevelMarkAlign", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                var win = new LevelMarkAlignSetup();
                if (win.ShowDialog() != true)
                    return;

                var alignArrowPoints =
                    win.ChkAlignArrowPoints.IsChecked.HasValue && win.ChkAlignArrowPoints.IsChecked.Value;
                var alignBasePoints =
                    win.ChkAlignBasePoints.IsChecked.HasValue && win.ChkAlignBasePoints.IsChecked.Value;

                var pso = new PromptSelectionOptions
                {
                    // Выберите отметки уровня:
                    MessageForAdding = $"\n{Language.GetItem(Invariables.LangItem, "msg14")}",

                    // Убрать объекты из выбора:
                    MessageForRemoval = $"\n{Language.GetItem(Invariables.LangItem, "msg16")}",
                    AllowSubSelections = false,
                    AllowDuplicates = true,
                    RejectObjectsFromNonCurrentSpace = true,
                    RejectObjectsOnLockedLayers = true
                };

                var availTypedValues = new TypedValue[1];
                availTypedValues.SetValue(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, LevelMarkDescriptor.Instance.Name), 0);

                var filter = new SelectionFilter(availTypedValues);

                var selectionResult = AcadUtils.Editor.GetSelection(pso, filter);
                if (selectionResult.Status != PromptStatus.OK || selectionResult.Value.Count == 0)
                    return;

                var processMarksIds = selectionResult.Value.GetObjectIds();

                pso = new PromptSelectionOptions
                {
                    // Выберите эталонную отметку уровня:
                    MessageForAdding = $"\n{Language.GetItem(Invariables.LangItem, "msg15")}",

                    // Убрать объекты из выбора:
                    MessageForRemoval = $"\n{Language.GetItem(Invariables.LangItem, "msg16")}",
                    AllowSubSelections = false,
                    AllowDuplicates = true,
                    RejectObjectsFromNonCurrentSpace = true,
                    RejectObjectsOnLockedLayers = true,
                    SingleOnly = true
                };

                selectionResult = AcadUtils.Editor.GetSelection(pso, filter);
                if (selectionResult.Status != PromptStatus.OK || selectionResult.Value.Count == 0)
                    return;

                var referenceMarkId = selectionResult.Value.GetObjectIds().First();

                using (AcadUtils.Document.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, true))
                {
                    using (var tr = AcadUtils.Document.TransactionManager.StartOpenCloseTransaction())
                    {
                        var referenceMarkBlock = tr.GetObject(referenceMarkId, OpenMode.ForWrite);
                        var referenceMark = EntityReaderService.Instance.GetFromEntity<LevelMark>(referenceMarkBlock);
                        if (referenceMark == null)
                            return;

                        foreach (var processMarkId in processMarksIds)
                        {
                            if (processMarkId == referenceMarkId)
                                continue;
                            var processMarkBlock = tr.GetObject(processMarkId, OpenMode.ForWrite);
                            var processMark = EntityReaderService.Instance.GetFromEntity<LevelMark>(processMarkBlock);
                            if (processMark == null)
                                continue;

                            if (alignBasePoints)
                            {
                                ((BlockReference)processMarkBlock).Position = referenceMark.InsertionPoint;
                                processMark.InsertionPoint = referenceMark.InsertionPoint;
                            }

                            if (alignArrowPoints)
                            {
                                processMark.SetArrowPoint(new Point3d(
                                    referenceMark.EndPoint.X,
                                    processMark.EndPoint.Y,
                                    processMark.EndPoint.Z));
                            }

                            processMark.UpdateEntities();
                            processMark.BlockRecord.UpdateAnonymousBlocks();

                            processMarkBlock.XData = processMark.GetDataForXData();
                        }

                        tr.Commit();
                    }

                    AcadUtils.Document.TransactionManager.QueueForGraphicsFlush();
                    AcadUtils.Document.TransactionManager.FlushGraphics();
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void CreateLevelMark()
        {
#if !DEBUG
            Statistic.SendCommandStarting(LevelMarkDescriptor.Instance.Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(LevelMarkDescriptor.Instance.Name);
                var style = StyleManager.GetCurrentStyle(typeof(LevelMark));
                var levelMark = new LevelMark();

                var blockReference = MainFunction.CreateBlock(levelMark);
                levelMark.ApplyStyle(style, true);

                InsertLevelMarkWithJig(levelMark, blockReference);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
            finally
            {
                Overrule.Overruling = true;
            }
        }

        private static void InsertLevelMarkWithJig(LevelMark levelMark, BlockReference blockReference)
        {
            // <msg11>Укажите точку начала отсчета:</msg11>
            var basePointPrompt = Language.GetItem(Invariables.LangItem, "msg11");

            // <msg12>Укажите точку уровня:</msg12>
            var levelPointPrompt = Language.GetItem(Invariables.LangItem, "msg12");

            // <msg13>Укажите точку положения отметки уровня:</msg13>
            var levelMarkPositionPointPrompt = Language.GetItem(Invariables.LangItem, "msg13");

            var entityJig = new DefaultEntityJig(levelMark, blockReference, new Point3d(0, 0, 0))
            {
                PromptForInsertionPoint = basePointPrompt
            };

            levelMark.LevelMarkJigState = LevelMarkJigState.InsertionPoint;
            do
            {
                var status = AcadUtils.Editor.Drag(entityJig).Status;
                if (status == PromptStatus.OK)
                {
                    if (levelMark.LevelMarkJigState == LevelMarkJigState.InsertionPoint)
                    {
                        levelMark.LevelMarkJigState = LevelMarkJigState.ObjectPoint;
                        entityJig.PromptForNextPoint = levelPointPrompt;
                        entityJig.PreviousPoint = levelMark.InsertionPoint;
                    }
                    else if (levelMark.LevelMarkJigState == LevelMarkJigState.ObjectPoint)
                    {
                        levelMark.LevelMarkJigState = LevelMarkJigState.EndPoint;
                        entityJig.PromptForNextPoint = levelMarkPositionPointPrompt;
                        levelMark.ObjectPoint = levelMark.EndPoint;
                        entityJig.PreviousPoint = levelMark.ObjectPoint;
                    }
                    else
                    {
                        break;
                    }

                    entityJig.JigState = JigState.PromptNextPoint;
                }
                else
                {
                    // mark to remove
                    using (AcadUtils.Document.LockDocument())
                    {
                        using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
                        {
                            var obj = (BlockReference)tr.GetObject(blockReference.Id, OpenMode.ForWrite, true, true);
                            obj.Erase(true);
                            tr.Commit();
                        }
                    }

                    break;
                }
            }
            while (true);

            if (!levelMark.BlockId.IsErased)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(levelMark.BlockId, OpenMode.ForWrite, true, true);
                    ent.XData = levelMark.GetDataForXData();
                    tr.Commit();
                }
            }
        }
    }
}
