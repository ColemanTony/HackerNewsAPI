using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HackerNewsAPI.Models
{
    public class NewsStory
    {
        public string Title { get; set; }

        public string By { get; set; }

        public string Url { get; set; }

        public int Score { get; set; }

        //public DateTime Time { get; set; }
    }
}
