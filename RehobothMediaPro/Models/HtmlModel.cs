using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RehobothMediaPro.Models
{
    public class HtmlModel
    {
        public string Lyrics { get; set; }
        // NEW: Stores the perfect full-paragraph HTML for the second monitor!
        public string PresentationLyrics { get; set; }
        public string Header { get; set; }
        public string FontSize { get; set; }
        public bool isVerseAvailable { get; set; } = true;
    }
}