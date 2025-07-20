using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Text.Json;
using UOTDBot;

namespace ExportData.Converters;

public class ListUInt64Converter : TypeConverter<List<ulong>>
{
    public override List<ulong>? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(List<ulong>? value, IWriterRow row, MemberMapData memberMapData)
    {
        return JsonSerializer.Serialize(value, AppJsonContext.Default.ListUInt64);
    }
}