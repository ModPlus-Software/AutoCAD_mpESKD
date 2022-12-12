namespace mpESKD.Functions.mpCrestedLeader;

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
using System.Collections.Generic;
using System.Linq;

/// <inheritdoc />
public class CrestedLeaderFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new CrestedLeaderGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<CrestedLeader>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<CrestedLeader>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<CrestedLeader>();
            
        try
        {
            Overrule.Overruling = false;

            ExtendedDataUtils.AddRegAppTableRecord<CrestedLeader>();

            var lastNodeNumber = FindLastNodeNumber();
            var CrestedLeader = new CrestedLeader(lastNodeNumber);
            var blockReference = MainFunction.CreateBlock(CrestedLeader);
            CrestedLeader.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            InsertCrestedLeaderWithJig(CrestedLeader, blockReference);
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
    /// Команда создания цепной выноски
    /// </summary>
    [CommandMethod("ModPlus", "mpCrestedLeader", CommandFlags.Modal)]
    public void CreateLevelPlanMarkCommand()
    {
        CreateCrestedLeader();
    }

    private static void CreateCrestedLeader()
    {
        SmartEntityUtils.SendStatistic<CrestedLeader>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<CrestedLeader>();

            var style = StyleManager.GetCurrentStyle(typeof(CrestedLeader));
            var lastNodeNumber = FindLastNodeNumber();
            var CrestedLeader = new CrestedLeader(lastNodeNumber);

            var blockReference = MainFunction.CreateBlock(CrestedLeader);
            CrestedLeader.ApplyStyle(style, true);
            
            InsertCrestedLeaderWithJig(CrestedLeader, blockReference);
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

    private static void InsertCrestedLeaderWithJig(CrestedLeader CrestedLeader, BlockReference blockReference)
    {
        // <msg1>Укажите точку вставки:</msg1>
        var insertionPointPrompt = Language.GetItem("msg1");

        // <msg17>Укажите точку выноски:</msg17> 
        var leaderPointPrompt = Language.GetItem("msg18");

        var entityJig = new DefaultEntityJig(CrestedLeader, blockReference, new Point3d(0, 0, 0))
        {
            PromptForInsertionPoint = insertionPointPrompt
        };
        
        CrestedLeader.JigState = CrestedLeaderJigState.InsertionPoint;
        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;
            if (status == PromptStatus.OK)
            {
                if (CrestedLeader.JigState == CrestedLeaderJigState.InsertionPoint)
                {
                    AcadUtils.WriteMessageInDebug(CrestedLeader.JigState.Value.ToString());

                    CrestedLeader.JigState = CrestedLeaderJigState.LeaderPoint;
                    entityJig.PromptForNextPoint = leaderPointPrompt;
                    entityJig.PreviousPoint = CrestedLeader.InsertionPoint;
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

        if (!CrestedLeader.BlockId.IsErased)
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(CrestedLeader.BlockId, OpenMode.ForWrite, true, true);
                ent.XData = CrestedLeader.GetDataForXData();
                tr.Commit();
            }
        }
    }

    /// <summary>
    /// Поиск номера узла последней созданной узловой выноски
    /// </summary>
    private static string FindLastNodeNumber()
    {
        if (!MainSettings.Instance.CrestedLeaderContinueNodeNumber)
            return string.Empty;

        var allValues = new List<string>();
        AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<CrestedLeader>(typeof(CrestedLeader)).ForEach(a =>
        {
            allValues.Add(a.NodeNumber);
        });

        return allValues.OrderBy(s => s, new OrdinalStringComparer()).LastOrDefault();
    }
}