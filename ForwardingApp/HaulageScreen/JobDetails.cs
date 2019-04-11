﻿using ASolute.Mobile.Models;
using ASolute_Mobile.CustomRenderer;
using ASolute_Mobile.HaulageScreen;
using ASolute_Mobile.Models;
using ASolute_Mobile.Utils;
using ASolute_Mobile.WoosimPrinterService;
using ASolute_Mobile.WoosimPrinterService.library.Cmds;
using Syncfusion.SfBusyIndicator.XForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLStorage;
using SignaturePad.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using XLabs.Forms.Controls;
using System.Text.RegularExpressions;
using Rg.Plugins.Popup.Services;

namespace ASolute_Mobile
{

    public class JobDetails : ContentPage
    {
        //public JobLists previousPage;
        public JobList previousPage;
        string currentJobId = Ultis.Settings.SessionCurrentJobId;
        //JobItems jobItem;
        JobItems jobItem;
        List<DetailItems> jobDetails;
        CustomEditor remarkTextEditor = null;
        List<AppImage> images = new List<AppImage>();
        Grid imageGrid, confirmGrid, trailerContainerGrid;
        double imageWidth;
        bool collectSealStatus = false, uploaded = false, connectedPrinter = false;
        Image savedSignature, map, phone, futile, camera, confirm;
        string jobNo, actionID, actionMessage, containerNumber;
        StackLayout mapPhoneStackLayout, remarksStackLayout, signatureStackLayout, imageButtonStackLayout;
        CustomEntry contPrefix, contNumber, sealEntry, mgwEntry, tareEntry, trailerEntry;
        clsHaulageModel haulageJob = new clsHaulageModel();
        static byte[] scaledImageByte;
        CheckBox check1, check2;
        string booking = "", action = "";
        SfBusyIndicator print;
        Label title1 = new Label();

        public JobDetails(string ID, string Message)
        {
            StackLayout main = new StackLayout();

            title1.FontSize = 15;

            title1.TextColor = Color.White;

            Label title2 = new Label
            {
                FontSize = 10,
                Text = Ultis.Settings.SubTitle,
                TextColor = Color.White
            };

            main.Children.Add(title1);
            main.Children.Add(title2);

            NavigationPage.SetTitleView(this, main);

            actionID = ID;
            actionMessage = Message;
            Ultis.Settings.Action = ID;
            Ultis.Settings.App = "Haulage";

            NavigationPage.SetHasNavigationBar(this, true);

            imageGrid = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            imageWidth = App.DisplayScreenWidth / 3;
            imageGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(imageWidth) });
            imageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            imageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            imageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });


            PageContent();
            Action();


            Ultis.Settings.MenuRequireAction = "Job_List";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();


            if (Ultis.Settings.NewJob.Equals("Yes"))
            {
                CommonFunction.CreateToolBarItem(this);
            }
            else
            {
                this.ToolbarItems.Clear();
            }

            MessagingCenter.Subscribe<App>((App)Application.Current, "Testing", (sender) =>
            {

                try
                {

                    CommonFunction.NewJobNotification(this);
                }
                catch (Exception e)
                {
                    DisplayAlert("Notification error", e.Message, "OK");
                }
            });

        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            MessagingCenter.Unsubscribe<App>((App)Application.Current, "Testing");

            if (connectedPrinter == true)
            {
                DependencyService.Get<IBthService>().disconnBTDevice();
            }

        }

        public static Label CreateLabel(string labelText)
        {
            var label = new Label
            {
                Text = labelText,
                Style = (Style)App.Current.Resources["jobDetailCaptionStyle"],
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.CenterAndExpand
            };

            return label;
        }

        public static CustomEntry CreateEntry(bool mandatory, int position)
        {
            var entry = new CustomEntry
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = 40,
            };

            if (mandatory)
            {
                entry.LineColor = Color.LightYellow;
            }
            else
            {
                entry.LineColor = Color.White;
            }

            if (position == 0)
            {
                entry.HorizontalTextAlignment = TextAlignment.Start;
            }
            else if (position == 1)
            {
                entry.HorizontalTextAlignment = TextAlignment.End;
            }

            return entry;
        }

        public static CheckBox CreateCheckBox(string text)
        {
            var checkBox = new CheckBox
            {
                DefaultText = text,
                FontSize = 15,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            return checkBox;
        }

        private async void GetActionID()
        {
            var content = await CommonFunction.CallWebService(0, null, Ultis.Settings.SessionBaseURI, ControllerUtil.getHaulageJobListURL(), this);
            clsResponse json_response = JsonConvert.DeserializeObject<clsResponse>(content);

            if (json_response.IsGood)
            {
                var HaulageJobList = JObject.Parse(content)["Result"].ToObject<List<clsHaulageModel>>();

                foreach (clsHaulageModel job in HaulageJobList)
                {
                    ListItems record = new ListItems
                    {
                        Id = job.Id,
                        Background = job.BackColor,
                        Category = "JobList",
                        TruckId = job.TruckId,
                        ReqSign = job.ReqSign,
                        Latitude = job.Latitude,
                        Longitude = job.Longitude,
                        TelNo = job.TelNo,
                        EventRecordId = job.EventRecordId,
                        TrailerId = job.TrailerId,
                        ContainerNo = job.ContainerNo,
                        MaxGrossWeight = job.MaxGrossWeight,
                        TareWeight = job.TareWeight,
                        CollectSeal = job.CollectSeal,
                        SealNo = job.SealNo,
                        ActionId = job.ActionId.ToString(),
                        ActionMessage = job.ActionMessage,
                        Title = job.Title,
                        SealMode = job.SealMode
                    };

                    App.Database.SaveMenuAsync(record);

                    foreach (clsCaptionValue summaryList in job.Summary)
                    {
                        SummaryItems summaryItem = new SummaryItems();

                        summaryItem.Id =  job.Id;
                        summaryItem.Caption = summaryList.Caption;
                        summaryItem.Value = summaryList.Value;
                        summaryItem.Display = summaryList.Display;
                        summaryItem.Type = "JobList";
                        summaryItem.BackColor = job.BackColor;
                        App.Database.SaveSummarysAsync(summaryItem);
                    }

                    foreach (clsCaptionValue detailList in job.Details)
                    {
                        DetailItems detailItem = new DetailItems();

                        detailItem.Id = job.Id;
                        detailItem.Caption = detailList.Caption;
                        detailItem.Value = detailList.Value;
                        detailItem.Display = detailList.Display;
                        App.Database.SaveDetailsAsync(detailItem);
                    }


                    if (job.Id.Equals(Ultis.Settings.SessionCurrentJobId))
                    {
                        Ultis.Settings.Action = job.ActionId.ToString();
                        containerNumber = job.ContainerNo;
                        actionMessage = job.ActionMessage;
                        jobItem = null;
                        PageContent();
                        Action();
                    }
                }
            }

        }

        private async void Action()
        {
            try
            {
                // Check the action id and show th control depend on what the action id needed
                if (!(String.IsNullOrEmpty(Ultis.Settings.Action)))
                {

                    if (Ultis.Settings.Action.Equals("EmptyPickup"))
                    {
                        trailerContainerGrid.IsVisible = true;
                        confirmGrid.IsVisible = true;
                        confirm.IsVisible = true;
                        mapPhoneStackLayout.IsVisible = true;
                        remarksStackLayout.IsVisible = true;
                        if (jobItem.ReqSign)
                        {
                            signatureStackLayout.IsVisible = true;
                        }
                        imageButtonStackLayout.IsVisible = true;

                        if (!(String.IsNullOrEmpty(jobItem.ContainerNo)))
                        {
                            try
                            {
                                string input = jobItem.ContainerNo;
                                string prefix = input.Substring(0, 4);
                                string number = input.Substring(4, 7);

                                contPrefix.Text = prefix;
                                contNumber.Text = number;

                                contPrefix.IsEnabled = false;
                                contNumber.IsEnabled = false;
                            }
                            catch (Exception exception)
                            {
                                await DisplayAlert("Sub string Error", exception.Message, "OK");
                            }
                        }
                    }
                    else if (Ultis.Settings.Action.Equals("Point1_In") || Ultis.Settings.Action.Equals("Point2_In"))
                    {

                        try
                        {
                            clsHaulageModel haulage = new clsHaulageModel();

                            haulage.Id = Ultis.Settings.SessionCurrentJobId;
                            if (actionID.Equals("Point1_In"))
                            {
                                haulage.ActionId = clsHaulageModel.HaulageActionEnum.Point1_In;
                            }
                            else
                            {
                                haulage.ActionId = clsHaulageModel.HaulageActionEnum.Point2_In;
                            }

                            var client = new HttpClient();
                            client.BaseAddress = new Uri(Ultis.Settings.SessionBaseURI);
                            var uri = ControllerUtil.updateHaulageJobURL();
                            var content = JsonConvert.SerializeObject(haulage);
                            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync(uri, httpContent);
                            var reply = await response.Content.ReadAsStringAsync();
                            Debug.WriteLine(reply);
                            clsResponse json_response = JsonConvert.DeserializeObject<clsResponse>(reply);

                            if (json_response.IsGood == true)
                            {
                                GetActionID();
                            }
                            else
                            {
                                await DisplayAlert("Upload Error", json_response.Message, "Okay");
                            }
                        }
                        catch (Exception exception)
                        {
                            await DisplayAlert("Error", exception.Message, "OK");
                        }


                        signatureStackLayout.IsVisible = false;
                    }
                    else if (Ultis.Settings.Action.Equals("Point1_Chk") || Ultis.Settings.Action.Equals("Point2_Chk"))
                    {
                        mapPhoneStackLayout.IsVisible = true;
                        remarksStackLayout.IsVisible = true;
                        if (jobItem.ReqSign)
                        {
                            signatureStackLayout.IsVisible = true;
                        }
                        trailerContainerGrid.IsVisible = true;
                        imageButtonStackLayout.IsVisible = true;

                        if (!(String.IsNullOrEmpty(jobItem.ContainerNo)))
                        {
                            try
                            {
                                string input = jobItem.ContainerNo.ToString();
                                string prefix = input.Substring(0, 4);
                                string number = input.Substring(4, 7);

                                contPrefix.Text = prefix;
                                contNumber.Text = number;

                                contPrefix.IsEnabled = false;
                                contNumber.IsEnabled = false;
                            }
                            catch (Exception exception)
                            {
                                await DisplayAlert("Sub string Error", exception.Message, "OK");
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void PageContent()
        {
            try
            {
                if (jobItem == null)
                {
                    jobItem = App.Database.GetItemAsync(currentJobId);
                    jobDetails = App.Database.GetDetailsAsync(currentJobId);
                }

                if (Ultis.Settings.DeleteImage == "Yes")
                {
                    DisplayImage();
                    Ultis.Settings.DeleteImage = "No";
                }

                if (remarkTextEditor == null)
                {
                    remarkTextEditor = new CustomEditor
                    {
                        WidthRequest = 120,
                        HeightRequest = 100,

                    };
                    remarkTextEditor.Text = jobItem.Remark;
                    //remarkTextEditor.BackgroundColor = Color.White;
                }

                if (!(String.IsNullOrEmpty(jobItem.Title)))
                {
                    title1.Text = jobItem.Title;
                }
                else
                {
                    Title = "Job";
                }

                StackLayout mainStackLayout = new StackLayout
                {
                    Spacing = 10,
                    BackgroundColor = Color.FromHex("#e8e5e5"),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };

                StackLayout jobDetailsStackLayout = new StackLayout
                {
                    Padding = new Thickness(15, 5, 0, 0),
                    BackgroundColor = Color.FromHex("#e8e5e5")
                };

                mapPhoneStackLayout = new StackLayout
                {
                    Padding = new Thickness(15, 0, 0, 25),
                    BackgroundColor = Color.FromHex("#e8e5e5"),
                    Orientation = StackOrientation.Horizontal
                };

                remarksStackLayout = new StackLayout
                {
                    Spacing = 0,
                    Padding = new Thickness(15, 10, 15, 10),
                    HeightRequest = 100,
                    IsVisible = false
                };

                signatureStackLayout = new StackLayout
                {
                    Spacing = 0,
                    Padding = new Thickness(15, 10, 15, 10),
                    HeightRequest = 150,
                    IsVisible = false
                };

                imageButtonStackLayout = new StackLayout
                {
                    Spacing = 30,
                    HorizontalOptions = LayoutOptions.Center,
                    Orientation = StackOrientation.Horizontal,
                    IsVisible = false
                };

                StackLayout container = new StackLayout
                {
                    Spacing = 5,
                    Orientation = StackOrientation.Horizontal
                };

                StackLayout Checked = new StackLayout
                {
                    Spacing = 5,
                    Orientation = StackOrientation.Horizontal
                };

                confirmGrid = new Grid
                {
                    Padding = new Thickness(15, 0, 15, 0)
                };

                confirmGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                confirmGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                confirmGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                confirmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                confirmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                confirmGrid.IsVisible = false;

                trailerContainerGrid = new Grid
                {
                    Padding = new Thickness(15, 0, 15, 0)
                };
                trailerContainerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                trailerContainerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                trailerContainerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                trailerContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                trailerContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                trailerContainerGrid.IsVisible = false;

                int count = 0;
                foreach (DetailItems detailItem in jobDetails)
                {
                    Label label = new Label();

                    if (detailItem.Caption == "Pickup" || detailItem.Caption == "Drop-off")
                    {
                        label.Text = detailItem.Caption + ":  " + detailItem.Value;
                        jobNo = detailItem.Caption + " :  " + detailItem.Value;

                    }
                    else if (detailItem.Caption == "")
                    {
                        label.Text = detailItem.Value;

                    }
                    else if (count == 0)
                    {
                        label.Text = detailItem.Caption + ":  " + detailItem.Value;
                        count++;
                        action = detailItem.Value;

                    }
                    else if (detailItem.Caption == "Booking")
                    {
                        label.Text = detailItem.Caption + ":  " + detailItem.Value;
                        booking = detailItem.Value;
                    }
                    else
                    {
                        label.Text = detailItem.Caption + ":  " + detailItem.Value;

                    }

                    label.FontAttributes = FontAttributes.Bold;

                    StackLayout stackLayout = new StackLayout { Orientation = StackOrientation.Horizontal, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Start, Padding = new Thickness(0, 5, 0, 5) };
                    stackLayout.Children.Add(label);
                    jobDetailsStackLayout.Children.Add(stackLayout);
                }

                var activity = new ActivityIndicator
                {
                    IsEnabled = true,
                    IsVisible = true,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    IsRunning = true
                };

                Label trailerID = CreateLabel("Trailer Id");
                Label containerNo = CreateLabel("Container No.");
                Label collectSeal = CreateLabel("Collect Seal");
                Label sealNo = CreateLabel("Seal No.");
                Label mgw = CreateLabel("MGW(KG)");
                Label tare = CreateLabel("Tare(KG)");

                trailerEntry = CreateEntry(true, 0);

                trailerEntry.TextChanged += (sender, e) =>
                {
                    string _trailertext = trailerEntry.Text.ToUpper();

                    //Get Current Text
                    if (_trailertext.Length > 10)
                    {
                        _trailertext = _trailertext.Remove(_trailertext.Length - 1);

                        trailerEntry.Unfocus();
                    }

                    trailerEntry.Text = _trailertext;
                };

                trailerEntry.Text = jobItem.TrailerId;
                if (!(String.IsNullOrEmpty(jobItem.TrailerId)))
                {
                    trailerEntry.IsEnabled = false;
                }
                else
                {
                    trailerEntry.IsEnabled = true;
                }

                sealEntry = CreateEntry(false, 0);
                sealEntry.TextChanged += (sender, e) =>
                {
                    string _sealtext = sealEntry.Text.ToUpper();

                    //Get Current Text
                    if (_sealtext.Length > 20)
                    {
                        _sealtext = _sealtext.Remove(_sealtext.Length - 1);
                        sealEntry.Unfocus();
                    }

                    sealEntry.Text = _sealtext;
                };


                sealEntry.Text = jobItem.SealNo;
                if ((Ultis.Settings.Action.Equals("Point1_Chk") || Ultis.Settings.Action.Equals("Point2_Chk")))
                {
                    if (jobItem.SealMode.Equals("R"))
                    {
                        sealEntry.IsEnabled = false;
                    }
                    else if (jobItem.SealMode.Equals("O"))
                    {
                        sealEntry.IsEnabled = true;

                    }
                    else if (jobItem.SealMode.Equals("M"))
                    {
                        sealEntry.IsEnabled = true;
                        sealEntry.LineColor = Color.LightYellow;
                    }
                    else if (jobItem.SealMode.Equals("H"))
                    {
                        sealNo.IsVisible = false;
                        sealEntry.IsVisible = false;
                    }

                }

                contPrefix = CreateEntry(true, 0);
                contNumber = CreateEntry(true, 0);
                contNumber.Keyboard = Keyboard.Numeric;
                container.Children.Add(contPrefix);
                container.Children.Add(contNumber);

                trailerContainerGrid.Children.Add(trailerID, 0, 0);
                trailerContainerGrid.Children.Add(trailerEntry, 1, 0);
                trailerContainerGrid.Children.Add(containerNo, 0, 1);
                trailerContainerGrid.Children.Add(container, 1, 1);
                trailerContainerGrid.Children.Add(sealNo, 0, 2);
                trailerContainerGrid.Children.Add(sealEntry, 1, 2);

                contPrefix.TextChanged += (sender, e) =>
                {
                    string _prefixtext = contPrefix.Text.ToUpper();
                    contPrefix.Text = _prefixtext;
                    //Get Current Text

                    if (_prefixtext.Length == 4)
                    {
                        contNumber.Focus();
                    }

                };

                contNumber.TextChanged += (sender, e) =>
                {
                    string _numbertext = contNumber.Text;

                    //Get Current Text
                    if (_numbertext.Length > 7)
                    {
                        _numbertext = _numbertext.Remove(_numbertext.Length - 1);
                        contNumber.Text = _numbertext;
                    }
                    else if (_numbertext.Length == 7)
                    {
                        contNumber.Unfocus();
                    }
                };


                check1 = new CheckBox
                {
                    HorizontalOptions = LayoutOptions.StartAndExpand,
                    Scale = 1.5,
                    DefaultText = "Yes"
                };

                check2 = new CheckBox
                {
                    HorizontalOptions = LayoutOptions.StartAndExpand,
                    Scale = 1.5,
                    DefaultText = "No"
                };

                Checked.Children.Add(check1);
                Checked.Children.Add(check2);

                check1.CheckedChanged += (sender, e) =>
                {
                    if (check2.Checked && check1.Checked)
                    {
                        check2.Checked = false;

                    }
                    if (!(String.IsNullOrEmpty(sealEntry.Text)))
                    {
                        mgwEntry.Focus();
                    }
                    collectSealStatus = true;
                };

                check2.CheckedChanged += (sender, e) =>
                {
                    if (check1.Checked && check2.Checked)
                    {
                        check1.Checked = false;

                    }

                    collectSealStatus = false;
                };


                mgwEntry = CreateEntry(false, 1);
                tareEntry = CreateEntry(false, 1);

                mgwEntry.Keyboard = Keyboard.Numeric;
                tareEntry.Keyboard = Keyboard.Numeric;

                mgwEntry.TextChanged += (sender, e) =>
                {
                    string _mgwtext = mgwEntry.Text;

                    //Get Current Text
                    if (_mgwtext.Length > 5)
                    {
                        _mgwtext = _mgwtext.Remove(_mgwtext.Length - 1);
                        mgwEntry.Text = _mgwtext;
                    }
                    else if (_mgwtext.Length == 5)
                    {
                        tareEntry.Focus();
                    }
                };

                tareEntry.TextChanged += (sender, e) =>
                {
                    string _taretext = tareEntry.Text;

                    //Get Current Text
                    if (_taretext.Length > 4)
                    {
                        _taretext = _taretext.Remove(_taretext.Length - 1);
                        tareEntry.Text = _taretext;
                    }
                };

                confirmGrid.Children.Add(collectSeal, 0, 0);
                confirmGrid.Children.Add(Checked, 1, 0);
                confirmGrid.Children.Add(mgw, 0, 1);
                confirmGrid.Children.Add(mgwEntry, 1, 1);
                confirmGrid.Children.Add(tare, 0, 2);
                confirmGrid.Children.Add(tareEntry, 1, 2);

                map = new Image
                {
                    Source = "map.png",
                    WidthRequest = 50,
                    HeightRequest = 50,
                    VerticalOptions = LayoutOptions.Center,
                    IsVisible = false,
                };
                var navigateToDest = new TapGestureRecognizer();
                navigateToDest.Tapped += (sender, e) =>
                {
                    string POI = jobItem.Latitude + "," + jobItem.Longitude;
                    Device.OpenUri(new Uri(String.Format("geo:{0}", POI)));
                };
                map.GestureRecognizers.Add(navigateToDest);
                mapPhoneStackLayout.Children.Add(map);

                phone = new Image
                {
                    Source = "phone.png",
                    WidthRequest = 50,
                    HeightRequest = 50,
                    VerticalOptions = LayoutOptions.Center,
                    IsVisible = false,
                    IsEnabled = true
                };
                var callTelNo = new TapGestureRecognizer();
                callTelNo.Tapped += (sender, e) =>
                {
                    string number = jobItem.TelNo;
                    Device.OpenUri(new Uri(String.Format("tel:{0}", number)));
                };
                phone.GestureRecognizers.Add(callTelNo);
                mapPhoneStackLayout.Children.Add(phone);

                Image printer = new Image
                {
                    Source = "printing.png",
                    WidthRequest = 50,
                    HeightRequest = 50,
                    VerticalOptions = LayoutOptions.Center
                };
                /* var printing = new TapGestureRecognizer();
                 printing.Tapped += async (sender, e) =>
                 {
                     savedTrailer = trailerEntry.Text;
                     savedContPre = contPrefix.Text;
                     savedNumber = contNumber.Text;
                     savedSealNo = sealEntry.Text;
                     savedMGW = mgwEntry.Text;
                     savedTare = tareEntry.Text;
                     savedRemark = remarkTextEditor.Text;
                     if (check1.Checked)
                     {
                         collectSealStatus = true;
                         checking = "true";
                     }
                     else
                     {
                         collectSealStatus = false;
                         checking = "false";
                     }

                     PrintConsigmentNote();

                 };
                 printer.GestureRecognizers.Add(printing);
                 mapPhoneStackLayout.Children.Add(printer);*/

                Image barcode = new Image
                {
                    Source = "barcode_scanner.png",
                    WidthRequest = 50,
                    HeightRequest = 50,
                    VerticalOptions = LayoutOptions.Center
                };
                var barCode = new TapGestureRecognizer();
                barCode.Tapped += async (sender, e) =>
                {
                    await PopupNavigation.Instance.PushAsync(new BarCodePopUp(action, booking));

                };
                barcode.GestureRecognizers.Add(barCode);
                mapPhoneStackLayout.Children.Add(barcode);

                print = new SfBusyIndicator()
                {
                    AnimationType = AnimationTypes.Print,
                    Title = "Printing",
                    TextColor = Color.Red,
                    IsVisible = false,
                    BackgroundColor = Color.Transparent
                };


                if (!string.IsNullOrEmpty(jobItem.Latitude))
                {
                    map.IsVisible = true;
                }
                if (!string.IsNullOrEmpty(jobItem.TelNo))
                {
                    phone.IsVisible = true;
                }

                var editorLabel = new Label { Text = "Remarks", Style = (Style)App.Current.Resources["jobDetailCaptionStyle"] };
                remarksStackLayout.Children.Add(editorLabel);
                remarksStackLayout.Children.Add(remarkTextEditor);

                var signatureLabel = new Label { Text = "Signature", Style = (Style)App.Current.Resources["jobDetailCaptionStyle"] };
                var signature = new SignaturePadView
                {
                    CaptionText = "",
                    StrokeColor = Color.Black,
                    BackgroundColor = Color.White,
                    StrokeWidth = 3,
                    WidthRequest = 120,
                    HeightRequest = 240,
                    SignatureLineColor = Color.White,
                    PromptText = ""
                };
                savedSignature = new Image
                {

                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    IsVisible = false
                };
                signatureStackLayout.Children.Add(signatureLabel);
                signatureStackLayout.Children.Add(signature);
                signatureStackLayout.Children.Add(savedSignature);

                if (jobItem.ReqSign)
                {
                    signatureStackLayout.IsVisible = true;
                }

                futile = new Image
                {
                    Source = "futile.png",
                    WidthRequest = 60,
                    HeightRequest = 60,
                    VerticalOptions = LayoutOptions.Center

                };
                var futileTrip = new TapGestureRecognizer();
                futileTrip.Tapped += async (sender, e) =>
                {
                    await Navigation.PushAsync(new FutileTrip_CargoReturn());
                };
                futile.GestureRecognizers.Add(futileTrip);
                imageButtonStackLayout.Children.Add(futile);

                camera = new Image
                {
                    Source = "camera.png",
                    WidthRequest = 60,
                    HeightRequest = 60,
                    VerticalOptions = LayoutOptions.Center
                };
                var takeImage = new TapGestureRecognizer();
                takeImage.Tapped += async (sender, e) =>
                {
                   
                    await CommonFunction.StoreImages(jobItem.EventRecordId.ToString(), this, "NormalImage");
                    DisplayImage();
                    BackgroundTask.StartTimer();
                };
                camera.GestureRecognizers.Add(takeImage);
                imageButtonStackLayout.Children.Add(camera);

                if (uploaded)
                {
                    confirm.IsEnabled = false;
                    confirm.Source = "confirmDisable.png";
                    futile.IsEnabled = false;
                    futile.Source = "futileDisable.png";
                }

                confirm = new Image
                {
                    Source = "confirm.png",
                    WidthRequest = 70,
                    HeightRequest = 70,
                    VerticalOptions = LayoutOptions.Center
                };
                var confirmJob = new TapGestureRecognizer();
                confirmJob.Tapped += async (sender, e) =>
                {
                    try
                    {
                        activity.IsRunning = true;
                        activity.IsVisible = true;

                        bool done = false;

                        if (signatureStackLayout.IsVisible == true)
                        {
                            Stream signatureImage = await signature.GetImageStreamAsync(SignatureImageFormat.Png, strokeColor: Color.Black, fillColor: Color.White);

                            if (signatureImage != null)
                            {
                                await CommonFunction.StoreSignature(jobItem.EventRecordId.ToString(), signatureImage, this);
                                done = true;
                                BackgroundTask.StartTimer();
                            }
                            else
                            {
                                await DisplayAlert("Signature problem", "Please sign for the job.", "OK");
                            }
                        }

                        if (done == true || signatureStackLayout.IsVisible == false)
                        {
                            confirm.IsEnabled = false;
                            confirm.Source = "confirmDisable.png";
                            futile.IsEnabled = false;
                            futile.Source = "futileDisable.png";

                            if (Ultis.Settings.Action.Equals("EmptyPickup"))
                            {
                                if (!(String.IsNullOrEmpty(trailerEntry.Text)))
                                {
                                    if (!(String.IsNullOrEmpty(contPrefix.Text)) && !(String.IsNullOrEmpty(contNumber.Text)) && contPrefix.Text.Length == 4 && contNumber.Text.Length == 7)
                                    {
                                        clsCommonFunc checker = new clsCommonFunc();
                                        bool status = checker.CheckContainerDigit(contPrefix.Text + contNumber.Text);

                                        if (status)
                                        {
                                            EmptyPickupObject();
                                        }
                                        else
                                        {
                                            var answer = await DisplayAlert("Error", "Invalid container check digit, continue?", "Yes", "No");
                                            if (answer.Equals(true))
                                            {
                                                EmptyPickupObject();
                                            }
                                            else
                                            {
                                                contNumber.Text = String.Empty;
                                                contPrefix.Text = String.Empty;
                                                confirm.IsEnabled = true;
                                                confirm.Source = "confirm.png";
                                                futile.IsEnabled = true;
                                                futile.Source = "futile.png";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        await DisplayAlert("Error", "Please fill in container number and make sure prefix have 4 character and number have 7 digit of number.", "OK");
                                        confirm.IsEnabled = true;
                                        confirm.Source = "confirm.png";
                                        futile.IsEnabled = true;
                                        futile.Source = "futile.png";
                                    }
                                }
                                else
                                {
                                    await DisplayAlert("Error", "Please fill in trailer id", "OK");
                                    confirm.IsEnabled = true;
                                    confirm.Source = "confirm.png";
                                    futile.IsEnabled = true;
                                    futile.Source = "futile.png";
                                }
                            }
                            else if (Ultis.Settings.Action.Equals("Point1_Chk") || Ultis.Settings.Action.Equals("Point2_Chk"))
                            {

                                if ((!(String.IsNullOrEmpty(sealEntry.Text)) && sealEntry.IsVisible) || sealEntry.IsVisible == false || (jobItem.SealMode.Equals("O") && sealEntry.IsVisible) || (jobItem.SealMode.Equals("R") && sealEntry.IsVisible))
                                {
                                    haulageJob.Id = Ultis.Settings.SessionCurrentJobId;
                                    if (Ultis.Settings.Action.Equals("Point1_Chk"))
                                    {
                                        haulageJob.ActionId = clsHaulageModel.HaulageActionEnum.Point1_Chk;
                                    }
                                    else
                                    {
                                        haulageJob.ActionId = clsHaulageModel.HaulageActionEnum.Point2_Chk;
                                    }
                                    if (!(String.IsNullOrEmpty(remarkTextEditor.Text)))
                                    {
                                        haulageJob.Remarks = remarkTextEditor.Text;
                                    }
                                    else
                                    {
                                        haulageJob.Remarks = "";
                                    }
                                    haulageJob.TrailerId = trailerEntry.Text;
                                    haulageJob.ContainerNo = contPrefix.Text + contNumber.Text;
                                    if (sealEntry.IsVisible == false || (String.IsNullOrEmpty(sealEntry.Text) && jobItem.SealMode.Equals("O")))
                                    {
                                        haulageJob.SealNo = "";
                                    }
                                    else
                                    {
                                        haulageJob.SealNo = sealEntry.Text;
                                    }
                                    UpdateRecord();
                                }
                                else
                                {
                                    await DisplayAlert("Error", "Please fill in the seal number", "OK");
                                    confirm.IsEnabled = true;
                                    confirm.Source = "confirm.png";
                                    futile.IsEnabled = true;
                                    futile.Source = "futile.png";
                                }
                            }

                        }

                        activity.IsRunning = false;
                        activity.IsVisible = false;

                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Reminder", "Please sign for the job.", "OK");
                    }
                };
                confirm.GestureRecognizers.Add(confirmJob);
                imageButtonStackLayout.Children.Add(confirm);

                mainStackLayout.Children.Add(jobDetailsStackLayout);
                mainStackLayout.Children.Add(mapPhoneStackLayout);
                mainStackLayout.Children.Add(print);
                mainStackLayout.Children.Add(trailerContainerGrid);
                mainStackLayout.Children.Add(confirmGrid);
                mainStackLayout.Children.Add(remarksStackLayout);
                mainStackLayout.Children.Add(signatureStackLayout);
                mainStackLayout.Children.Add(imageButtonStackLayout);
                mainStackLayout.Children.Add(imageGrid);

                Content = new ScrollView
                {
                    Content = mainStackLayout
                };

                DisplayImage();
            }
            catch (Exception e)
            {
                await DisplayAlert("Error", e.Message, "OK");
            }


        }

        public async void EmptyPickupObject()
        {
            if (check1.Checked == true || check2.Checked == true)
            {
                haulageJob.Id = Ultis.Settings.SessionCurrentJobId;
                haulageJob.ActionId = clsHaulageModel.HaulageActionEnum.EmptyPickup;
                haulageJob.ContainerNo = contPrefix.Text + contNumber.Text;
                haulageJob.TrailerId = trailerEntry.Text;
                if (!(String.IsNullOrEmpty(remarkTextEditor.Text)))
                {
                    haulageJob.Remarks = remarkTextEditor.Text;
                }
                else
                {
                    haulageJob.Remarks = "";
                }
                haulageJob.CollectSeal = collectSealStatus;
                if (!(String.IsNullOrEmpty(sealEntry.Text)))
                {
                    haulageJob.SealNo = sealEntry.Text;
                }
                else
                {
                    haulageJob.SealNo = "";
                }
                if (!(String.IsNullOrEmpty(mgwEntry.Text)) && !(String.IsNullOrEmpty(tareEntry.Text)))
                {
                    if (mgwEntry.Text.Length == 5 && tareEntry.Text.Length == 4)
                    {
                        try
                        {
                            haulageJob.MaxGrossWeight = Convert.ToInt32(mgwEntry.Text);
                            haulageJob.TareWeight = Convert.ToInt32(tareEntry.Text);

                            UpdateRecord();
                        }
                        catch
                        {
                            await DisplayAlert("Error", "Please make sure do not have decimal", "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "Please make sure max gross weight have 5 digit and tare weight have 4 digit", "OK");
                    }
                }
                else
                {
                    UpdateRecord();
                }
            }
            else
            {
                await DisplayAlert("Reminder", "Please indicate collect seal.", "OK");
                confirm.IsEnabled = true;
                confirm.Source = "confirm.png";
                futile.IsEnabled = true;
                futile.Source = "futile.png";
            }

        }

        public async void UpdateRecord()
        {
            try
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri(Ultis.Settings.SessionBaseURI);
                var uri = ControllerUtil.updateHaulageJobURL();
                var content = JsonConvert.SerializeObject(haulageJob);
                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(uri, httpContent);
                var reply = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(reply);
                clsResponse json_response = JsonConvert.DeserializeObject<clsResponse>(reply);

                if (json_response.IsGood == true)
                {

                    uploaded = true;
                   
                    Ultis.Settings.RefreshListView = "Yes";

                    /*if (signatureStackLayout.IsVisible)
                    {
                        UploadImage(jobItem.Id);
                    }*/

                    if (Ultis.Settings.Language.Equals("English"))
                    {

                        await DisplayAlert("Success", "Job updated", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Berjaya", "Kemas kini berjaya.", "OK");
                    }

                }
                else
                {
                    await DisplayAlert("Update Error", json_response.Message, "OK");
                    confirm.IsEnabled = true;
                    confirm.Source = "confirm.png";
                    futile.IsEnabled = true;
                    futile.Source = "futile.png";
                }
            }
            catch
            {
                await DisplayAlert("Error", "No internet connection", "OK");
                confirm.IsEnabled = true;
                confirm.Source = "confirm.png";
                futile.IsEnabled = true;
                futile.Source = "futile.png";
            }

        }



        private void AddThumbnailToImageGrid(Image image, AppImage appImage)
        {
            image.HeightRequest = imageWidth;
            image.HorizontalOptions = LayoutOptions.FillAndExpand;
            image.VerticalOptions = LayoutOptions.FillAndExpand;
            image.Aspect = Aspect.AspectFill;

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.CommandParameter = appImage;
            tapGestureRecognizer.Tapped += async (sender, e) =>
            {
                await Navigation.PushAsync(new ImageViewer((AppImage)((TappedEventArgs)e).Parameter));
            };
            //image.GestureRecognizers.Add(tapGestureRecognizer);
            int noOfImages = imageGrid.Children.Count();
            int noOfCols = imageGrid.ColumnDefinitions.Count();
            int rowNo = noOfImages / noOfCols;
            int colNo = noOfImages - (rowNo * noOfCols);
            imageGrid.Children.Add(image, colNo, rowNo);
        }

        public async void DisplayImage()
        {
            try
            {
                images.Clear();
                imageGrid.Children.Clear();

                images = App.Database.GetUplodedRecordImagesAsync(jobItem.EventRecordId.ToString(), "NormalImage");


                foreach (AppImage Image in images)
                {
                    byte[] imageByte;

                    IFile actualFile = await FileSystem.Current.GetFileFromPathAsync(Image.photoThumbnailFileLocation);
                    Stream stream = await actualFile.OpenAsync(PCLStorage.FileAccess.Read);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.Position = 0; // needed for WP (in iOS and Android it also works without it)!!
                        stream.CopyTo(ms);  // was empty without stream.Position = 0;
                        imageByte = ms.ToArray();
                    }
                    var image = new Image();
                    image.Source = ImageSource.FromStream(() => new MemoryStream(imageByte));
                    AddThumbnailToImageGrid(image, Image);
                }
            }
            catch
            {

            }

        }

        async void PrintConsigmentNote()
        {
            print.IsVisible = true;

            var content = await CommonFunction.CallWebService(0, null, Ultis.Settings.SessionBaseURI, ControllerUtil.getConsigmentNoteURL(currentJobId), this);
            clsResponse response = JsonConvert.DeserializeObject<clsResponse>(content);

            if (response.IsGood)
            {
                if (response.Result.Count != 0)
                {
                    try
                    {
                        if (connectedPrinter == false)
                        {

                            bool x = await DependencyService.Get<IBthService>().connectBTDevice("00:15:0E:E6:25:23");


                            if (!x)
                            {
                                Device.BeginInvokeOnMainThread(async () =>
                                {
                                    await DisplayAlert("Error", "Unable connect", "OK");
                                });
                            }
                            else
                            {
                                connectedPrinter = true;
                            }
                        }

                        System.IO.MemoryStream buffer = new System.IO.MemoryStream(512);
                        string detail = "";
                        int detailCount = 0;
                        foreach (string details in response.Result)
                        {
                            detailCount++;

                            if (details.Contains("<H>"))
                            {
                                detail = details.Replace("<H>", "");
                                WriteMemoryStream(buffer, WoosimCmd.setTextStyle(0, true, (int)WoosimCmd.TEXTWIDTH.TEXTWIDTH02, (int)WoosimCmd.TEXTHEIGHT.TEXTHEIGHT02, false));
                            }
                            else if (details.Contains("<B>"))
                            {
                                detail = details.Replace("<B>", "");
                                WriteMemoryStream(buffer, WoosimCmd.setTextStyle(0, true, (int)WoosimCmd.TEXTWIDTH.TEXTWIDTH01, (int)WoosimCmd.TEXTHEIGHT.TEXTHEIGHT01, false));
                            }
                            else
                            {
                                detail = details;
                                WriteMemoryStream(buffer, WoosimCmd.setTextStyle(0, false, (int)WoosimCmd.TEXTWIDTH.TEXTWIDTH01, (int)WoosimCmd.TEXTHEIGHT.TEXTHEIGHT01, false));
                            }


                            if (detailCount == response.Result.Count)
                            {
                                detail = detail + "\r\n\r\n";
                            }
                            else
                            {
                                detail = detail + "\r\n";
                            }


                            WriteMemoryStream(buffer, Encoding.ASCII.GetBytes(detail));

                        }

                        WriteMemoryStream(buffer, WoosimPageMode.print());

                        DependencyService.Get<IBthService>().WriteComm(buffer.ToArray());
                    }
                    catch (Exception error)
                    {
                        await DisplayAlert("Error", error.Message, "OK");
                    }
                }

            }
            else
            {
                await DisplayAlert("Json Error", response.Message, "OK");
            }

            print.IsVisible = false;
        }

        async private void WriteMemoryStream(System.IO.MemoryStream stream, byte[] data)
        {
            await stream.WriteAsync(data, 0, data.Length);
        }

    }
}
