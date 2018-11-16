﻿namespace mpESKD.Functions.mpBreakLine
{
    using System.Diagnostics.CodeAnalysis;
    using Autodesk.AutoCAD.Runtime;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Base;
    using Base.Enums;
    using Overrules;
    using Base.Helpers;
    using mpESKD.Base.Styles;
    using ModPlusAPI;
    using ModPlusAPI.Windows;


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class BreakLineFunction : IIntellectualEntityFunction
    {
        public void Initialize()
        {
            // Включение работы переопределения ручек (нужна регенерация в конце метода (?))
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), BreakLineGripPointsOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), BreakLineOsnapOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), BreakLineObjectOverrule.Instance(), true);
            Overrule.Overruling = true;
        }
        public void Terminate()
        {
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), BreakLineGripPointsOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), BreakLineOsnapOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), BreakLineObjectOverrule.Instance());
        }
    }

    public class BreakLineCommands
    {
        [CommandMethod("ModPlus", "mpBreakLine", CommandFlags.Modal)]
        public void CreateLinearBreakLine()
        {
            CreateBreakLine(BreakLineType.Linear);
        }
        [CommandMethod("ModPlus", "mpBreakLineCurve", CommandFlags.Modal)]
        public void CreateCurvilinearBreakLine()
        {
            CreateBreakLine(BreakLineType.Curvilinear);
        }
        [CommandMethod("ModPlus", "mpBreakLineCylinder", CommandFlags.Modal)]
        public void CreateCylindricalBreakLine()
        {
            CreateBreakLine(BreakLineType.Cylindrical);
        }

        private static void CreateBreakLine(BreakLineType breakLineType)
        {
            // send statistic
            Statistic.SendCommandStarting(BreakLineInterface.Name, MpVersionData.CurCadVers);
            try
            {
                Overrule.Overruling = false;
                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataHelpers.AddRegAppTableRecord(BreakLineInterface.Name);
                var style = StyleManager.GetCurrentStyle(typeof(BreakLine));
                var breakLine = new BreakLine { BreakLineType = breakLineType };
                breakLine.ApplyStyle(style);
                var blockReference = MainFunction.CreateBlock(breakLine);
                
                // set layer
                AcadHelpers.SetLayerByName(blockReference.ObjectId, style.GetLayerNameProperty(), style.LayerXmlData);

                var breakLoop = false;
                //todo change it
                while (!breakLoop)
                {
                    //var breakLineJig = new BreakLineJig(breakLine, blockReference);
                    var breakLineJig = new DefaultEntityJig(
                        breakLine, 
                        blockReference,
                        new Point3d(15, 0, 0),
                        Language.GetItem(MainFunction.LangItem, "msg2"));
                    do
                    {
                        label0:
                        var status = AcadHelpers.Editor.Drag(breakLineJig).Status;
                        if (status == PromptStatus.OK)
                        {
                            if (breakLineJig.JigState != JigState.PromptInsertPoint)
                            {
                                breakLoop = true;
                                status = PromptStatus.Other;
                            }
                            else
                            {
                                breakLineJig.JigState = JigState.PromptNextPoint;
                                goto label0;
                            }
                        }
                        else if (status != PromptStatus.Other)
                        {
                            using (AcadHelpers.Document.LockDocument())
                            {
                                using (var tr = AcadHelpers.Document.TransactionManager.StartTransaction())
                                {
                                    var obj = (BlockReference)tr.GetObject(blockReference.Id, OpenMode.ForWrite);
                                    obj.Erase(true);
                                    tr.Commit();
                                }
                            }
                            breakLoop = true;
                        }
                        else
                        {
                            breakLine.UpdateEntities();
                            breakLine.BlockRecord.UpdateAnonymousBlocks();
                        }
                    } while (!breakLoop);
                }
                if (!breakLine.BlockId.IsErased)
                {
                    using (var tr = AcadHelpers.Database.TransactionManager.StartTransaction())
                    {
                        var ent = tr.GetObject(breakLine.BlockId, OpenMode.ForWrite);
                        ent.XData = breakLine.GetParametersForXData();
                        tr.Commit();
                    }
                }
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
    }

}