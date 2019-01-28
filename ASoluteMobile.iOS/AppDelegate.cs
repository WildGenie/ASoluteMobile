﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Foundation;
using Tesseract.iOS;
using UIKit;
using Xamarin.Forms;
using XLabs.Ioc;
using XLabs.Ioc.Autofac;
using XLabs.Platform.Services.Media;
using ASolute_Mobile;


namespace ASolute_Mobile.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {

            Rg.Plugins.Popup.Popup.Init();
            global::Xamarin.Forms.Forms.Init();


            ZXing.Net.Mobile.Forms.iOS.Platform.Init();
            LoadApplication(new App());
            Xamarin.FormsMaps.Init();
            Syncfusion.SfChart.XForms.iOS.Renderers.SfChartRenderer.Init();
            Syncfusion.SfDataGrid.XForms.iOS.SfDataGridRenderer.Init();
            Syncfusion.SfAutoComplete.XForms.iOS.SfAutoCompleteRenderer.Init();
            Syncfusion.SfBusyIndicator.XForms.iOS.SfBusyIndicatorRenderer.Init();
            Syncfusion.XForms.iOS.ComboBox.SfComboBoxRenderer.Init();
            Syncfusion.XForms.iOS.Buttons.SfButtonRenderer.Init();
            Syncfusion.XForms.iOS.TextInputLayout.SfTextInputLayoutRenderer.Init();
            Syncfusion.SfNumericTextBox.XForms.iOS.SfNumericTextBoxRenderer.Init();
            Syncfusion.SfPullToRefresh.XForms.iOS.SfPullToRefreshRenderer.Init();
            Syncfusion.ListView.XForms.iOS.SfListViewRenderer.Init();

            UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(UIApplication.BackgroundFetchIntervalMinimum);

            App.DisplayScreenWidth = UIScreen.MainScreen.Bounds.Width;

            return base.FinishedLaunching(app, options);
        }

        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
        {
            // Check for new data, and display it


            // Inform system of fetch results
            completionHandler(UIBackgroundFetchResult.NewData);
        }
    }
}