using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using TmEssentials;

namespace ExportData.Converters;

public class DateTimeOffsetConverter : TypeConverter<DateTimeOffset>
{
    public override DateTimeOffset ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(DateTimeOffset value, IWriterRow row, MemberMapData memberMapData)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
    }
}