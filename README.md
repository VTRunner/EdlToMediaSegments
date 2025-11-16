# EDL to Media Segments Plugin

This plugin reads EDL files next to your media files and adds them as Media
Segments automatically.

This is my solution for https://features.jellyfin.org/posts/180/support-for-edl-comskip-files

## Installation Instructions

1. Add this plugin repository to your server:

In Jellyfin, go to http://localhost:8096/web/#/dashboard/plugins/repositories and add

`https://raw.githubusercontent.com/vtrunner/EdlToMediaSegments/refs/heads/main/manifest.json`

2. Install the EdlToMediaSegments plugin from the Metadata section.
3. Restart Jellyfin.

## How to Use

Next to any media file you wish to generate skips for, add a .edl file with the same
basename. For example, say you have /path/to/Movie.mp4, then create /path/to/Movie.edl.

The contents of the EDL file are lines following the pattern of:

```
start stop type
```

Where `start` is the start time in seconds (e.g. 510.333), `stop` is the stop time in
seconds, and `type` is the type of the media segment, as follows:

0 - Intro
1 - Preview
2 - Recap
3 - Commercial
4 - Outro

For example, a file to skip a 1:30 intro and then a commercial break from 4:30.5 to 5:00 would
look like:

```
0 90 0
270.5 300 3
```

Once you have installed the plugin and added your .edl files, your jellyfin server should
scan for media segments automatically, but you can force it to do so by going to the
Dashboard > ScheduledTasks and click the play button next to "Media Segment Scan"
