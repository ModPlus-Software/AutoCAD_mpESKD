namespace mpESKD.Functions.mpViewLabel;

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
public class ViewLabelFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new ViewLabelGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<ViewLabel>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<ViewLabel>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<ViewLabel>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<ViewLabel>();

            var viewLabelLastLetterValue = string.Empty;
            var viewLabelLastIntegerValue = string.Empty;
            FindLastViewLabelValues(ref viewLabelLastLetterValue, ref viewLabelLastIntegerValue);
            var viewLabel = new ViewLabel(viewLabelLastIntegerValue, viewLabelLastLetterValue);

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
    /// Команда создания обозначения разреза
    /// </summary>
    [CommandMethod("ModPlus", "mpViewLabel", CommandFlags.Modal)]
    public void CreateViewLabelCommand()
    {
        CreateViewLabel(ViewLabelType.View);
    }

    /// <summary>
    /// Команда создания обозначения разреза
    /// </summary>
    [CommandMethod("ModPlus", "mpSectionLabel", CommandFlags.Modal)]
    public void CreateSectionCommand()
    {
        CreateViewLabel(ViewLabelType.Section);
    }

    private static void CreateViewLabel(ViewLabelType viewLabelType)
    {
        SmartEntityUtils.SendStatistic<ViewLabel>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<ViewLabel>();

            var style = StyleManager.GetCurrentStyle(typeof(ViewLabel));
            var viewLabelLastLetterValue = string.Empty;
            var viewLabelLastIntegerValue = string.Empty;

            FindLastViewLabelValues(ref viewLabelLastLetterValue, ref viewLabelLastIntegerValue);
            var viewLabel = new ViewLabel(viewLabelLastIntegerValue, viewLabelLastLetterValue);

            var blockReference = MainFunction.CreateBlock(viewLabel);
            viewLabel.ApplyStyle(style, true);
            viewLabel.ViewType = viewLabelType;
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

    private static void InsertLabelWithJig(ViewLabel viewLabel, BlockReference blockReference)
    {
        var nextPointPrompt = Language.GetItem("msg5");
        var entityJig = new DefaultEntityJig(
            viewLabel,
            blockReference,
            new Point3d(20, 0, 0));

        var status = AcadUtils.Editor.Drag(entityJig).Status;
        if (status != PromptStatus.OK)
            return;

        entityJig.JigState = JigState.PromptNextPoint;
        entityJig.PromptForNextPoint = nextPointPrompt;

        if (viewLabel.BlockId.IsErased)
            return;

        using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
        {
            var ent = tr.GetObject(viewLabel.BlockId, OpenMode.ForWrite, true, true);
            ent.XData = viewLabel.GetDataForXData();
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
        var viewLabels = AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<ViewLabel>(typeof(ViewLabel));
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