﻿using System;
using System.Collections.Generic;
using ASolute.Mobile.Models;
using ASolute_Mobile.Utils;
//using Com.OneSignal;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ASolute_Mobile.CustomerTracking
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AccountActivation : ContentPage
    {
        static string firebaseID = "qwert-qwer45-asfafaf";

        public AccountActivation()
        {
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            Application.Current.MainPage = new MainPage();
            return true;
        }

        public async void Submit_Clicked(object sender, EventArgs e)
        {
            var content = await CommonFunction.GetWebService(Ultis.Settings.SessionBaseURI, ControllerUtil.sendActivationURL(ActivationEntry.Text));
            clsResponse send_response = JsonConvert.DeserializeObject<clsResponse>(content);

            if (send_response.IsGood)
            {
               // OneSignal.Current.IdsAvailable((playerID, pushToken) => firebaseID = playerID);

                var login_content = await CommonFunction.GetWebService(Ultis.Settings.SessionBaseURI, ControllerUtil.getActionURL(Ultis.Settings.DeviceUniqueID, firebaseID));
                clsResponse login_response = JsonConvert.DeserializeObject<clsResponse>(login_content);

                if(login_response.IsGood)
                {
                    Ultis.Settings.SessionSettingKey = login_response.Result["SessionId"];
                    Ultis.Settings.SessionUserId = login_response.Result["UserName"];
                    Application.Current.MainPage = new MainPage();
                }
                else
                {
                    await DisplayAlert("Error", login_response.Message, "OK");
                }


            }
            else
            {
                await DisplayAlert("Error", send_response.Message, "OK");
            }
        }

        public async void Resend_Clicked(object sender, EventArgs e)
        {
            var content = await CommonFunction.GetWebService(Ultis.Settings.SessionBaseURI, ControllerUtil.sendActivationURL());
            clsResponse resend_response = JsonConvert.DeserializeObject<clsResponse>(content);

            if(resend_response.IsGood)
            {
                await DisplayAlert("Success", "Activaton had been send to your email.", "OK");
            }
            else
            {
                await DisplayAlert("Error", resend_response.Message, "OK");
            }
        }
    }
}
