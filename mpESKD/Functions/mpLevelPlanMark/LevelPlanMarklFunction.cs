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

/// <inheritdoc />
public class LevelPlanMarkFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new LevelPlanMarkGripPointOverrule(), true);
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
            InsertLevelPlanMarkWithJig(levelPlanMark, blockReference);
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
    public void CreateLevelPlanMarkCommand()
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
            InsertLevelPlanMarkWithJig(levelPlanMark, blockReference);
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

    private static void InsertLevelPlanMarkWithJig(LevelPlanMark levelPlanMark, BlockReference blockReference)
    {
        var entityJig = new DefaultEntityJig(
            levelPlanMark,
            blockReference,
            new Point3d(20, 0, 0));

        var status = AcadUtils.Editor.Drag(entityJig).Status;
        if (status != PromptStatus.OK)
        {
            EntityUtils.Erase(blockReference.Id);
            return;
        }
        
        using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
        {
            var ent = tr.GetObject(levelPlanMark.BlockId, OpenMode.ForWrite, true, true);
            ent.XData = levelPlanMark.GetDataForXData();
            tr.Commit();
        }
    }
}