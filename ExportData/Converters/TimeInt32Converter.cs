using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using TmEssentials;

namespace ExportData.Converters;

public class TimeInt32Converter : TypeConverter<TimeInt32>
{
    public override TimeInt32 ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(TimeInt32 value, IWriterRow row, MemberMapData memberMapData)
    {
        return value.TotalMilliseconds.ToString();
    }
}