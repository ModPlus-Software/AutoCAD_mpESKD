namespace mpESKD.Functions.mpSecantNodalLeader
{
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
    public class SecantNodalLeaderFunction : ISmartEntityFunction
    {
        /// <inheritdoc/>
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SecantNodalLeaderGripPointOverrule(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityOsnapOverrule<SecantNodalLeader>(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), new SmartEntityObjectOverrule<SecantNodalLeader>(), true);
        }

        /// <inheritdoc/>
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
#if !DEBUG
            Statistic.SendCommandStarting(
                SecantNodalLeader.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(SecantNodalLeader.GetDescriptor());
                var lastNodeNumber = FindLastNodeNumber();
                var nodalLeader = new SecantNodalLeader(lastNodeNumber);
                var blockReference = MainFunction.CreateBlock(nodalLeader);
                nodalLeader.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                InsertSecantNodalLeaderWithJig(nodalLeader, blockReference);
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
        /// Команда создания секущей узловой выноски
        /// </summary>
        [CommandMethod("ModPlus", "mpSecantNodalLeader", CommandFlags.Modal)]
        public void CreateSecantNodalLeaderCommand()
        {
            CreateSecantNodalLeader();
        }
        
        private static void CreateSecantNodalLeader()
        {
#if !DEBUG
            Statistic.SendCommandStarting(
                SecantNodalLeader.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(SecantNodalLeader.GetDescriptor());
                var style = StyleManager.GetCurrentStyle(typeof(SecantNodalLeader));
                var lastNodeNumber = FindLastNodeNumber();
                var nodalLeader = new SecantNodalLeader(lastNodeNumber);

                var blockReference = MainFunction.CreateBlock(nodalLeader);
                nodalLeader.ApplyStyle(style, true);

                InsertSecantNodalLeaderWithJig(nodalLeader, blockReference);
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
        
        private static void InsertSecantNodalLeaderWithJig(SecantNodalLeader secantNodalLeader, BlockReference blockReference)
        {
            // <msg1>Укажите точку вставки:</msg1>
            var insertionPointPrompt = Language.GetItem("msg1");

            // <msg18>Укажите точку выноски:</msg18>
            var leaderPointPrompt = Language.GetItem("msg18");
            
            var entityJig = new DefaultEntityJig(secantNodalLeader, blockReference, new Point3d(0, 0, 0))
            {
                PromptForInsertionPoint = insertionPointPrompt
            };
            
            secantNodalLeader.JigState = SecantNodalLeaderJigState.InsertionPoint;
            do
            {
                var status = AcadUtils.Editor.Drag(entityJig).Status;
                if (status == PromptStatus.OK)
                {
                    if (secantNodalLeader.JigState == SecantNodalLeaderJigState.InsertionPoint)
                    {
                        secantNodalLeader.JigState = SecantNodalLeaderJigState.LeaderPoint;
                        entityJig.PromptForNextPoint = leaderPointPrompt;
                        entityJig.PreviousPoint = secantNodalLeader.InsertionPoint;
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
            
            if (!secantNodalLeader.BlockId.IsErased)
            {
                using (var tr = AcadUtils.Database.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(secantNodalLeader.BlockId, OpenMode.ForWrite, true, true);
                    ent.XData = secantNodalLeader.GetDataForXData();
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
            AcadUtils.GetAllIntellectualEntitiesInCurrentSpace<SecantNodalLeader>(typeof(SecantNodalLeader)).ForEach(a =>
            {
                allValues.Add(a.NodeNumber);
            });

            return allValues.OrderBy(s => s, new OrdinalStringComparer()).LastOrDefault();
        }
    }
}
