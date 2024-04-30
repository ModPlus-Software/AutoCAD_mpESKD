namespace mpESKD.Functions.mpRevisionMark;

using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Abstractions;
using Base.Overrules;
using Base.Styles;
using Base.Utils;
using ModPlusAPI.IO;
using ModPlusAPI.Windows;
using System.Collections.Generic;

/// <inheritdoc />
public class RevisionMarkFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new RevisionMarkGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<RevisionMark>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<RevisionMark>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<RevisionMark>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<RevisionMark>();

            var revisionMark = new RevisionMark(FindLastRevisionNumber());
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
        CreateRevisionMark();
    }

    private static void CreateRevisionMark()
    {
        SmartEntityUtils.SendStatistic<RevisionMark>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<RevisionMark>();

            var style = StyleManager.GetCurrentStyle(typeof(RevisionMark));
            var revisionMark = new RevisionMark(FindLastRevisionNumber());

            var blockReference = MainFunction.CreateBlock(revisionMark);
            revisionMark.ApplyStyle(style, true);

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

    private static void InsertRevisionMarkWithJig(RevisionMark revisionMark, BlockReference blockReference)
    {
        var entityJig = new DefaultEntityJig(
            revisionMark,
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
            var ent = tr.GetObject(revisionMark.BlockId, OpenMode.ForWrite, true, true);
            ent.XData = revisionMark.GetDataForXData();
            tr.Commit();
        }
    }

    /// <summary>
    /// Поиск номера изменения последнего созданного маркера изменения
    /// </summary>
    private static string FindLastRevisionNumber()
    {
        if (!MainSettings.Instance.RevisionMarkContinueRevisionNumber)
            return string.Empty;

        var allValues = new List<string>();
        AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<RevisionMark>(typeof(RevisionMark)).ForEach(a =>
        {
            allValues.Add(a.RevisionNumber);
        });

        return allValues.OrderBy(s => s, new OrdinalStringComparer()).LastOrDefault();
    }
}