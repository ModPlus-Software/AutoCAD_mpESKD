namespace mpESKD.Functions.mpLevelPlanMark;

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
using mpESKD.Functions.mpLevelMark;

/// <inheritdoc />
public class LevelPlanMarkFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new LevelMarkGripPointOverrule(), true);
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

            var levelPlanMark = new LevelPlanMark();

            var blockReference = MainFunction.CreateBlock(levelPlanMark);

            levelPlanMark.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);
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

    /// <summary>
    /// Команда создания обозначения уровня
    /// </summary>
    [CommandMethod("ModPlus", "mpLevelPlanMark", CommandFlags.Modal)]
    public void CreateViewLabelCommand()
    {
        CreateLevelPlanMark();
    }

    private static void CreateLevelPlanMark()
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

            var levelPlanMark = new LevelPlanMark();

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
}