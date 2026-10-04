using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum AttributionCategory
    {
        Music,
        SoundEffects,
        Fonts
    }

    public sealed class ThirdPartyAttributionEntry
    {
        public AttributionCategory Category;
        public string Title;
        public string Credit;
        public string License;
        public string PurposeChinese;
        public string PurposeEnglish;
        public string SourceUrl;
        public string LicenseUrl;
    }

    public static class ThirdPartyAttributionCatalog
    {
        private static readonly IReadOnlyList<ThirdPartyAttributionEntry> entries =
            new[]
            {
                Entry(AttributionCategory.Music, "Kingdom Quest",
                    "by Eric Matyas · © Eric Matyas · www.soundimage.org",
                    "Soundimage International Public License",
                    "主页及通用页面循环背景音乐", "Looping home and general-screen background music",
                    "https://soundimage.org/fantasy-11/", "https://soundimage.org/sample-page/"),
                Entry(AttributionCategory.Music, "The Voyage Begins",
                    "by Eric Matyas · © Eric Matyas · www.soundimage.org · excerpted, crossfaded and Ogg encoded",
                    "Soundimage International Public License",
                    "远征探索循环背景音乐（取段、交叉淡化及 Ogg 转码）",
                    "Looping expedition music (excerpted, crossfaded and Ogg encoded)",
                    "https://soundimage.org/epic-battle/", "https://soundimage.org/sample-page/"),
                Entry(AttributionCategory.Music, "Training",
                    "by Eric Matyas · © Eric Matyas · www.soundimage.org",
                    "Soundimage International Public License",
                    "仅实际战斗使用的循环背景音乐", "Looping music used only during active battles",
                    "https://soundimage.org/sports/", "https://soundimage.org/sample-page/"),

                Entry(AttributionCategory.SoundEffects, "switch18.ogg",
                    "Kenney · UI Audio", "CC0 1.0",
                    "界面主点击音", "Primary UI click",
                    "https://kenney.nl/assets/ui-audio", "https://kenney.nl/support"),
                Entry(AttributionCategory.SoundEffects, "switch35.ogg",
                    "Kenney · UI Audio", "CC0 1.0",
                    "取消与返回音", "Cancel and back UI cue",
                    "https://kenney.nl/assets/ui-audio", "https://kenney.nl/support"),
                Entry(AttributionCategory.SoundEffects, "Dagger woosh",
                    "Mixkit", "Mixkit Sound Effects Free License",
                    "通用近战动作音", "Generic melee action cue",
                    "https://mixkit.co/free-sound-effects/woosh/", "https://mixkit.co/license/"),
                Entry(AttributionCategory.SoundEffects, "Metal arrow fast hit",
                    "Mixkit", "Mixkit Sound Effects Free License",
                    "通用远程命中音", "Generic ranged-hit cue",
                    "https://mixkit.co/free-sound-effects/medieval-battle/", "https://mixkit.co/license/"),
                Entry(AttributionCategory.SoundEffects, "Quick magic sword slice",
                    "Mixkit", "Mixkit Sound Effects Free License",
                    "普通治疗动作音", "Basic healing-action cue",
                    "https://mixkit.co/free-sound-effects/sword/", "https://mixkit.co/license/"),
                Entry(AttributionCategory.SoundEffects, "Whoosh Short Realistic",
                    "Pixabay", "Pixabay Content License",
                    "通用技能释放音", "Generic skill-release cue",
                    "https://pixabay.com/sound-effects/film-special-effects-whoosh-short-realistic-379079/",
                    "https://pixabay.com/service/license-summary/"),
                Entry(AttributionCategory.SoundEffects, "Stone on Stone",
                    "Fenodyrie (Freesound) · Pixabay", "Pixabay Content License",
                    "布阵底座落位音", "Formation placement cue",
                    "https://pixabay.com/sound-effects/film-special-effects-stone-on-stone-62692/",
                    "https://pixabay.com/service/license-summary/"),

                Entry(AttributionCategory.Fonts, "Source Han Serif SC Regular",
                    "Adobe Fonts", "SIL Open Font License 1.1",
                    "简体中文正文与界面字体", "Simplified Chinese body and UI font",
                    "https://github.com/adobe-fonts/source-han-serif/", "https://openfontlicense.org/"),
                Entry(AttributionCategory.Fonts, "Source Han Serif SC SemiBold",
                    "Adobe Fonts", "SIL Open Font License 1.1",
                    "简体中文标题与强调字体", "Simplified Chinese heading and emphasis font",
                    "https://github.com/adobe-fonts/source-han-serif/", "https://openfontlicense.org/")
            };

        public static IReadOnlyList<ThirdPartyAttributionEntry> Entries
        {
            get { return entries; }
        }

        private static ThirdPartyAttributionEntry Entry(AttributionCategory category,
            string title, string credit, string license, string purposeChinese,
            string purposeEnglish, string sourceUrl, string licenseUrl)
        {
            return new ThirdPartyAttributionEntry
            {
                Category = category,
                Title = title,
                Credit = credit,
                License = license,
                PurposeChinese = purposeChinese,
                PurposeEnglish = purposeEnglish,
                SourceUrl = sourceUrl,
                LicenseUrl = licenseUrl
            };
        }
    }
}
