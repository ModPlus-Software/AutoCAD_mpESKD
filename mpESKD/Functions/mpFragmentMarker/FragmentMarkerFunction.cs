﻿namespace mpESKD.Functions.mpFragmentMarker
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Abstractions;
    using Base.Enums;
    using Base.Styles;
    using Base.Utils;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Overrules;

    /// <inheritdoc />
    public class FragmentMarkerFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), FragmentMarkerGripPointOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), FragmentMarkerOsnapOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), FragmentMarkerObjectOverrule.Instance(), true);
        }

        /// <inheritdoc />
        public void Terminate()
        {
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)),  FragmentMarkerGripPointOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), FragmentMarkerOsnapOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), FragmentMarkerObjectOverrule.Instance());
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

                var fragmentMarker = new FragmentMarker();
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
                var breakLine = new FragmentMarker();

                var blockReference = MainFunction.CreateBlock(breakLine);
                breakLine.ApplyStyle(style, true);

                InsertFragmentMarkerWithJig(breakLine, blockReference);
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
            var entityJig = new DefaultEntityJig(
                fragmentMarker,
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
    }
}