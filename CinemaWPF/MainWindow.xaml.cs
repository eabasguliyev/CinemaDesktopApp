using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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
using CinemaWPF.models;
using Newtonsoft.Json;

namespace CinemaWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Movie> Movies { get; set; }

        private HttpClient _http;
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            _http = new HttpClient();

            Movies = new ObservableCollection<Movie>();
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
        }

        private void ButtonSearch_OnClick(object sender, RoutedEventArgs e)
        {
            var name = TextBoxMovieName.Text;

            if (String.IsNullOrWhiteSpace(name))
                return;

            Movies.Clear();

            SearchMovie(name).ForEach(Movies.Add);
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


            foreach (var movie in moviesData.Search)
            {
                try
                {
                    string imdbId = movie.imdbID;

                    var str2 = _http.GetAsync($@"http://www.omdbapi.com/?i={imdbId}&apikey={apiKey}&plot={plotStatus}").Result.Content.ReadAsStringAsync().Result;

                    dynamic mv = JsonConvert.DeserializeObject(str2);

                    movies.Add(new Movie()
                    {
                        Name = mv.Title,
                        Year = Convert.ToInt32(mv.Year),
                        Poster = mv.Poster,
                        Summary = mv.Plot,
                    });
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            return movies;
        }
    }
}
