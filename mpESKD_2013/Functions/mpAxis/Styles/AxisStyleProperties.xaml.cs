﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Windows;
using mpESKD.Base.Helpers;
using mpESKD.Base.Styles;
using System.Collections.Generic;

namespace mpESKD.Functions.mpAxis.Styles
{
    public partial class AxisStyleProperties
    {
        public AxisStyleProperties(string layerNameFromStyle)
        {
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
            ModPlusAPI.Windows.Helpers.WindowHelpers.ChangeStyleForResourceDictionary(Resources);
            // markers positions
            //CbMarkersPosition.ItemsSource = AxisPropertiesHelpers.AxisMarkersTypeLocalNames;
            // get list of scales
            CbScale.ItemsSource = AcadHelpers.Scales;
            // fill text styles
            CbTextStyle.ItemsSource = AcadHelpers.TextStyles;
            // layers
            var layers = AcadHelpers.Layers;
            layers.Insert(0, ModPlusAPI.Language.GetItem(MainFunction.LangItem, "defl")); // "По умолчанию"
            if (!layers.Contains(layerNameFromStyle))
                layers.Insert(1, layerNameFromStyle);
            CbLayerName.ItemsSource = layers;
            // marker types
            var markerTypes = new List<string>
            {
                ModPlusAPI.Language.GetItem(MainFunction.LangItem, "type1"), // "Тип 1",
                ModPlusAPI.Language.GetItem(MainFunction.LangItem, "type2") // "Тип 2"
            };
            CbFirstMarkerType.ItemsSource = markerTypes;
            CbSecondMarkerType.ItemsSource = markerTypes;
            CbThirdMarkerType.ItemsSource = markerTypes;
            CbOrientMarkerType.ItemsSource = markerTypes;
        }
        private void FrameworkElement_OnGotFocus(object sender, RoutedEventArgs e)
        {
            
        }

        private void FrameworkElement_OnLostFocus(object sender, RoutedEventArgs e)
        {
            StyleEditorWork.ShowDescription(string.Empty);
        }
        // set line type
        private void TbLineType_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            using (AcadHelpers.Document.LockDocument())
            {
                var ltd = new LinetypeDialog { IncludeByBlockByLayer = false };
                if (ltd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!ltd.Linetype.IsNull)
                        using (var tr = AcadHelpers.Document.TransactionManager.StartTransaction())
                        {
                            using (var ltr = tr.GetObject(ltd.Linetype, OpenMode.ForRead) as LinetypeTableRecord)
                            {
                                if (ltr != null)
                                {
                                    TbLineType.Text = ltr.Name;
                                }
                            }
                            tr.Commit();
                        }
                }
            }
        }
    }

    public class AxisMarkersPositionValueConverter : IValueConverter
    {
        //public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    return AxisPropertiesHelpers.GetLocalAxisMarkersPositionName((AxisMarkersPosition) value);
        //}

        //public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    //return AxisPropertiesHelpers.GetAxisMarkersPositionByLocalName((string) value);
        //}
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}