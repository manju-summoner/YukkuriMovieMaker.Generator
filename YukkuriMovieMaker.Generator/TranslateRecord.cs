using CsvHelper.Configuration.Attributes;

#pragma warning disable RS1036
namespace YukkuriMovieMaker.Generator
{
    class TranslateRecord
    {
        const string jajp = "ja-jp";
        const string enus = "en-us";
        const string zhcn = "zh-cn";
        const string zhtw = "zh-tw";
        const string kokr = "ko-kr";
        const string eses = "es-es";
        const string arsa = "ar-sa";
        const string idid = "id-id";
        public static string[] LangCodes = [jajp, enus, zhcn, zhtw, kokr, eses, arsa, idid];

        [Index(0)]
        public string? Key { get; set; }

        [Index(1)]
        public string? Comment { get; set; }

        [Index(2)]
        public string? Japanese { get; set; }

        [Index(3)]
        public string? English { get; set; }

        [Index(4)]
        public string? SimplifiedChinese { get; set; }

        [Index(5)]
        public string? TraditionalChinese { get; set; }

        [Index(6)]
        public string? Korean { get; set; }

        [Index(7)]
        public string? Spanish { get; set; }

        [Index(8)]
        public string? Arabic { get; set; }

        [Index(9)]
        public string? BahasaIndonesia { get; set; }

        [Index(10)]
        public string? Unknown { get; set; }

        public string? GetValue(string langCode)
        {
            return langCode switch
            {
                jajp => Japanese,
                enus => English,
                zhcn => SimplifiedChinese,
                zhtw => TraditionalChinese,
                kokr => Korean,
                eses => Spanish,
                arsa => Arabic,
                idid => BahasaIndonesia,
                _ => Unknown
            };
        }
    }
}
