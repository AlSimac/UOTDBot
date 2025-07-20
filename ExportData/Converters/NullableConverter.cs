using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace ExportData.Converters;

public class NullableConverter<T> : TypeConverter<T?> where T : struct
{
    public override T? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(T? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value?.ToString() ?? "NULL";
    }
}