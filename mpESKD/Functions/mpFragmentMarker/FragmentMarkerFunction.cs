namespace mpESKD.Functions.mpFragmentMarker
{
    using System.Collections.Generic;
    using System.Diagnostics;
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

    /// <inheritdoc />
    public class FragmentMarkerFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new FragmentMarkerGripPointOverrule(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<FragmentMarker>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<FragmentMarker>(), true);
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
#if !DEBUG
            Statistic.SendCommandStarting(
                FragmentMarker.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;
                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(FragmentMarker.GetDescriptor());
                var lastNodeNumber = FindLastNodeNumber();
                var fragmentMarker = new FragmentMarker(lastNodeNumber);
                var blockReference = MainFunction.CreateBlock(fragmentMarker);

                fragmentMarker.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                InsertFragmentMarkerWithJig(fragmentMarker, blockReference);
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

        [CommandMethod("ModPlus", "mpFragmentMarker", CommandFlags.Modal)]
        public void CreateFragment()
        {
            CreateFragmentMarker();
        }

        private static void CreateFragmentMarker()
        {
#if !DEBUG
            Statistic.SendCommandStarting(
                FragmentMarker.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;
                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(FragmentMarker.GetDescriptor());
                var style = StyleManager.GetCurrentStyle(typeof(FragmentMarker));
                var lastNodeNumber = FindLastNodeNumber();
                var fragmentMarker = new FragmentMarker(lastNodeNumber);

                var blockReference = MainFunction.CreateBlock(fragmentMarker);
                fragmentMarker.ApplyStyle(style, true);

                InsertFragmentMarkerWithJig(fragmentMarker, blockReference);
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

        private static void InsertFragmentMarkerWithJig(FragmentMarker fragmentMarker, BlockReference blockReference)
        {
            // <msg1>Укажите точку вставки:</msg1>
            var insertionPointPrompt = Language.GetItem("msg1");

            // <msg17>Укажите точку рамки:</msg17> // <-- TODO другой текст. Про вторую точку
            var endPointPrompt = Language.GetItem("msg17");

            // <msg18>Укажите точку выноски:</msg18>
            var leaderPointPrompt = Language.GetItem("msg18");

            var entityJig = new DefaultEntityJig(fragmentMarker, blockReference, new Point3d(15, 0, 0), point3d =>
            {
                fragmentMarker.LeaderPoint = point3d;
            })
            {
                PromptForInsertionPoint = insertionPointPrompt
            };

            fragmentMarker.JigState = FragmentMarkerJigState.InsertionPoint;

            do
            {
                var status = AcadUtils.Editor.Drag(entityJig).Status;
                if (status == PromptStatus.OK)
                {
                    if (fragmentMarker.JigState == FragmentMarkerJigState.InsertionPoint)
                    {
                        Debug.Print(fragmentMarker.JigState.Value.ToString());

                        fragmentMarker.JigState = FragmentMarkerJigState.EndPoint;
                        entityJig.PromptForNextPoint = endPointPrompt;
                        entityJig.PreviousPoint = fragmentMarker.InsertionPoint;
                        
                        entityJig.JigState = JigState.PromptNextPoint;
                    }
                    else if (fragmentMarker.JigState == FragmentMarkerJigState.EndPoint)
                    {
                        Debug.Print(fragmentMarker.JigState.Value.ToString());
                        
                        fragmentMarker.JigState = FragmentMarkerJigState.LeaderPoint;
                        entityJig.PromptForCustomPoint = leaderPointPrompt;
                        
                        // Тут не нужна привязка к предыдущей точке
                        entityJig.PreviousPoint = fragmentMarker.InsertionPoint;
                        entityJig.JigState = JigState.CustomPoint;
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

            if (!fragmentMarker.BlockId.IsErased)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(fragmentMarker.BlockId, OpenMode.ForWrite, true, true);
                    ent.XData = fragmentMarker.GetDataForXData();
                    tr.Commit();
                }
            }
        }

        /// <summary>
        /// Поиск номера узла последней созданной узловой выноски
        /// </summary>
        private static string FindLastNodeNumber()
        {
            if (!MainSettings.Instance.FragmentMarkerContinueNodeNumber)
                return string.Empty;

            var allValues = new List<string>();
            AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<FragmentMarker>(typeof(FragmentMarker)).ForEach(a =>
            {
                allValues.Add(a.MainText);
            });

            return allValues.OrderBy(s => s, new OrdinalStringComparer()).LastOrDefault();
        }
    }
}