namespace mpESKD.Functions.mpLevelPlanMark;

using System;
using System.Linq;
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
public class LevelPlanMarkFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new ViewLabelGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<LevelPlanMark>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<LevelPlanMark>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<LevelPlanMark>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<LevelPlanMark>();

            var viewLabelLastLetterValue = string.Empty;
            var viewLabelLastIntegerValue = string.Empty;
            FindLastViewLabelValues(ref viewLabelLastLetterValue, ref viewLabelLastIntegerValue);
            var viewLabel = new LevelPlanMark(viewLabelLastIntegerValue, viewLabelLastLetterValue);

            var blockReference = MainFunction.CreateBlock(viewLabel);

            viewLabel.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);
            InsertLabelWithJig(viewLabel, blockReference);
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
    /// Команда создания обозначения уровня
    /// </summary>
    [CommandMethod("ModPlus", "mpLevelPlanMark", CommandFlags.Modal)]
    public void CreateViewLabelCommand()
    {
        CreateViewLabel(LevelPlanMark);
    }

    private static void CreateViewLabel(LevelPlanMark viewLabelType)
    {
        SmartEntityUtils.SendStatistic<LevelPlanMark>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<LevelPlanMark>();

            var style = StyleManager.GetCurrentStyle(typeof(LevelPlanMark));
            var viewLabelLastLetterValue = string.Empty;
            var viewLabelLastIntegerValue = string.Empty;

            FindLastViewLabelValues(ref viewLabelLastLetterValue, ref viewLabelLastIntegerValue);
            var levelPlanMark = new LevelPlanMark(viewLabelLastIntegerValue, viewLabelLastLetterValue);

            var blockReference = MainFunction.CreateBlock(levelPlanMark);
            levelPlanMark.ApplyStyle(style, true);
            InsertLabelWithJig(levelPlanMark, blockReference);
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

    private static void InsertLabelWithJig(LevelPlanMark levelPlanMark, BlockReference blockReference)
    {
        var nextPointPrompt = Language.GetItem("msg5");
        var entityJig = new DefaultEntityJig(
            levelPlanMark,
            blockReference,
            new Point3d(20, 0, 0));

        var status = AcadUtils.Editor.Drag(entityJig).Status;
        if (status != PromptStatus.OK)
            return;

        entityJig.JigState = JigState.PromptNextPoint;
        entityJig.PromptForNextPoint = nextPointPrompt;

        if (levelPlanMark.BlockId.IsErased)
            return;

        using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
        {
            var ent = tr.GetObject(levelPlanMark.BlockId, OpenMode.ForWrite, true, true);
            ent.XData = levelPlanMark.GetDataForXData();
            tr.Commit();
        }
    }

    /// <summary>
    /// Поиск последних цифровых и буквенных значений разрезов на текущем виде
    /// </summary>
    private static void FindLastViewLabelValues(ref string viewLabelLastLetterValue, ref string viewLabelLastIntegerValue)
    {
        if (!MainSettings.Instance.ViewLabelSaveLastTextAndContinueNew)
            return;
        var viewLabels = AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<LevelPlanMark>(typeof(LevelPlanMark));
        if (!viewLabels.Any())
            return;
        viewLabels.Sort((s1, s2) => string.Compare(s1.BlockRecord.Name, s2.BlockRecord.Name, StringComparison.Ordinal));
        var valueDesignation = viewLabels.Last().Designation;
        if (int.TryParse(valueDesignation, out var i))
        {
            viewLabelLastIntegerValue = i.ToString();
        }
        else
        {
            viewLabelLastLetterValue = valueDesignation;
        }
    }
}