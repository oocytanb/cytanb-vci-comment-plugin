// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2020 oO (https://github.com/oocytanb)
// The plugin interface is derived from [MultiCommentViewer | Copyright (c) ryu-s](https://github.com/CommentViewerCollection/MultiCommentViewer)

namespace CytanbVciCommentSourcePlugin

open System.Text.RegularExpressions

open PluginCommon
open SitePlugin

open Cytanb.Util

type CommentMessage = {
    Message: string;
    Name: string;
    CommentSource: string;
    Timestamp: int64;
}

module CommentMessageConverter =
    let private nowUnixTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds

    let private postedDateToUnixTime date =
        if date = Unchecked.defaultof<System.DateTime>
        then
            nowUnixTime ()
        else
            (System.DateTimeOffset date).ToUnixTimeSeconds ()

    let siteTypeToCommentSource siteType =
        match siteType with
        | SiteType.NicoLive -> "Nicolive"
        | SiteType.ShowRoom -> "Showroom"
        | _ -> System.Enum.GetName (typeof<SiteType>, siteType)

    let private youtubeLiveMssageMap =
        dict [
            (YouTubeLiveSitePlugin.YouTubeLiveMessageType.Comment, fun (message: YouTubeLiveSitePlugin.IYouTubeLiveMessage) ->
                let m = message :?> YouTubeLiveSitePlugin.IYouTubeLiveComment
                Some {
                    Message = m.CommentItems.ToText ();
                    Name = m.NameItems.ToText ();
                    CommentSource = "YouTubeLive";
                    Timestamp = postedDateToUnixTime m.PostedAt;
                }
            );
            (YouTubeLiveSitePlugin.YouTubeLiveMessageType.Superchat, fun (message: YouTubeLiveSitePlugin.IYouTubeLiveMessage) ->
                let m = message :?> YouTubeLiveSitePlugin.IYouTubeLiveSuperchat
                Some {
                    Message = m.CommentItems.ToText ();
                    Name = m.NameItems.ToText ();
                    CommentSource = "YouTubeLiveSuperchat";
                    Timestamp = postedDateToUnixTime m.PostedAt;
                }
            );
        ]

    let private parseNicoLiveItem text =
        Regex.Replace (text, """^/gift\s+([\w]+\s+((\d+|NULL)\s+)?)?""", "")

    let private nicoLiveMssageMap =
        dict [
            (NicoSitePlugin.NicoMessageType.Comment, fun (message: NicoSitePlugin.INicoMessage) ->
                let m = message :?> NicoSitePlugin.INicoComment
                let chat = NicoSitePlugin.Chat.Parse m.Raw
                let premium = chat.Premium

                if premium.HasValue
                then
                    let p = premium.Value
                    // 1: Premium user, 3: Broadcaster (Need to confirm)
                    if p = 2 || p = 3 || p = 6 || p = 7
                    then
                        Some {
                            Message = m.Text |? "";
                            Name = m.UserName |? "";
                            CommentSource = "NicoliveBroadcaster";
                            Timestamp = postedDateToUnixTime m.PostedAt;
                        }
                    else None
                else None
            );
            (NicoSitePlugin.NicoMessageType.Item, fun (message: NicoSitePlugin.INicoMessage) ->
                let m = message :?> NicoSitePlugin.INicoItem
                Some {
                    Message = parseNicoLiveItem (m.Text |? "");
                    Name = "";
                    CommentSource = "NicoliveItem";
                    Timestamp = postedDateToUnixTime m.PostedAt;
                }
            );
            (NicoSitePlugin.NicoMessageType.Ad, fun (message: NicoSitePlugin.INicoMessage) ->
                let m = message :?> NicoSitePlugin.INicoAd
                Some {
                    Message = m.Text |? "";
                    Name = "";
                    CommentSource = "NicoliveAd";
                    Timestamp = postedDateToUnixTime m.PostedAt;
                }
            );
            (NicoSitePlugin.NicoMessageType.Info, fun (message: NicoSitePlugin.INicoMessage) ->
                let m = message :?> NicoSitePlugin.INicoInfo
                Some {
                    Message = m.Text |? "";
                    Name = "";
                    CommentSource = "NicoliveInfo";
                    Timestamp = postedDateToUnixTime m.PostedAt;
                }
            );
        ]

    let private applySiteMessage keyType message (map: System.Collections.Generic.IDictionary<_, _>) =
        if map.ContainsKey keyType
        then map.Item keyType <| message
        else None

    let private siteMessageMap =
        dict [
            SiteType.YouTubeLive, fun (message: SitePlugin.ISiteMessage) ->
                let m = message :?> YouTubeLiveSitePlugin.IYouTubeLiveMessage
                applySiteMessage m.YouTubeLiveMessageType m youtubeLiveMssageMap
            SiteType.NicoLive, fun (message: SitePlugin.ISiteMessage) ->
                let m = message :?> NicoSitePlugin.INicoMessage
                applySiteMessage m.NicoMessageType m nicoLiveMssageMap
            SiteType.ShowRoom, fun (message: SitePlugin.ISiteMessage) -> None;
        ]

    let toCommentMessage (message: SitePlugin.ISiteMessage) =
        let t = message.SiteType
        if siteMessageMap.ContainsKey t
        then siteMessageMap.Item t <| message
        else
            match Tools.GetData message with
            | struct(null, null) -> None
            | struct(optName, optComment) ->
                Some {
                    Message = optComment |? "";
                    Name = optName |? "";
                    CommentSource = siteTypeToCommentSource message.SiteType;
                    Timestamp = nowUnixTime ();
                }
