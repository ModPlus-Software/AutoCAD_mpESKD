namespace mpESKD.Functions.mpBreakLine
{
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
    using ModPlusAPI.Windows;

    /// <inheritdoc />
    public class BreakLineFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new BreakLineGripPointOverrule(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<BreakLine>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<BreakLine>(), true);
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
            SmartEntityUtils.SendStatistic<BreakLine>();
            
            try
            {
                Overrule.Overruling = false;
                
                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord<BreakLine>();

                var breakLine = new BreakLine();
                var blockReference = MainFunction.CreateBlock(breakLine);

                breakLine.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                InsertBreakLineWithJig(breakLine, blockReference);
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
            SmartEntityUtils.SendStatistic<BreakLine>();
            
            try
            {
                Overrule.Overruling = false;
                
                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord<BreakLine>();
                
                var style = StyleManager.GetCurrentStyle(typeof(BreakLine));
                var breakLine = new BreakLine();

                var blockReference = MainFunction.CreateBlock(breakLine);
                breakLine.ApplyStyle(style, true);
                breakLine.BreakLineType = breakLineType;

                InsertBreakLineWithJig(breakLine, blockReference);
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

        private static void InsertBreakLineWithJig(BreakLine breakLine, BlockReference blockReference)
        {
            var entityJig = new DefaultEntityJig(
                breakLine,
                blockReference,
                new Point3d(15, 0, 0));
            do
            {
                var status = AcadUtils.Editor.Drag(entityJig).Status;
                if (status == PromptStatus.OK)
                {
                    if (entityJig.JigState == JigState.PromptInsertPoint)
                    {
                        entityJig.JigState = JigState.PromptNextPoint;
                    }
                    else
                    {
                        break;
                    }
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

            if (!breakLine.BlockId.IsErased)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(breakLine.BlockId, OpenMode.ForWrite, true, true);
                    ent.XData = breakLine.GetDataForXData();
                    tr.Commit();
                }
            }
        }
    }
}