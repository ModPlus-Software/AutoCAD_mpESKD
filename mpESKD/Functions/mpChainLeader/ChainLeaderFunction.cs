namespace mpESKD.Functions.mpChainLeader;

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
public class ChainLeaderFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new ChainLeaderGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<ChainLeader>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<ChainLeader>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
        SmartEntityUtils.SendStatistic<ChainLeader>();
            
        try
        {
            Overrule.Overruling = false;

            ExtendedDataUtils.AddRegAppTableRecord<ChainLeader>();

            var lastNodeNumber = FindLastNodeNumber();
            var chainLeader = new ChainLeader(lastNodeNumber);
            var blockReference = MainFunction.CreateBlock(chainLeader);
            chainLeader.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            InsertChainLeaderWithJig(chainLeader, blockReference);
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
    [CommandMethod("ModPlus", "mpChainLeader", CommandFlags.Modal)]
    public void CreateLevelPlanMarkCommand()
    {
        CreateChainLeader();
    }

    private static void CreateChainLeader()
    {
        SmartEntityUtils.SendStatistic<ChainLeader>();
            
        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<ChainLeader>();

            var style = StyleManager.GetCurrentStyle(typeof(ChainLeader));
            var lastNodeNumber = FindLastNodeNumber();
            var chainLeader = new ChainLeader(lastNodeNumber);

            var blockReference = MainFunction.CreateBlock(chainLeader);
            chainLeader.ApplyStyle(style, true);
            
            InsertChainLeaderWithJig(chainLeader, blockReference);
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

    private static void InsertChainLeaderWithJig(ChainLeader chainLeader, BlockReference blockReference)
    {
        // Укажите точку выноски:
        var leaderPointPrompt = Language.GetItem("msg18");

        var entityJig = new DefaultEntityJig(chainLeader, blockReference, new Point3d(0, 0, 0));
        
        chainLeader.JigState = ChainLeaderJigState.InsertionPoint;
        do
        {
            var status = AcadUtils.Editor.Drag(entityJig).Status;
            if (status == PromptStatus.OK)
            {
                if (chainLeader.JigState == ChainLeaderJigState.InsertionPoint)
                {
                    AcadUtils.WriteMessageInDebug(chainLeader.JigState.Value.ToString());

                    chainLeader.JigState = ChainLeaderJigState.LeaderPoint;
                    entityJig.PromptForNextPoint = leaderPointPrompt;
                    entityJig.PreviousPoint = chainLeader.InsertionPoint;
                }
                else
                {
                    break;
                }

                entityJig.JigState = JigState.PromptNextPoint;
            }
            else
            {
                EntityUtils.Erase(blockReference.Id);
                break;
            }
        }
        while (true);

        if (!chainLeader.BlockId.IsErased)
        {
            using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(chainLeader.BlockId, OpenMode.ForWrite, true, true);
                ent.XData = chainLeader.GetDataForXData();
                tr.Commit();
            }
        }
    }

    /// <summary>
    /// Поиск номера узла последней созданной узловой выноски
    /// </summary>
    private static string FindLastNodeNumber()
    {
        if (!MainSettings.Instance.ChainLeaderContinueNodeNumber)
            return string.Empty;

        var allValues = new List<string>();
        AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<ChainLeader>(typeof(ChainLeader)).ForEach(a =>
        {
            allValues.Add(a.LeaderTextValue);
        });

        return allValues.OrderBy(s => s, new OrdinalStringComparer()).LastOrDefault();
    }
}