namespace mpESKD.Functions.mpRevisionMark;

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
public class RevisionMarkFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: Initialize");

        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new RevisionMarkGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<RevisionMark>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<RevisionMark>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: CreateAnalog");

        SmartEntityUtils.SendStatistic<RevisionMark>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<RevisionMark>();

            var revisionMark = new RevisionMark();

            var blockReference = MainFunction.CreateBlock(revisionMark);

            revisionMark.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);
            InsertRevisionMarkWithJig(revisionMark, blockReference);
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
    [CommandMethod("ModPlus", "mpRevisionMark", CommandFlags.Modal)]
    public void CreateRevisionMarkCommand()
    {
        AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: CreateRevisionMarkCommand");
        CreateRevisionMark();
    }

    private static void CreateRevisionMark()
    {
        AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: CreateRevisionMark");

#if !DEBUG
        SmartEntityUtils.SendStatistic<RevisionMark>();
#endif

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<RevisionMark>();

            var style = StyleManager.GetCurrentStyle(typeof(RevisionMark));

            var revisionMark = new RevisionMark();

            AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: CreateRevisionMark; " +
                                          "before [var blockReference = MainFunction.CreateBlock(revisionMark);]");
            var blockReference = MainFunction.CreateBlock(revisionMark);
            AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: CreateRevisionMark; " +
                                          "after [var blockReference = MainFunction.CreateBlock(revisionMark);]");

            revisionMark.ApplyStyle(style, true);
            InsertRevisionMarkWithJig(revisionMark, blockReference);
        }
        catch (System.Exception exception)
        {
            AcadUtils.WriteMessageInDebug("ERROR: class: RevisionMarkFunction; metod: CreateRevisionMark");
            ExceptionBox.Show(exception);
        }
        finally
        {
            Overrule.Overruling = true;
        }

        AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: CreateRevisionMark => END" );
    }

    private static void InsertRevisionMarkWithJig(RevisionMark revisionMark, BlockReference blockReference)
    {
        AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: InsertRevisionMarkWithJig");

        var entityJig = new DefaultEntityJig(
            revisionMark,
            blockReference,
            new Point3d(20, 0, 0));

        AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: InsertRevisionMarkWithJig; " +
                                      "before [var status = AcadUtils.Editor.Drag(entityJig).Status;]");

        var status = AcadUtils.Editor.Drag(entityJig).Status;

        AcadUtils.WriteMessageInDebug("REVISIONMARK: class: RevisionMarkFunction; metod: InsertRevisionMarkWithJig; " +
                                      "after [var status = AcadUtils.Editor.Drag(entityJig).Status;]");

        if (status != PromptStatus.OK)
        {
            EntityUtils.Erase(blockReference.Id);
            return;
        }

        using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
        {
            var ent = tr.GetObject(revisionMark.BlockId, OpenMode.ForWrite, true, true);
            ent.XData = revisionMark.GetDataForXData();
            tr.Commit();
        }
    }
}