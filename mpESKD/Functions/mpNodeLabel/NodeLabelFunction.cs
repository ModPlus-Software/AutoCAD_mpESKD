namespace mpESKD.Functions.mpNodeLabel;

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Base;
using Base.Overrules;
using Base.Styles;
using Base.Utils;
using ModPlusAPI.IO;
using ModPlusAPI.Windows;
using Base.Abstractions;

/// <inheritdoc />
public class NodeLabelFunction : ISmartEntityFunction
{
    /// <inheritdoc />
    public void Initialize()
    {
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new NodeLabelGripPointOverrule(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<NodeLabel>(), true);
        Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<NodeLabel>(), true);
    }

    /// <inheritdoc />
    public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
    {
#if !DEBUG
        SmartEntityUtils.SendStatistic<NodeLabel>();
#endif

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<NodeLabel>();

            var lastNodeNumber = FindLastNodeNumber();
            var nodeLabel = new NodeLabel(lastNodeNumber);
            var blockReference = MainFunction.CreateBlock(nodeLabel);
            nodeLabel.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

            InsertNodeLabelWithJig(nodeLabel, blockReference);
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
    /// Команда создания обозначения узла
    /// </summary>
    [CommandMethod("ModPlus", "mpNodeLabel", CommandFlags.Modal)]
    public void CreateNodeLabelCommand()
    {
        CreateNodeLabel();
    }

    private static void CreateNodeLabel()
    {
        SmartEntityUtils.SendStatistic<NodeLabel>();

        try
        {
            Overrule.Overruling = false;

            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord<NodeLabel>();

            var style = StyleManager.GetCurrentStyle(typeof(NodeLabel));

            var lastNodeNumber = FindLastNodeNumber();
            var nodeLabel = new NodeLabel(lastNodeNumber);
            var blockReference = MainFunction.CreateBlock(nodeLabel);

            nodeLabel.ApplyStyle(style, true);

            InsertNodeLabelWithJig(nodeLabel, blockReference);
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

    private static void InsertNodeLabelWithJig(NodeLabel nodeLabel, BlockReference blockReference)
    {
        var entityJig = new DefaultEntityJig(
            nodeLabel,
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
            var ent = tr.GetObject(nodeLabel.BlockId, OpenMode.ForWrite, true, true);
            ent.XData = nodeLabel.GetDataForXData();
            tr.Commit();
        }
    }

    /// <summary>
    /// Поиск номера узла последней созданной узловой выноски
    /// </summary>
    private static string FindLastNodeNumber()
    {
        if (!MainSettings.Instance.NodeLabelContinueNodeNumber)
            return string.Empty;

        var allValues = new List<string>();
        AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<NodeLabel>(typeof(NodeLabel)).ForEach(a =>
        {
            allValues.Add(a.NodeNumber);
        });

        return allValues.OrderBy(s => s, new OrdinalStringComparer()).LastOrDefault();
    }
}