using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace ExportData.Converters;

public class BooleanConverter : TypeConverter<bool>
{
    public override bool ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(bool value, IWriterRow row, MemberMapData memberMapData)
    {
        return value ? "1" : "0";
    }
}