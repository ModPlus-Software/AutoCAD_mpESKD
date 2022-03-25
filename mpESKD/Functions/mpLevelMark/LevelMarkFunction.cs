namespace mpESKD.Functions.mpLevelMark;

using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Abstractions;
using Base.Enums;
using Base.Overrules;
using Base.Styles;
using Base.Utils;
using ModPlusAPI;
using ModPlusAPI.Windows;

/// <inheritdoc />
public class LevelMarkFunction : ISmartEntityFunction
{
    /// <inheritdoc/>
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new LevelMarkGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<LevelMark>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<LevelMark>(), true);
    }

    /// <inheritdoc/>
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<LevelMark>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<LevelMark>();
                
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
                MessageForAdding = $"\n{Language.GetItem("msg14")}",

                // Убрать объекты из выбора:
                MessageForRemoval = $"\n{Language.GetItem("msg16")}",
                AllowSubSelections = false,
                AllowDuplicates = true,
                RejectObjectsFromNonCurrentSpace = true,
                RejectObjectsOnLockedLayers = true
            };

            var availTypedValues = new TypedValue[1];
            availTypedValues.SetValue(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, SmartEntityUtils.GetDescriptor<LevelMark>().Name), 0);

            var filter = new SelectionFilter(availTypedValues);

            var selectionResult = AcadUtils.Editor.GetSelection(pso, filter);
            if (selectionResult.Status != PromptStatus.OK || selectionResult.Value.Count == 0)
                return;

            var processMarksIds = selectionResult.Value.GetObjectIds();

            pso = new PromptSelectionOptions
            {
                // Выберите эталонную отметку уровня:
                MessageForAdding = $"\n{Language.GetItem("msg15")}",

                // Убрать объекты из выбора:
                MessageForRemoval = $"\n{Language.GetItem("msg16")}",
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
        SmartEntityUtils.SendStatistic<LevelMark>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<LevelMark>();
                
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
        var basePointPrompt = Language.GetItem("msg11");

        // <msg12>Укажите точку уровня:</msg12>
        var levelPointPrompt = Language.GetItem("msg12");

        // <msg13>Укажите точку положения отметки уровня:</msg13>
        var levelMarkPositionPointPrompt = Language.GetItem("msg13");

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