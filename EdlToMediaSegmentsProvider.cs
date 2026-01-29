using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaSegments;
using Microsoft.Extensions.Logging;

namespace EdlToMediaSegments;

/// <inheritdoc />
public class EdlToMediaSegmentsProvider(
    IItemRepository itemRepository,
    ILoggerFactory loggerFactory) : IMediaSegmentProvider
{
    private readonly ILogger<EdlToMediaSegmentsProvider> logger = loggerFactory.CreateLogger<EdlToMediaSegmentsProvider>();

    /// <inheritdoc />
    public string Name => "EDL to Media Segments Provider";

    /// <inheritdoc />
    public ValueTask<bool> Supports(BaseItem item) => new(item is IHasMediaSources);

    /// <inheritdoc />
    public async Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(MediaSegmentGenerationRequest request, CancellationToken cancellationToken)
    {
        var item = itemRepository.RetrieveItem(request.ItemId);
        if (item is not IHasMediaSources mediaItem)
        {
            logger.LogDebug("Item {ItemId} is not IHasMediaSources", request.ItemId);
            return [];
        }

        // Find EDL file next to the media file.
        var edlFilePath = Path.ChangeExtension(mediaItem.Path, ".edl");
        if (!await Task.Run(() => File.Exists(edlFilePath), cancellationToken))
        {
            logger.LogDebug("EDL file {EdlFilePath} does not exist for item {ItemId}", edlFilePath, request.ItemId);
            return [];
        }
        logger.LogDebug("Found EDL file {EdlFilePath} for item {ItemId}", edlFilePath, request.ItemId);
        
        // Read EDL file and parse the segments.
        return ParseSegments(await Task.Run(() => File.ReadAllLines(edlFilePath), cancellationToken), item.Id, logger);
    }

    public static List<MediaSegmentDto> ParseSegments(string[] lines, Guid id, ILogger logger)
    {
        var segments = new List<MediaSegmentDto>();
        char[] edlseparators = [' ', '\t'];
        
        foreach (var line in lines)
        {
            var parts = line.Split(edlseparators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                logger.LogWarning("EDL line '{Line}' is not in the correct format", line);
                continue;
            }

            if (!double.TryParse(parts[0], out var start) || !double.TryParse(parts[1], out var stop) || !int.TryParse(parts[2], out var action))
            {
                logger.LogWarning("EDL line '{Line}' has invalid format. Start='{Start}', Stop='{Stop}', Action='{Action}'", line, parts[0], parts[1], parts[2]);
                continue;
            }
            
            segments.Add(new MediaSegmentDto
            {
                Id = Guid.NewGuid(),
                ItemId = id,
                Type = action switch
                {
                    // This doesn't strictly agree with the EDL spec, but it is what Jellyfin understands.
                    0 => MediaSegmentType.Intro,
                    1 => MediaSegmentType.Preview,
                    2 => MediaSegmentType.Recap,
                    3 => MediaSegmentType.Commercial,
                    4 => MediaSegmentType.Outro,
                    _ => MediaSegmentType.Unknown
                },
                StartTicks = ConvertToTicks(start),
                EndTicks = ConvertToTicks(stop)
            });
        }

        return segments;
    }

    private static long ConvertToTicks(double seconds) => (long)(seconds * TimeSpan.TicksPerSecond);
}