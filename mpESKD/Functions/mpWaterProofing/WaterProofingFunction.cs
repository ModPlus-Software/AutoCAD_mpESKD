namespace mpESKD.Functions.mpWaterProofing
{
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using Base;
    using Base.Abstractions;
    using Base.Styles;
    using Base.Utils;
    using ModPlusAPI.Windows;
    using Overrules;

    /// <inheritdoc />
    public class WaterProofingFunction : ISmartEntityFunction
    {
        /// <inheritdoc />
        public void Initialize()
        {
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingGripPointOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingOsnapOverrule.Instance(), true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingObjectOverrule.Instance(), true);
        }

        /// <inheritdoc />
        public void Terminate()
        {
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingGripPointOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingOsnapOverrule.Instance());
            Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), WaterProofingObjectOverrule.Instance());
        }

        /// <inheritdoc />
        public void CreateAnalog(SmartEntity sourceEntity, bool copyLayer)
        {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(
                WaterProofing.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif

            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(WaterProofing.GetDescriptor());

                var waterProofing = new WaterProofing();
                var blockReference = MainFunction.CreateBlock(waterProofing);

                waterProofing.SetPropertiesFromSmartEntity(sourceEntity, copyLayer);

                LinearEntityUtils.InsertWithJig(waterProofing, blockReference);
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
        /// Команда создания линии грунта
        /// </summary>
        [CommandMethod("ModPlus", "mpWaterProofing", CommandFlags.Modal)]
        public void CreateWaterProofingCommand()
        {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(
                WaterProofing.GetDescriptor().Name, ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            CreateWaterProofing();
        }

        /// <summary>
        /// Команда создания линия грунта из полилинии
        /// </summary>
        [CommandMethod("ModPlus", "mpWaterProofingFromPolyline", CommandFlags.Modal)]
        public void CreateWaterProofingFromPolylineCommand()
        {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(
                "mpWaterProofingFromPolyline", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            /* Регистрация ЕСКД приложения должна запускаться при запуске
             * функции, т.к. регистрация происходит в текущем документе
             * При инициализации плагина регистрации нет!
             */
            ExtendedDataUtils.AddRegAppTableRecord(WaterProofing.GetDescriptor());
            LinearEntityUtils.CreateFromPolyline<WaterProofing>();
        }

        private void CreateWaterProofing()
        {
            try
            {
                Overrule.Overruling = false;

                /* Регистрация ЕСКД приложения должна запускаться при запуске
                 * функции, т.к. регистрация происходит в текущем документе
                 * При инициализации плагина регистрации нет!
                 */
                ExtendedDataUtils.AddRegAppTableRecord(WaterProofing.GetDescriptor());

                var style = StyleManager.GetCurrentStyle(typeof(WaterProofing));
                var waterProofing = new WaterProofing();

                var blockReference = MainFunction.CreateBlock(waterProofing);
                waterProofing.ApplyStyle(style, true);

                LinearEntityUtils.InsertWithJig(waterProofing, blockReference);
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