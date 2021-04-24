using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.Wpf;
using CinemaWPF.Annotations;
using CinemaWPF.Entities;
using CinemaWPF.models;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;

namespace CinemaWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<Movie> Movies { get; set; }

        private HttpClient _http;

        private Movie _movie;
        public Movie Movie
        {
            get => _movie;
            set
            {
                _movie = value;
                OnPropertyChanged();
            }
        }

        private string _youtubeVideo;

        ChromiumWebBrowser chrome;

        private ApiConfig _config;

        private readonly YouTubeService _youtubeService;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            _http = new HttpClient();

            Movies = new ObservableCollection<Movie>();

            PrepareApi();

            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _config.Token,
                ApplicationName = this.GetType().ToString()
            });
        }

        private void PrepareApi()
        {
            var str = string.Empty;

            var fileName = "ApiConfig.json";

            if (!File.Exists(fileName))
                return;

            using(var fs = File.OpenRead("ApiConfig.json"))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
                str = sr.ReadToEnd();

            _config = JsonConvert.DeserializeObject<ApiConfig>(str);
        }
        private void ButtonCollapse_OnClick(object sender, RoutedEventArgs e)
        {
            AnimatePanels(12, 0, Visibility.Collapsed, Visibility.Visible);
        }

        private void ButtonShowSide_OnClick(object sender, RoutedEventArgs e)
        {
            AnimatePanels(8, 4, Visibility.Visible, Visibility.Collapsed);
        }

        private void AnimatePanels(int value1, int value2, Visibility panelVisibility, Visibility buttonVisibility)
        {
            GridSearchSide.Visibility = panelVisibility;
            GridCenter.ColumnDefinitions[0].Width = new GridLength(value1, GridUnitType.Star);
            GridCenter.ColumnDefinitions[1].Width = new GridLength(value2, GridUnitType.Star);
            ButtonShowSide.Visibility = buttonVisibility;
            ButtonShowSide2.Visibility = buttonVisibility;
        }

        private void ButtonSearch_OnClick(object sender, RoutedEventArgs e)
        {
            var name = TextBoxMovieName.Text;

            if (String.IsNullOrWhiteSpace(name))
                return;

            Movies.Clear();

            
            var movies = SearchMovie(name);

            
            if (movies.Count != 0)
            {
                movies.ForEach(Movies.Add);
                Movie = Movies[0];
                AddGenrePanel();

                GridHome.Visibility = Visibility.Collapsed;
                GridMovie.Visibility = Visibility.Visible;
            }
        }
        

        private List<Movie> SearchMovie(string name)
        {
            var movies = new List<Movie>();

            var response = new HttpResponseMessage();

            var apiKey = "61aac69c";

            var plotStatus = "full";

            response = _http.GetAsync($@"https://www.omdbapi.com/?s={name}&apikey={apiKey}&plot={plotStatus}").Result;

            var str = response.Content.ReadAsStringAsync().Result;

            dynamic moviesData = JsonConvert.DeserializeObject(str);


            try
            {
                foreach (var movie in moviesData.Search)
                {
                    try
                    {
                        string imdbId = movie.imdbID;

                        var str2 = _http.GetAsync($@"http://www.omdbapi.com/?i={imdbId}&apikey={apiKey}&plot={plotStatus}").Result.Content.ReadAsStringAsync().Result;

                        dynamic mv = JsonConvert.DeserializeObject(str2);

                        string genre = mv.Genre;

                        movies.Add(new Movie()
                        {
                            Name = mv.Title,
                            Year = Convert.ToInt32(mv.Year),
                            Poster = mv.Poster,
                            Summary = mv.Plot,
                            Imdb = mv.Ratings[0].Value,
                            Genre = new List<string>(genre.Split(',').Select(g => g.Trim()).ToList()),
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return movies;
        }

        private Border CreateGenreBorder()
        {
            return new Border()
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.White,
                Width = 100,
                Height = 25,
                Margin = new Thickness(5),
                Child = new TextBlock()
                {
                    Foreground = Brushes.White,
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            };
        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ListBoxMovies_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(GridMovie.Visibility == Visibility.Collapsed)
            {
                CollapseGridTrailer();
            }

            Movie = ListBoxMovies.SelectedItem as Movie;

            AddGenrePanel();
        }

        private void AddGenrePanel()
        {
            PanelGenre.Children.Clear();

            Movie.Genre.ForEach((genre) =>
            {
                var border = CreateGenreBorder();

                var textBlock = border.Child as TextBlock;

                textBlock.Text = genre;

                PanelGenre.Children.Add(border);
            });
        }

        [STAThread]
        private async void ButtonPlay_OnClick(object sender, RoutedEventArgs e)
        {
            var keyword = String.Format("{0} {1} trailer", Movie.Name, Movie.Year);

            
            await Search(keyword);

            LoadTrailer();

            GridMovie.Visibility = Visibility.Collapsed;
            GridMix.Visibility = Visibility.Visible;
            GridTrailer.Visibility = Visibility.Visible;
        }

        private void LoadTrailer()
        {
            ChromiumBrowser.Address = $@"https://www.youtube.com/embed/{_youtubeVideo}";
        }

        private async Task Search(string keyword)
        {

            var searchListRequest = _youtubeService.Search.List("snippet");
            searchListRequest.Q = keyword; // Replace with your search term.
            searchListRequest.MaxResults = 1;

            // Call the search.list method to retrieve results matching the specified query term.

            var searchListResponse = await searchListRequest.ExecuteAsync();

            List<string> videos = new List<string>();
            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(searchResult.Id.VideoId);
                        break;
                    default:
                        break;
                }
            }

            _youtubeVideo = videos[0];
        }

        private void ButtonBack_OnClick(object sender, RoutedEventArgs e)
        {
            CollapseGridTrailer();
        }

        private void CollapseGridTrailer()
        {
            ChromiumBrowser.Address = string.Empty;
            ChromiumBrowser.Reload();
            GridMovie.Visibility = Visibility.Visible;
            GridMix.Visibility = Visibility.Collapsed;
            GridTrailer.Visibility = Visibility.Collapsed;
        }
    }
}
