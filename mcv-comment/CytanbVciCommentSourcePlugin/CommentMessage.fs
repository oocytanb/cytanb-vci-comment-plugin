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
            (YouTubeLiveSitePlugin.YouTubeLiveMessageType.Comment,
                fun (message: YouTubeLiveSitePlugin.IYouTubeLiveMessage)
                    (_: IMessageMetadata)
                    (_: SettingsDialogViewModel) ->
                        let m = message :?> YouTubeLiveSitePlugin.IYouTubeLiveComment
                        Some {
                            Message = m.CommentItems.ToText ();
                            Name = m.NameItems.ToText ();
                            CommentSource = "YouTubeLive";
                            Timestamp = postedDateToUnixTime m.PostedAt;
                        }
            );
            (YouTubeLiveSitePlugin.YouTubeLiveMessageType.Superchat,
                fun (message: YouTubeLiveSitePlugin.IYouTubeLiveMessage)
                    (_: IMessageMetadata)
                    (_: SettingsDialogViewModel) ->
                        let m = message :?> YouTubeLiveSitePlugin.IYouTubeLiveSuperchat
                        Some {
                            Message = m.CommentItems.ToText ();
                            Name = m.NameItems.ToText ();
                            CommentSource = "YouTubeLiveSuperchat";
                            Timestamp = postedDateToUnixTime m.PostedAt;
                        }
            );
        ]

    let private parseNicoLiveSpi text =
        Regex.Replace (text, """^/spi\s*""", "")

    let private nicoLiveMssageMap =
        dict [
            (NicoSitePlugin.NicoMessageType.Comment,
                fun (message: NicoSitePlugin.INicoMessage)
                    (_: IMessageMetadata)
                    (settings: SettingsDialogViewModel) ->
                        if settings.IsNicoLiveNormalCommentEnabled.Value then
                            let m = message :?> NicoSitePlugin.INicoComment
                            Some {
                                Message = m.Text |? "";
                                Name = m.UserName |? "";
                                CommentSource = "Nicolive";
                                Timestamp = postedDateToUnixTime m.PostedAt;
                            }
                        else
                            None
            );
            (NicoSitePlugin.NicoMessageType.Ad,
                fun (message: NicoSitePlugin.INicoMessage)
                    (_: IMessageMetadata)
                    (_: SettingsDialogViewModel) ->
                        let m = message :?> NicoSitePlugin.INicoAd
                        Some {
                            Message = m.Text |? "";
                            Name = "";
                            CommentSource = "NicoliveAd";
                            Timestamp = postedDateToUnixTime m.PostedAt;
                        }
            );
            (NicoSitePlugin.NicoMessageType.Item,
                fun (message: NicoSitePlugin.INicoMessage)
                    (_: IMessageMetadata)
                    (_: SettingsDialogViewModel) ->
                        let m = message :?> NicoSitePlugin.INicoGift
                        Some {
                            Message = m.Text |? "";
                            Name = "";
                            CommentSource = "NicoliveItem";
                            Timestamp = postedDateToUnixTime m.PostedAt;
                        }
            );
            (NicoSitePlugin.NicoMessageType.Spi,
                fun (message: NicoSitePlugin.INicoMessage)
                    (_: IMessageMetadata)
                    (_: SettingsDialogViewModel) ->
                        let m = message :?> NicoSitePlugin.INicoSpi
                        Some {
                            Message = parseNicoLiveSpi (m.Text |? "");
                            Name = "";
                            CommentSource = "NicoliveSpi";
                            Timestamp = postedDateToUnixTime m.PostedAt;
                        }
            );
            (NicoSitePlugin.NicoMessageType.Emotion,
                fun (message: NicoSitePlugin.INicoMessage)
                    (_: IMessageMetadata)
                    (_: SettingsDialogViewModel) ->
                        let m = message :?> NicoSitePlugin.INicoEmotion
                        Some {
                            Message = m.Content |? "";
                            Name = "";
                            CommentSource = "NicoliveEmotion";
                            Timestamp = postedDateToUnixTime m.PostedAt;
                        }
            );
            (NicoSitePlugin.NicoMessageType.Info,
                fun (message: NicoSitePlugin.INicoMessage)
                    (_: IMessageMetadata)
                    (_: SettingsDialogViewModel) ->
                        let m = message :?> NicoSitePlugin.INicoInfo
                        Some {
                            Message = m.Text |? "";
                            Name = "";
                            CommentSource = "NicoliveInfo";
                            Timestamp = postedDateToUnixTime m.PostedAt;
                        }
            );
        ]

    let private showroomMssageMap =
        dict [
            (ShowRoomSitePlugin.ShowRoomMessageType.Comment,
                fun (message: ShowRoomSitePlugin.IShowRoomMessage)
                    (_: IMessageMetadata)
                    (settings: SettingsDialogViewModel) ->
                        if settings.IsShowroomNormalCommentEnabled.Value then
                            let m = message :?> ShowRoomSitePlugin.IShowRoomComment
                            Some {
                                Message = m.Text |? "";
                                Name = m.UserName |? "";
                                CommentSource = "Showroom";
                                Timestamp = postedDateToUnixTime m.PostedAt;
                            }
                        else
                            None
            );
        ]

    let private applySiteMessage keyType message metadata settings (map: System.Collections.Generic.IDictionary<_, _>) =
        if map.ContainsKey keyType
        then map.Item keyType message metadata settings
        else None

    let private siteMessageMap =
        dict [
            SiteType.YouTubeLive,
                fun (message: SitePlugin.ISiteMessage)
                    (metadata: IMessageMetadata)
                    (settings: SettingsDialogViewModel) ->
                        let m = message :?> YouTubeLiveSitePlugin.IYouTubeLiveMessage
                        applySiteMessage m.YouTubeLiveMessageType m metadata settings youtubeLiveMssageMap
            SiteType.NicoLive,
                fun (message: SitePlugin.ISiteMessage)
                    (metadata: IMessageMetadata)
                    (settings: SettingsDialogViewModel) ->
                        let m = message :?> NicoSitePlugin.INicoMessage
                        applySiteMessage m.NicoMessageType m metadata settings nicoLiveMssageMap
            SiteType.ShowRoom,
                fun (message: SitePlugin.ISiteMessage)
                    (metadata: IMessageMetadata)
                    (settings: SettingsDialogViewModel) ->
                        let m = message :?> ShowRoomSitePlugin.IShowRoomMessage
                        applySiteMessage m.ShowRoomMessageType m metadata settings showroomMssageMap
        ]

    let toUserName optName (metadata: IMessageMetadata) =
        if System.String.IsNullOrEmpty(optName)
            then ""
            else
                let user = metadata.User
                if isNull user
                    then optName
                    else
                        let optNick = user.Nickname
                        if System.String.IsNullOrEmpty optNick then optName else optNick

    let toCommentMessage (message: ISiteMessage) (metadata: IMessageMetadata) (settings: SettingsDialogViewModel) =
        let t = message.SiteType
        if siteMessageMap.ContainsKey t
            then siteMessageMap.Item t message metadata settings
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
        |> Option.bind (
            fun { Message = m; Name = n; CommentSource = cs; Timestamp = ts } ->
                Some {
                    Message = m;
                    Name = toUserName n metadata;
                    CommentSource = cs;
                    Timestamp = ts
                }
            )
