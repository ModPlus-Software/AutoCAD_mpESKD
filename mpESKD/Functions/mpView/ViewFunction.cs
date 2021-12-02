namespace mpESKD.Functions.mpView
{
    using System;
    using System.Linq;
    using Autodesk.AutoCAD.ApplicationServices;
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

        ///// <summary>
        ///// Команда создания обозначения ломанного разреза
        ///// </summary>
        //[CommandMethod("ModPlus", "mpViewBroken", CommandFlags.Modal)]
        //public void CreateSimplyViewCommand()
        //{
        //    CreateView(false);
        //}

        ///// <summary>
        ///// Команда создания обозначения разреза из полилинии
        ///// </summary>
        //[CommandMethod("ModPlus", "mpViewFromPolyline", CommandFlags.Modal)]
        //public void CreateViewFromPolylineCommand()
        //{
        //    SmartEntityUtils.SendStatistic<View>();
            
        //    try
        //    {
        //        // Выберите полилинию:
        //        var peo = new PromptEntityOptions($"\n{Language.GetItem("msg6")}")
        //        {
        //            AllowNone = false,
        //            AllowObjectOnLockedLayer = true
        //        };
        //        peo.SetRejectMessage($"\n{Language.GetItem("wrong")}");
        //        peo.AddAllowedClass(typeof(Polyline), true);

        //        var per = AcadUtils.Editor.GetEntity(peo);
        //        if (per.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }

        //        /* Регистрация ЕСКД приложения должна запускаться при запуске
        //         * функции, т.к. регистрация происходит в текущем документе
        //         * При инициализации плагина регистрации нет!
        //         */
        //        ExtendedDataUtils.AddRegAppTableRecord<View>();

        //        var style = StyleManager.GetCurrentStyle(typeof(View));
        //        var ViewLastLetterValue = string.Empty;
        //        var ViewLastIntegerValue = string.Empty;
        //        FindLastViewValues(ref ViewLastLetterValue, ref ViewLastIntegerValue);
        //        var View = new View(ViewLastIntegerValue, ViewLastLetterValue);

        //        MainFunction.CreateBlock(View);
        //        View.ApplyStyle(style, true);

        //        var plineId = per.ObjectId;

        //        using (AcadUtils.Document.LockDocument(DocumentLockMode.ProtectedAutoWrite, null, null, true))
        //        {
        //            using (var tr = AcadUtils.Document.TransactionManager.StartOpenCloseTransaction())
        //            {
        //                var dbObj = tr.GetObject(plineId, OpenMode.ForRead);
        //                if (dbObj is Polyline pline)
        //                {
        //                    for (int i = 0; i < pline.NumberOfVertices; i++)
        //                    {
        //                        if (i == 0)
        //                        {
        //                            View.InsertionPoint = pline.GetPoint3dAt(i);
        //                        }
        //                        else if (i == pline.NumberOfVertices - 1)
        //                        {
        //                            View.EndPoint = pline.GetPoint3dAt(i);
        //                        }
        //                        else
        //                        {
        //                            View.MiddlePoints.Add(pline.GetPoint3dAt(i));
        //                        }
        //                    }

        //                    View.UpdateEntities();
        //                    View.BlockRecord.UpdateAnonymousBlocks();

        //                    var ent = (BlockReference)tr.GetObject(View.BlockId, OpenMode.ForWrite, true, true);
        //                    ent.Position = pline.GetPoint3dAt(0);
        //                    ent.XData = View.GetDataForXData();
        //                }

        //                tr.Commit();
        //            }

        //            AcadUtils.Document.TransactionManager.QueueForGraphicsFlush();
        //            AcadUtils.Document.TransactionManager.FlushGraphics();

        //            // "Удалить исходную полилинию?"
        //            if (MessageBox.ShowYesNo(Language.GetItem("msg7"), MessageBoxIcon.Question))
        //            {
        //                using (var tr = AcadUtils.Document.TransactionManager.StartTransaction())
        //                {
        //                    var dbObj = tr.GetObject(plineId, OpenMode.ForWrite, true, true);
        //                    dbObj.Erase(true);
        //                    tr.Commit();
        //                }
        //            }
        //        }
        //    }
        //    catch (System.Exception exception)
        //    {
        //        ExceptionBox.Show(exception);
        //    }
        //}

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
                var View = new View(ViewLastIntegerValue, ViewLastLetterValue);

                var blockReference = MainFunction.CreateBlock(View);
                View.ApplyStyle(style, true);

                InsertViewWithJig(isSimple, View, blockReference);
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

        private static void InsertViewWithJig(bool isSimple, View View, BlockReference blockReference)
        {
            var nextPointPrompt = Language.GetItem("msg5");
            var entityJig = new DefaultEntityJig(
                View,
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
               
            }
            while (true);

            if (!View.BlockId.IsErased)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(View.BlockId, OpenMode.ForWrite, true, true);
                    ent.XData = View.GetDataForXData();
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
