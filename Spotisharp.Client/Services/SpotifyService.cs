﻿using SpotifyAPI.Web;
using Spotisharp.Client.ColoredConsole;
using Spotisharp.Client.Enums;
using Spotisharp.Client.Models;
using System.Collections.Concurrent;

namespace Spotisharp.Client.Services;

public static class SpotifyService
{
    public static async Task PackSingleTrack
    (
        SpotifyClient client,
        string input,
        ConcurrentBag<TrackInfoModel> bag,
        SpotifyUriType spotifyUriType = SpotifyUriType.None
    )
    {
        int topCursorPosition = Console.CursorTop;
        FullTrack? track = null;

        if (spotifyUriType == SpotifyUriType.None)
        {
            try
            {
                SearchResponse searchResponse = await client.Search.Item
                (
                    new SearchRequest
                    (
                        SearchRequest.Types.Track,
                        input
                    )
                );

                if (searchResponse.Tracks.Items != null)
                {
                    if (searchResponse.Tracks.Items.Count > 0)
                    {
                        track = searchResponse.Tracks.Items[0];
                    }
                }
            }
            catch (APIException) { };
        }
        else
        {
            track = await client.Tracks.Get(input);
        }

        if (track != null)
        {
            FullAlbum album = await client.Albums.TryGet(track.Album.Id);
            FullArtist artist = await client.Artists.TryGet(album.Artists[0].Id);
            List<FullArtist> artists = new List<FullArtist>();
            foreach (var artistx in album.Artists)
            {
                FullArtist fullArtist = await client.Artists.TryGet(artistx.Id);
                artists.Add(fullArtist);
            }
            TrackInfoModel trackInfo = new TrackInfoModel()
            {
                Artist = string.Join(" & ", artists.Select(a => a.Name)),
                Title = track.Name,
                Url = track.ExternalUrls["spotify"],
                Playlist = "Unknown",
                DiscNumber = track.DiscNumber,
                TrackNumber = track.TrackNumber,
                Album = album.Name,
                AlbumPicture = album.Images[0].Url,
                Copyright = album.Label ?? string.Empty,
                Genres = artist.Genres.FirstOrDefault() ?? string.Empty,
                Year = DateTime.TryParse(album.ReleaseDate, out var value) ? value.Year : int.Parse(album.ReleaseDate),
                ReleaseDate = DateTime.Parse(album.ReleaseDate),
                DurationMS = track.DurationMs

            };
            bag.Add(trackInfo);
            CConsole.Overwrite
            (
                $"Q: {bag.Count} | {trackInfo}",
                topCursorPosition
            );
        }
        else
        {
            CConsole.Overwrite
            (
                $"Spotify returned empty search list",
                topCursorPosition,
                CConsoleType.Error
            );
        }
    }

    public static async Task PackPlaylistTracks(
        SpotifyClient client,
        string input,
        ConcurrentBag<TrackInfoModel> bag
    )
    {
        int topCursorPosition = Console.CursorTop;
        FullPlaylist playlist = await client.Playlists.Get(input);
        if (playlist.Tracks != null)
        {
            await foreach (var item in client.Paginate(playlist.Tracks))
            {
                if (item.Track is FullTrack track && !string.IsNullOrEmpty(track.Album?.Id))
                {
                    try
                    {
                        if (item.Track == null)
                        {
                            continue;
                        }
                        FullAlbum album = await client.Albums.TryGet(track.Album.Id);
                        FullArtist artist = await client.Artists.TryGet(album.Artists[0].Id);
                        List<FullArtist> artists = new List<FullArtist>();
                        foreach (var artistx in album.Artists)
                        {
                            FullArtist fullArtist = await client.Artists.TryGet(artistx.Id);
                            artists.Add(fullArtist);
                        }
                        TrackInfoModel trackInfo = new TrackInfoModel()
                        {
                            Artist = string.Join(" & ", artists.Select(a => a.Name)),
                            Title = track.Name,
                            Url = track.ExternalUrls["spotify"],
                            Playlist = playlist.Name ?? string.Empty,
                            DiscNumber = track.DiscNumber,
                            TrackNumber = track.TrackNumber,
                            Album = album.Name,
                            AlbumPicture = album.Images[0].Url,
                            Copyright = album.Label ?? string.Empty,
                            Genres = artist.Genres.FirstOrDefault() ?? string.Empty,
                            Year = DateTime.TryParse(album.ReleaseDate, out var value) ? value.Year : int.Parse(album.ReleaseDate),
                            ReleaseDate = DateTime.Parse(album.ReleaseDate),
                            DurationMS = track.DurationMs

                        };
                        bag.Add(trackInfo);
                        CConsole.Overwrite
                        (
                            $"Q: {bag.Count} A: {playlist.Tracks.Total} | {trackInfo}",
                            topCursorPosition
                        );
                    }
                    catch (Exception)
                    {

                    }

                }
            }
        }
    }

    public static async Task PackAlbumTracks(
        SpotifyClient client,
        string input,
        ConcurrentBag<TrackInfoModel> bag
    )
    {
        int topCursorPosition = Console.CursorTop;
        FullAlbum album = await client.Albums.Get(input);
        FullArtist artist = await client.Artists.TryGet(album.Artists[0].Id);
        List<FullArtist> artists = new List<FullArtist>();
        foreach (var artistx in album.Artists)
        {
            FullArtist fullArtist = await client.Artists.TryGet(artistx.Id);
            artists.Add(fullArtist);
        }
        await foreach (SimpleTrack track in client.Paginate(album.Tracks))
        {
            TrackInfoModel trackInfo = new TrackInfoModel()
            {
                Artist = string.Join(" & ", artists.Select(a => a.Name)),
                Title = track.Name,
                Url = track.ExternalUrls["spotify"],
                Playlist = album.Name ?? string.Empty,
                DiscNumber = track.DiscNumber,
                TrackNumber = track.TrackNumber,
                Album = album.Name ?? string.Empty,
                AlbumPicture = album.Images[0].Url,
                Copyright = album.Label ?? string.Empty,
                Genres = artist.Genres.FirstOrDefault() ?? string.Empty,
                Year = DateTime.TryParse(album.ReleaseDate, out var value) ? value.Year : int.Parse(album.ReleaseDate),
                ReleaseDate = DateTime.Parse(album.ReleaseDate),
                DurationMS = track.DurationMs

            };
            bag.Add(trackInfo);
            CConsole.Overwrite
            (
                $"Q: {bag.Count} A: {album.Tracks.Total} | {trackInfo}",
                topCursorPosition
            );
        }
    }

    private static async Task<FullAlbum> TryGet(this IAlbumsClient client, string albumid)
    {
        FullAlbum album;
    TryAgain:
        try
        {
            album = await client.Get(albumid);
            return album;
        }
        catch (APITooManyRequestsException ex)
        {
            await Task.Delay((int)(ex.RetryAfter.TotalMilliseconds));
            goto TryAgain;
        }
    }

    private static async Task<FullArtist> TryGet(this IArtistsClient client, string artistid)
    {
    TryAgain:
        try
        {
            FullArtist _ = await client.Get(artistid);
            return _;
        }
        catch (APITooManyRequestsException ex)
        {
            await Task.Delay((int)(ex.RetryAfter.TotalMilliseconds));
            goto TryAgain;
        }
    }
}