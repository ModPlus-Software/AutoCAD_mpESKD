namespace mpESKD.Functions.mpNodalLeader;

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
public class NodalLeaderFunction : ISmartEntityFunction
{
    /// <inheritdoc/>
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new NodalLeaderGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<NodalLeader>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<NodalLeader>(), true);
    }

    /// <inheritdoc/>
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<NodalLeader>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<NodalLeader>();
                
            var lastNodeNumber = FindLastNodeNumber();
            var nodalLeader = new NodalLeader(lastNodeNumber);
            var blockReference = MainFunction.CreateBlock(nodalLeader);
            nodalLeader.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            InsertNodalLeaderWithJig(nodalLeader, blockReference);
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
    [CommandMethod("ModPlus", "mpNodalLeader", CommandFlags.Modal)]
    public void CreateNodalLeaderCommand()
    {
        CreateNodalLeader();
    }

    private static void CreateNodalLeader()
    {
        SmartEntityUtils.SendStatistic<NodalLeader>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<NodalLeader>();
                
            var style = StyleManager.GetCurrentStyle(typeof(NodalLeader));
            var lastNodeNumber = FindLastNodeNumber();
            var nodalLeader = new NodalLeader(lastNodeNumber);

            var blockReference = MainFunction.CreateBlock(nodalLeader);
            nodalLeader.ApplyStyle(style, true);

            InsertNodalLeaderWithJig(nodalLeader, blockReference);
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

    private static void InsertNodalLeaderWithJig(NodalLeader nodalLeader, BlockReference blockReference)
    {
        // <msg1>Укажите точку вставки:</msg1>
        var insertionPointPrompt = Language.GetItem("msg1");

        // <msg17>Укажите точку рамки:</msg17>
        var framePointPrompt = Language.GetItem("msg17");

        // <msg18>Укажите точку выноски:</msg18>
        var leaderPointPrompt = Language.GetItem("msg18");
            
        var entityJig = new DefaultEntityJig(nodalLeader, blockReference, new Point3d(0, 0, 0),point3d =>
        {
            nodalLeader.LeaderPoint = point3d;
        })
        {
            PromptForInsertionPoint = insertionPointPrompt
        };
            
        nodalLeader.JigState = NodalLeaderJigState.InsertionPoint;
        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;
            if (status == PromptStatus.OK)
            {
                if (nodalLeader.JigState == NodalLeaderJigState.InsertionPoint)
                {
                    AcadUtils.Editor.WriteMessage($" JigState in function {nodalLeader.JigState.Value.ToString()} \n");
                    nodalLeader.JigState = NodalLeaderJigState.EndPoint;
                    entityJig.PromptForNextPoint = framePointPrompt;
                    entityJig.PreviousPoint = nodalLeader.InsertionPoint;

                    entityJig.JigState = JigState.PromptNextPoint;
                }
                else if (nodalLeader.JigState == NodalLeaderJigState.EndPoint)
                {
                    AcadUtils.Editor.WriteMessage($"{nodalLeader.JigState.Value.ToString()}");
                    nodalLeader.JigState = NodalLeaderJigState.LeaderPoint;
                    entityJig.PromptForCustomPoint = leaderPointPrompt;
                    //nodalLeader.LeaderPoint = nodalLeader.EndPoint;
                        
                    // Тут не нужна привязка к предыдущей точке
                    entityJig.PreviousPoint = nodalLeader.InsertionPoint;
                    entityJig.JigState = JigState.CustomPoint;
                }
                else
                {
                    AcadUtils.Editor.WriteMessage($" JigState in function {nodalLeader.JigState.Value.ToString()} \n");
                    break;
                }

                //entityJig.JigState = JigState.PromptNextPoint;
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
            
        if (!nodalLeader.BlockId.IsErased)
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(nodalLeader.BlockId, OpenMode.ForWrite, true, true);
                ent.XData = nodalLeader.GetDataForXData();
                tr.Commit();
            }
        }
    }

    /// <summary>
    /// Поиск номера узла последней созданной узловой выноски
    /// </summary>
    private static string FindLastNodeNumber()
    {
        if (!MainSettings.Instance.NodalLeaderContinueNodeNumber)
            return string.Empty;

        var allValues = new List<string>();
        AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<NodalLeader>(typeof(NodalLeader)).ForEach(a =>
        {
            allValues.Add(a.NodeNumber);
        });

        return allValues.OrderBy(s => s, new OrdinalStringComparer()).LastOrDefault();
    }
}