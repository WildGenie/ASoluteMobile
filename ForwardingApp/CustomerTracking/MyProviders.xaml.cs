﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ASolute.Mobile.Models;
using ASolute_Mobile.Models;
using ASolute_Mobile.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plugin.Geolocator;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace ASolute_Mobile.CustomerTracking
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MyProviders : ContentPage
    {

        public MyProviders()
        {
            InitializeComponent();
            Title = "Home";
            loading.IsRunning = true;
            loading.IsVisible = true;
            loading.IsEnabled = true;
        }

        public async Task StartListening()
        {
            if (CrossGeolocator.Current.IsListening)
                return;

            await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(60), 0, true, new Plugin.Geolocator.Abstractions.ListenerSettings
            {
                ActivityType = Plugin.Geolocator.Abstractions.ActivityType.AutomotiveNavigation,
                AllowBackgroundUpdates = true,
                DeferLocationUpdates = true,
                DeferralDistanceMeters = 1,
                ListenForSignificantChanges = true,
                PauseLocationUpdatesAutomatically = true
            });
        }

        protected async override void OnAppearing()
        {
            try
            {
                if (Ultis.Settings.AppFirstInstall == "First")
                {

                    Ultis.Settings.AppFirstInstall = "Second";
                    var content = await CommonFunction.GetRequestAsync(Ultis.Settings.SessionBaseURI, ControllerUtil.getAutoScanURL());
                    clsResponse autoScan_response = JsonConvert.DeserializeObject<clsResponse>(content);

                    if (autoScan_response.IsGood)
                    {
                        await DisplayAlert("Succeed", autoScan_response.Result, "OK");
                        await getProviderList();
                    }
                    else
                    {
                        await DisplayAlert("Error", autoScan_response.Message, "OK");
                    }

                    await StartListening();
                }
                else
                {

                    if (App.Database.GetMainMenu("ProviderList").Count == 0 || Ultis.Settings.AppFirstInstall == "Refresh")
                    {
                        await getProviderList();
                        Ultis.Settings.AppFirstInstall = "No";
                    }
                    else
                    {
                        LoadProviderList();
                    }

                }
            }
            catch
            {

            }
                
        }

        public async void selectProvider(object sender, ItemTappedEventArgs e)
        {

            await Navigation.PushAsync(new ContainerCategory(((ListItems)e.Item).Id,((ListItems)e.Item).Name));


        }

        protected async void refreshProviderList(object sender, EventArgs e)
        {
            await getProviderList();
            provide_list.IsRefreshing = false;

        }
    
        public async Task getProviderList()
        {
            var content = await CommonFunction.GetRequestAsync(Ultis.Settings.SessionBaseURI, ControllerUtil.getProviderListURL());
            clsResponse provider_response = JsonConvert.DeserializeObject<clsResponse>(content);

            if(provider_response.IsGood)
            {
                var providers = JObject.Parse(content)["Result"].ToObject<List<clsProvider>>();

              
                App.Database.deleteRecords("ProviderList");
                App.Database.DeleteProvider();
                
                foreach(clsProvider p in providers)
                {

                    ListItems menu = new ListItems();
                    menu.Id = p.Code;
                    menu.Name = p.Name;
                    menu.Category = "ProviderList";
                   App.Database.SaveMenuAsync(menu);


                    ProviderInfo available_provider = new ProviderInfo();
                    available_provider.Code = p.Code;
                    available_provider.Name = p.Name;
                    

                    App.Database.SaveProvider(available_provider);
                }
               
                 LoadProviderList();
            }
            else
            {
                await DisplayAlert("JsonError", provider_response.Message, "OK");
            }
        }

        public void LoadProviderList()
        {
            Ultis.Settings.List = "provider_List";
            ObservableCollection<ListItems> Item = new ObservableCollection<ListItems>(App.Database.GetMainMenu("ProviderList"));
            provide_list.ItemsSource = Item;
            provide_list.HasUnevenRows = true;          
            provide_list.ItemTemplate = new DataTemplate(typeof(CustomListViewCell));

            loading.IsRunning = false;
            loading.IsVisible = false;
            loading.IsEnabled = false;
        }
    }
}
