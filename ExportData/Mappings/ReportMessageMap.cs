using CsvHelper.Configuration;
using ExportData.Converters;
using UOTDBot.Models;

namespace ExportData.Mappings;

internal class ReportMessageMap : ClassMap<ReportMessage>
{
    public ReportMessageMap()
    {
        Map(m => m.Id).Index(0);
        Map(m => m.MessageId).Index(1);
        Map(m => m.OriginalChannelId).Index(2).TypeConverter<NullableConverter<ulong>>();
        Map(m => m.Map.Id).Index(3).Name("MapId");
        Map(m => m.Channel.Id).Index(4).Name("ChannelId");
        Map(m => m.DM.Id).Index(5).Name("DMId");
        Map(m => m.CreatedAt).Index(6);
        Map(m => m.UpdatedAt).Index(7);
        Map(m => m.IsDeleted).Index(8);
    }
}
