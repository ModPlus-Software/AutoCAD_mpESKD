namespace mpESKD.Functions.mpView
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
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using System;
    using System.Linq;

    /// <inheritdoc />
    public class ViewFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new ViewGripPointOverrule(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<View>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<View>(), true);
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
            SmartEntityUtils.SendStatistic<View>();
            
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord<View>();

                var ViewLastLetterValue = string.Empty;
                var ViewLastIntegerValue = string.Empty;
                FindLastViewValues(ref ViewLastLetterValue, ref ViewLastIntegerValue);
                var view = new View(ViewLastIntegerValue, ViewLastLetterValue);

                var blockReference = MainFunction.CreateBlock(view);

                view.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                InsertViewWithJig(true, view, blockReference);
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
        /// Команда создания обозначения разреза
        /// </summary>
        [CommandMethod("ModPlus", "mpView", CommandFlags.Modal)]
        public void CreateViewCommand()
        {
            CreateView(true);
        }

        private static void CreateView(bool isSimple)
        {
            SmartEntityUtils.SendStatistic<View>();
            
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord<View>();

                var style = StyleManager.GetCurrentStyle(typeof(View));
                var ViewLastLetterValue = string.Empty;
                var ViewLastIntegerValue = string.Empty;
                FindLastViewValues(ref ViewLastLetterValue, ref ViewLastIntegerValue);
                var view = new View(ViewLastIntegerValue, ViewLastLetterValue);

                var blockReference = MainFunction.CreateBlock(view);
                view.ApplyStyle(style, true);

                InsertViewWithJig(isSimple, view, blockReference);
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

        private static void InsertViewWithJig(bool isSimple, View view, BlockReference blockReference)
        {
            var nextPointPrompt = Language.GetItem("msg5");
            var entityJig = new DefaultEntityJig(
                view,
                blockReference,
                new Point3d(20, 0, 0));
            do
            {
                var status = AcadUtils.Editor.Drag(entityJig).Status;
                if (status == PromptStatus.OK)
                {
                    if (isSimple)
                    {
                        if (entityJig.JigState == JigState.PromptInsertPoint)
                        {
                            entityJig.JigState = JigState.PromptNextPoint;
                            entityJig.PromptForNextPoint = nextPointPrompt;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
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

            if (!view.BlockId.IsErased)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(view.BlockId, OpenMode.ForWrite, true, true);
                    ent.XData = view.GetDataForXData();
                    tr.Commit();
                }
            }
        }

        /// <summary>
        /// Поиск последних цифровых и буквенных значений разрезов на текущем виде
        /// </summary>
        private static void FindLastViewValues(ref string ViewLastLetterValue, ref string ViewLastIntegerValue)
        {
            if (MainSettings.Instance.ViewContinueNodeNumber)
            {
                var Views = AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<View>(typeof(View));
                if (Views.Any())
                {
                    Views.Sort((s1, s2) => string.Compare(s1.BlockRecord.Name, s2.BlockRecord.Name, StringComparison.Ordinal));
                    var v = Views.Last().Designation;
                    if (int.TryParse(v, out var i))
                    {
                        ViewLastIntegerValue = i.ToString();
                    }
                    else
                    {
                        ViewLastLetterValue = v;
                    }
                }
            }
        }
    }
}
