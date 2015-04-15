using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using System.IO;
using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Maps;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;
using Windows.Devices.Geolocation; //Provides the Geocoordinate class. 
using System.IO.IsolatedStorage;

namespace Sydney_MTB_Riders
{
    public partial class MainPage : PhoneApplicationPage
    {

        // Constructor
        public MainPage()
        {

            InitializeComponent();

          
            //Location Settings
            if (!System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings.Contains("EnabledLocation"))
                EnableLocation.IsChecked = true;
            else
                EnableLocation.IsChecked = (bool?)System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings["EnableLocation"] ?? true;
            

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            
        }
        
        

        // Check if user has been prompted and display message
        public static Boolean UserPrompted
        {
            get
            {
                Boolean Result = false;
                if (IsolatedStorageSettings.ApplicationSettings.Contains("UserPrompted"))
                {
                    Result = (Boolean)IsolatedStorageSettings.ApplicationSettings["UserPrompted"]; 
                }
                return Result;                
            }
            set
            {
                IsolatedStorageSettings.ApplicationSettings["UserPrompted"] = value;
            }

        }

        public static Boolean LocationEnabled
        {
            get
            {
                Boolean Result = false;
                if (IsolatedStorageSettings.ApplicationSettings.Contains("EnableLocation"))
                {
                    Result = (Boolean)IsolatedStorageSettings.ApplicationSettings["EnableLocation"];
                }
                return Result;
            }
            set
            {
                IsolatedStorageSettings.ApplicationSettings["EnableLocation"] = value;
            }

        }

        // Placeholder code to contain the ApplicationID and AuthenticationToken 
        // Reference https://msdn.microsoft.com/library/windows/apps/jj207045(v=vs.105).aspx#BKMK_CartographicModes
        private void map1_Loaded(object sender, RoutedEventArgs e)
        {
            MapsSettings.ApplicationContext.ApplicationId = "e7cb46fd-ef6b-48e8-ae93-0b6b312f98f4";
            MapsSettings.ApplicationContext.AuthenticationToken = "3_tZpq-vuX1liM_tDbSnyg";
            map1.CartographicMode = MapCartographicMode.Terrain;
        }


        // Run once main page has loaded
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure user agrees to using location services
            if (UserPrompted == false)
            {
                MessageBox.Show("This application uses your location, please press ok, You will have an opportunity to disable location services from the settings page");
                IsolatedStorageSettings.ApplicationSettings["UserPrompted"] = true;
            }

            //Pull RSS data from website

            // WebClient is used instead of HttpWebRequest in this code sample because 
            // the implementation is simpler and easier to use, and we do not need to use 
            // advanced functionality that HttpWebRequest provides, such as the ability to send headers.

            WebClient webClient = new WebClient();

            // Subscribe to the DownloadStringCompleted event prior to downloading the RSS feed.
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadStringCompleted);

            // Download the RSS feed. DownloadStringAsync was used instead of OpenStreamAsync because we do not need 
            // to leave a stream open, and we will not need to worry about closing the channel. 
            webClient.DownloadStringAsync(new System.Uri("http://sydneymtb.org/index.php?option=com_content&view=category&id=31&format=feed&type=rss"));

            // Youtube Channel RSS
            // https://code.msdn.microsoft.com/windowsapps/Youtube-Video-Sample-f2692dc9

            // http://gdata.youtube.com/feeds/base/users/UCANUSlGDYQn5VBuMOikW77Q/uploads?v=2&client=ytapi-youtube-rss-redirect&orderby=updated&alt=rss
        }

       private IEnumerable<DependencyObject> GetVisualElements(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                yield return child;
                foreach (var descendant in GetVisualElements(child))
                {
                    yield return descendant;
                }
            }
        }


        // Event handler which runs after the feed is fully downloaded.
        private void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    // Showing the exact error message is useful for debugging. In a finalized application, 
                    // output a friendly and applicable string to the user instead. 
                    MessageBox.Show("There was an error retreiving Blog posts, there may be a network problem");
                });
            }
            else
            {
                // Save the feed into the State property in case the application is tombstoned. 
                this.State["feed"] = e.Result;

                UpdateFeedList(e.Result);

            }
        }

        // This method determines whether the user has navigated to the application after the application was tombstoned.
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // First, check whether the feed is already saved in the page state.
            if (this.State.ContainsKey("feed"))
            {
                // Get the feed again only if the application was tombstoned, which means the ListBox will be empty.
                // This is because the OnNavigatedTo method is also called when navigating between pages in your application.
                // You would want to rebind only if your application was tombstoned and page state has been lost. 
                if (feedListBox.Items.Count == 0)
                {
                    UpdateFeedList(State["feed"] as string);
                }
            }
        }

        // This method sets up the feed and binds it to our ListBox. 
        private void UpdateFeedList(string feedXML)
        {
            // Load the feed into a SyndicationFeed instance.
            StringReader stringReader = new StringReader(feedXML);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            SyndicationFeed feed = SyndicationFeed.Load(xmlReader);

            // In Windows Phone OS 7.1, WebClient events are raised on the same type of thread they were called upon. 
            // For example, if WebClient was run on a background thread, the event would be raised on the background thread. 
            // While WebClient can raise an event on the UI thread if called from the UI thread, a best practice is to always 
            // use the Dispatcher to update the UI. This keeps the UI thread free from heavy processing.
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // Bind the list of SyndicationItems to our ListBox.
                feedListBox.ItemsSource = feed.Items;

            });
        }

        // The SelectionChanged handler for the feed items 
        private void feedListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;

            if (listBox != null && listBox.SelectedItem != null)
            {
                // Get the SyndicationItem that was tapped.
                SyndicationItem sItem = (SyndicationItem)listBox.SelectedItem;

                // Set up the page navigation only if a link actually exists in the feed item.
                if (sItem.Links.Count > 0)
                {
                    // Get the associated URI of the feed item.
                    Uri uri = sItem.Links.FirstOrDefault().Uri;

                    // Create a new WebBrowserTask Launcher to navigate to the feed item. 
                    // An alternative solution would be to use a WebBrowser control, but WebBrowserTask is simpler to use. 
                    WebBrowserTask webBrowserTask = new WebBrowserTask();
                    webBrowserTask.Uri = uri;
                    webBrowserTask.Show();
                }
            }
        }

        //GeoLocation

        GeoCoordinate currentLocation = null;
        MapLayer locationLayer = null;


        private async void GetLocation()
        {

            // Get current location.
            Geolocator myGeolocator = new Geolocator();
            Geoposition myGeoposition = await myGeolocator.GetGeopositionAsync();
            Geocoordinate myGeocoordinate = myGeoposition.Coordinate;
            currentLocation = CoordinateConverter.ConvertGeocoordinate(myGeocoordinate);
        }
        private void ShowLocation()
        {

            // Create a MapOverlay with icon.
            MapOverlay myLocationOverlay = new MapOverlay();
            myLocationOverlay.Content = new Image()
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("BicycleIcon.png", UriKind.Relative)),
            };
            myLocationOverlay.PositionOrigin = new Point(0.5, 0.5);
            myLocationOverlay.GeoCoordinate = currentLocation;

            // Create a MapLayer to contain the MapOverlay.
            locationLayer = new MapLayer();
            locationLayer.Add(myLocationOverlay);

            // Add the MapLayer to the Map.
            map1.Layers.Add(locationLayer);

        }

        private async void CenterMapOnLocation()
        {
            // Get current location.
            Geolocator myGeolocator = new Geolocator();
            Geoposition myGeoposition = await myGeolocator.GetGeopositionAsync();
            Geocoordinate myGeocoordinate = myGeoposition.Coordinate;
            currentLocation = CoordinateConverter.ConvertGeocoordinate(myGeocoordinate);
            // Create a MapOverlay with icon.
            MapOverlay myLocationOverlay = new MapOverlay();
            myLocationOverlay.Content = new Image()
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("BicycleIcon.png", UriKind.Relative)),
            };
            myLocationOverlay.PositionOrigin = new Point(0.5, 0.5);
            myLocationOverlay.GeoCoordinate = currentLocation;

            // Create a MapLayer to contain the MapOverlay.
            locationLayer = new MapLayer();
            locationLayer.Add(myLocationOverlay);

            // Add the MapLayer to the Map.
            map1.Layers.Add(locationLayer);
            map1.Center = currentLocation;
            map1.SetView(currentLocation, 17.0);

        }

        // Buttons

        private async void btnEmail_Click(object sender, RoutedEventArgs e)
        {
            //If the location service is disabled or not supported

            if (LocationEnabled != true)
            {

                //Display a message

                MessageBox.Show("Location services must be enabled");

                return;

            }

            var geoLocator = new Geolocator();
            var geoPosition = await geoLocator.GetGeopositionAsync();
            var geoCoordinate = geoPosition.Coordinate;


           string Msg = "Lat=" + geoCoordinate.Latitude.ToString();

            Msg = Msg + " Long=" + geoCoordinate.Longitude.ToString();

            string Bing = geoCoordinate.Latitude.ToString();

            Bing = Bing + "~" + geoCoordinate.Longitude.ToString();

            Msg = Msg + " Click to view in Bing Maps http://www.bing.com/maps/default.aspx?v=2&FORM=LMLTCC&cp=" + Bing + "&style=r&lvl=22&tilt=-90&dir=0&alt=-1000&phx=0&phy=0&phscl=1&encType=1&sp=Point.9bjqprxkwy1f_Me____";

            EmailComposeTask emailcomposer = new EmailComposeTask();

            emailcomposer.To = "";

            emailcomposer.Subject = "My current location";

            emailcomposer.Body = Msg;

            //Show the Email application 

            emailcomposer.Show();

           
        }

        private async void btnSMS_Click(object sender, RoutedEventArgs e)
        {
            //If the location service is disabled or not supported

            if (LocationEnabled != true)
            {

                //Display a message

                MessageBox.Show("Location services must be enabled");

                return;

            }

            var geoLocator = new Geolocator();
            var geoPosition = await geoLocator.GetGeopositionAsync();
            var geoCoordinate = geoPosition.Coordinate;


            string Msg = "Lat=" + geoCoordinate.Latitude.ToString();

            Msg = Msg + " Long=" + geoCoordinate.Longitude.ToString();

            string Bing = geoCoordinate.Latitude.ToString();

            Bing = Bing + "~" + geoCoordinate.Longitude.ToString();

            Msg = Msg + " Click to view in Bing Maps http://www.bing.com/maps/default.aspx?v=2&FORM=LMLTCC&cp=" + Bing + "&style=r&lvl=22&tilt=-90&dir=0&alt=-1000&phx=0&phy=0&phscl=1&encType=1&sp=Point.9bjqprxkwy1f_Me____";

            SmsComposeTask smsComposeTask = new SmsComposeTask();

            smsComposeTask.To = "";

            smsComposeTask.Body = Msg;

            //Show the Email application 

            smsComposeTask.Show();

            
        }


        private void btnMe_Click(object sender, RoutedEventArgs e)
        {
            //If the location service is disabled or not supported

            if (LocationEnabled != true)
            {

                //Display a message

                MessageBox.Show("Location services must be enabled");

                return;

            }

            CenterMapOnLocation();

        }

        private void btnAerial_Click(object sender, RoutedEventArgs e)
        {

            map1.CartographicMode = MapCartographicMode.Aerial;

        }
        private void btnHybrid_Click(object sender, RoutedEventArgs e)
        {

            map1.CartographicMode = MapCartographicMode.Hybrid;

        }
        private void btnRaod_Click(object sender, RoutedEventArgs e)
        {

            map1.CartographicMode = MapCartographicMode.Road;

        }

        // Toggles

        private void EnableLocation_Checked(object sender, RoutedEventArgs e)
        {
            System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings["EnableLocation"] = true;
        }

        private void EnableLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings["EnableLocation"] = false;
        }

        private void Textblock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            new EmailComposeTask
            {
                Subject = "Privacy Question",
                Body = "SydneyMTB App",
                To = "admin@sydneymtb.asn.au",
            }.Show();
        }


        private void Button_Gears_1(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/GearsPage.xaml", UriKind.Relative));
        }

        private void Button_WAH_1(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/WAHPage.xaml", UriKind.Relative));
        }

        private void Button_ACC_1(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/ACCPage.xaml", UriKind.Relative));
        }

    }
}