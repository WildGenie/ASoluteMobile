﻿using System;

using Android.App;
using Android.Util;
using Android.Content;
using Android.OS;
using Android.Locations;
using Xamarin.Forms;
using System.Threading.Tasks;
using Haulage.Droid;
using System.Net.Http;
using ASolute_Mobile.Utils;
using Newtonsoft.Json;
using ASolute.Mobile.Models;
using Android.Runtime;
using Android.Net.Wifi;

namespace ASolute_Mobile.Droid.Services
{
   [Service]
    public class LocationService : Service, ILocationListener
    {
        public event EventHandler<LocationChangedEventArgs> LocationChanged = delegate { };
        public event EventHandler<ProviderDisabledEventArgs> ProviderDisabled = delegate { };
        public event EventHandler<ProviderEnabledEventArgs> ProviderEnabled = delegate { };
        public event EventHandler<StatusChangedEventArgs> StatusChanged = delegate { };
        public HandlerThread _handlerThreadSingle;
        private  PowerManager.WakeLock _mWakeLock;
        private WifiManager.WifiLock _wifiLock;
        public LocationService()
        {
        }

        // Set our location manager as the system location service
        protected LocationManager LocMgr = Android.App.Application.Context.GetSystemService("location") as LocationManager;

        readonly string logTag = "LocationService";
        IBinder binder;

        public override void OnCreate()
        {
            base.OnCreate();
            Log.Debug(logTag, "OnCreate called in the Location Service");

            var pm = (PowerManager)GetSystemService(Context.PowerService);
            _mWakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "PartialWakeLockTag");
            _mWakeLock.Acquire();

            var wf = (WifiManager)GetSystemService(Context.WifiService);
            _wifiLock = wf.CreateWifiLock(Android.Net.WifiMode.Full, "WifiLockTag");
            _wifiLock.Acquire();
        }

        // This gets called when StartService is called in our App class
        [Obsolete("deprecated in base class")]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(logTag, "LocationService started");

            

            return StartCommandResult.Sticky;
        }


        // This gets called once, the first time any client bind to the Service
        // and returns an instance of the LocationServiceBinder. All future clients will
        // reuse the same instance of the binder
        public override IBinder OnBind(Intent intent)
        {
            Log.Debug(logTag, "Client now bound to service");

            binder = new LocationServiceBinder(this);
            return binder;
        }

        // Handle location updates from the location manager
        public void StartLocationUpdates()
        {
            //we can set different location criteria based on requirements for our app -
            //for example, we might want to preserve power, or get extreme accuracy


            /* var locationCriteria = new Criteria();
             var locationProviderTest = LocMgr.AllProviders;
             string test = locationProviderTest[0];
              locationCriteria.Accuracy = Accuracy.Medium;
              locationCriteria.PowerRequirement = Power.Medium;

              //get provider: GPS, Network, etc.
             var locationProvider = LocMgr.GetBestProvider(locationCriteria, true);

             Log.Debug(logTag, string.Format("You are about to get location updates via {0}", locationProvider));*/

            var location = LocMgr.GetLastKnownLocation(LocationManager.GpsProvider);
        
            // Get an initial fix on location
            LocMgr.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, this);

            Device.StartTimer(TimeSpan.FromSeconds(60), () =>
            {
                Task.Run(async () => {

                    await BackgroundTask.GetGPS();

                    await Task.Delay(600000);
                });
                return true;
            });
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Log.Debug(logTag, "Service has been terminated");
            // Stop getting updates from the location manager:
            LocMgr.RemoveUpdates(this);
            //_mWakeLock.Release();
            //_wifiLock.Release();
        }

        #region ILocationListener implementation
        // ILocationListener is a way for the Service to subscribe for updates
        // from the System location Service

        public void OnLocationChanged(Android.Locations.Location location)
        {
            this.LocationChanged(this, new LocationChangedEventArgs(location));

            // This should be updating every time we request new location updates
            // both when teh app is in the background, and in the foreground


            Log.Debug(logTag, String.Format("Latitude is {0}", location.Latitude));
            Log.Debug(logTag, String.Format("Longitude is {0}", location.Longitude));
            Log.Debug(logTag, String.Format("Altitude is {0}", location.Altitude));
            Log.Debug(logTag, String.Format("Speed is {0}", location.Speed));
            Log.Debug(logTag, String.Format("Accuracy is {0}", location.Accuracy));
            Log.Debug(logTag, String.Format("Bearing is {0}", location.Bearing));

        }

        public void OnProviderDisabled(string provider)
        {
            this.ProviderDisabled(this, new ProviderDisabledEventArgs(provider));
        }

        public void OnProviderEnabled(string provider)
        {
            this.ProviderEnabled(this, new ProviderEnabledEventArgs(provider));
        }

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            this.StatusChanged(this, new StatusChangedEventArgs(provider, status, extras));
        }

        #endregion

    }
}