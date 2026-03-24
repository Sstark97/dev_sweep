using System.Text.Json.Serialization;
using DevSweep.Infrastructure.Cli.Output.JsonModels;

namespace DevSweep.Infrastructure.Cli.Output;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false)]
[JsonSerializable(typeof(AnalysisReportJson))]
[JsonSerializable(typeof(CleanupCompletionJson))]
[JsonSerializable(typeof(LogLineJson))]
[JsonSerializable(typeof(BannerJson))]
[JsonSerializable(typeof(SectionJson))]
internal sealed partial class DevSweepJsonContext : JsonSerializerContext
{
}
