using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace ExportData.Converters;

public class DateOnlyConverter : TypeConverter<DateOnly>
{
    public override DateOnly ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(DateOnly value, IWriterRow row, MemberMapData memberMapData)
    {
        return value.ToString("yyyy-MM-dd");
    }
}