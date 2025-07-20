using CsvHelper;
using ExportData;
using ExportData.Mappings;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

if (args.Length != 1)
{
    Console.WriteLine("Usage: ExportData <connection-string>");
    return;
}

using var db = new ExportAppDbContext(args[0]);

await using (var mapsWriter = new StreamWriter("Maps.csv"))
using (var csv = new CsvWriter(mapsWriter, CultureInfo.InvariantCulture))
{
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.TimeInt32Converter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.DateTimeOffsetConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.DateOnlyConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.MapFeaturesConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.NullableConverter<int>());

    csv.WriteRecords(db.Maps);
}

await using (var reportChannelsWriter = new StreamWriter("ReportChannels.csv"))
using (var csv = new CsvWriter(reportChannelsWriter, CultureInfo.InvariantCulture))
{
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.DateTimeOffsetConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.ReportConfigurationConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.BooleanConverter());

    csv.WriteRecords(db.ReportChannels.Include(x => x.Configuration));
}

await using (var reportUsersWriter = new StreamWriter("ReportUsers.csv"))
using (var csv = new CsvWriter(reportUsersWriter, CultureInfo.InvariantCulture))
{
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.DateTimeOffsetConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.ReportConfigurationConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.BooleanConverter());

    csv.WriteRecords(db.ReportUsers.Include(x => x.Configuration));
}

await using (var reportMessagesWriter = new StreamWriter("ReportMessages.csv"))
using (var csv = new CsvWriter(reportMessagesWriter, CultureInfo.InvariantCulture))
{
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.DateTimeOffsetConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.BooleanConverter());
    csv.Context.RegisterClassMap<ReportMessageMap>();

    csv.WriteRecords(db.ReportMessages.Include(x => x.Map).Include(x => x.Channel).Include(x => x.DM));
}

await using (var reportConfigurationWriter = new StreamWriter("ReportConfiguration.csv"))
using (var csv = new CsvWriter(reportConfigurationWriter, CultureInfo.InvariantCulture))
{
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.DictionaryStringStringConverter());
    csv.Context.TypeConverterCache.AddConverter(new ExportData.Converters.ListUInt64Converter());

    csv.WriteRecords(db.ReportConfiguration);
}