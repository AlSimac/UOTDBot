using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Text.Json;
using UOTDBot;

namespace ExportData.Converters;

public class DictionaryStringStringConverter : TypeConverter<Dictionary<string, string>>
{
    public override Dictionary<string, string>? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(Dictionary<string, string>? value, IWriterRow row, MemberMapData memberMapData)
    {
        return JsonSerializer.Serialize(value, AppJsonContext.Default.DictionaryStringString);
    }
}