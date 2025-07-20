using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Text.Json;
using UOTDBot;
using UOTDBot.Models;

namespace ExportData.Converters;

public class MapFeaturesConverter : TypeConverter<MapFeatures>
{
    public override MapFeatures ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(MapFeatures? value, IWriterRow row, MemberMapData memberMapData)
    {
        return JsonSerializer.Serialize(value, AppJsonContext.Default.MapFeatures);
    }
}