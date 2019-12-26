﻿using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class GeneralSettings : MoongladeSettings
    {
        public string SiteTitle { get; set; }

        public string LogoText { get; set; }

        public string MetaKeyword { get; set; }

        public string MetaDescription { get; set; }

        public string Copyright { get; set; }

        public string SideBarCustomizedHtmlPitch { get; set; }

        public string FooterCustomizedHtmlPitch { get; set; }

        // Use string instead of TimeSpan as workaround for System.Text.Json issue
        // https://github.com/EdiWang/Moonglade/issues/310
        public string TimeZoneUtcOffset { get; set; }

        public string TimeZoneId { get; set; }

        public string ThemeFileName { get; set; }

        public GeneralSettings()
        {
            ThemeFileName = "word-blue.css";
        }
    }
}
