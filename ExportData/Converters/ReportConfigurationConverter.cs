using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using UOTDBot.Models;

namespace ExportData.Converters;

public class ReportConfigurationConverter : TypeConverter<ReportConfiguration>
{
    public override ReportConfiguration ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        throw new NotImplementedException();
    }

    public override string? ConvertToString(ReportConfiguration? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value?.Id.ToString() ?? "NULL";
    }
}