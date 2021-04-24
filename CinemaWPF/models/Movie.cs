using System.Collections.Generic;
using System.Windows.Documents;

namespace CinemaWPF.models
{
    public class Movie
    {
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Poster { get; set; }
        public int Year { get; set; }
        public string Imdb { get; set; }

        public List<string> Genre { get; set; }

        public Movie()
        {
            Genre = new List<string>();
        }
    }
}