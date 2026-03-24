namespace DevSweep.Infrastructure.Cli.Output.JsonModels;

internal sealed record LogLineJson(string Level, string Message);

internal sealed record BannerJson(string Event, string Version);

internal sealed record SectionJson(string Event, string Title);
