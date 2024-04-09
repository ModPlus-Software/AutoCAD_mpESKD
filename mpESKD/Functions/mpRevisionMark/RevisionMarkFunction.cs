namespace mpESKD.Functions.mpRevisionMark;

using System.Collections.Generic;
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
using ModPlusAPI.IO;
using ModPlusAPI.Windows;

/// <inheritdoc/>
public class RevisionMarkFunction : ISmartEntityFunction
{
    /// <inheritdoc/>
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new RevisionMarkGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<RevisionMark>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<RevisionMark>(), true);
    }

    /// <inheritdoc/>
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

            var lastNodeNumber = FindLastNodeNumber();
            var revisionMark = new RevisionMark(lastNodeNumber);
            var blockReference = MainFunction.CreateBlock(revisionMark);
            revisionMark.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            InsertRevisionMarkWithJig(revisionMark, blockReference);
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

    /// <summary>
    /// Команда создания узловой выноски
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
            var lastNodeNumber = FindLastNodeNumber();
            var revisionMark = new RevisionMark(lastNodeNumber);

            var blockReference = MainFunction.CreateBlock(revisionMark);
            revisionMark.ApplyStyle(style, true);

            InsertRevisionMarkWithJig(revisionMark, blockReference);
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

    private static void InsertRevisionMarkWithJig(RevisionMark revisionMark, BlockReference blockReference)
    {
        // <msg1>Укажите точку вставки:</msg1>
        var insertionPointPrompt = Language.GetItem("msg1");

        // <msg17>Укажите точку рамки:</msg17>
        var framePointPrompt = Language.GetItem("msg17");

        // <msg18>Укажите точку выноски:</msg18>
        var leaderPointPrompt = Language.GetItem("msg18");

        var entityJig = new DefaultEntityJig(revisionMark, blockReference, new Point3d(0, 0, 0), point3d =>
        {
            revisionMark.LeaderPoint = point3d;
        })
        {
            PromptForInsertionPoint = insertionPointPrompt
        };

        revisionMark.JigState = RevisionMarkJigState.InsertionPoint;
        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;
            if (status == PromptStatus.OK)
            {
                if (revisionMark.JigState == RevisionMarkJigState.InsertionPoint)
                {
                    revisionMark.JigState = RevisionMarkJigState.EndPoint;
                    entityJig.PromptForNextPoint = framePointPrompt;
                    entityJig.PreviousPoint = revisionMark.InsertionPoint;

                    entityJig.JigState = JigState.PromptNextPoint;
                }
                else if (revisionMark.JigState == RevisionMarkJigState.EndPoint)
                {
                    AcadUtils.Editor.WriteMessage($"{revisionMark.JigState.Value.ToString()}");
                    revisionMark.JigState = RevisionMarkJigState.LeaderPoint;
                    entityJig.PromptForCustomPoint = leaderPointPrompt;

                    // Тут не нужна привязка к предыдущей точке
                    entityJig.PreviousPoint = revisionMark.InsertionPoint;
                    entityJig.JigState = JigState.CustomPoint;
                }
                else
                {
                    break;
                }
            }
            else
            {
                EntityUtils.Erase(blockReference.Id);
                break;
            }
        }
        while (true);

        if (!revisionMark.BlockId.IsErased)
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(revisionMark.BlockId, OpenMode.ForWrite, true, true);
                ent.XData = revisionMark.GetDataForXData();
                tr.Commit();
            }
        }
    }

    /// <summary>
    /// Поиск номера узла последней созданной узловой выноски
    /// </summary>
    private static string FindLastNodeNumber()
    {
        if (!MainSettings.Instance.RevisionMarkContinueNodeNumber)
            return string.Empty;

        var allValues = new List<string>();
        AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<RevisionMark>(typeof(RevisionMark)).ForEach(a =>
        {
            allValues.Add(a.RevisionNumber);
        });

        return allValues.OrderBy(s => s, new OrdinalStringComparer()).LastOrDefault();
    }
}