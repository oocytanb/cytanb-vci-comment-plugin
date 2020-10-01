namespace CytanbVciCommentSourcePlugin

open System.ComponentModel.Composition
open System.IO
open FSharp.Control.Reactive

open Plugin

open Cytanb
open Cytanb.Util

[<Export(typeof<IPlugin>)>]
type CommentSourcePlugin () =
    static let commentMessageBufferSize = 64

    static let updateDue = System.TimeSpan.FromSeconds 20.

    static let pastCommentTime = System.TimeSpan.FromMinutes 10.

    static let assemblyResolver =
        let fsharpCoreName = "FSharp.Core"

        AssemblyVersionResolver.register [
            (
                new System.Reflection.AssemblyName (Name = fsharpCoreName),
                new System.Reflection.AssemblyName (Name = fsharpCoreName)
            )
        ]

    static let resources =
        let asm = System.Reflection.Assembly.GetExecutingAssembly ()
        System.Resources.ResourceManager ("CytanbVciCommentSourcePlugin.resources.Resource", asm)

    let timestampThreshold = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds () - (int64 pastCommentTime.TotalSeconds)

    let settingsVM = new SettingsDialogViewModel ()

    let mutable settingsFilePath = None

    let mutable dialog = None

    let messageStream = Subject<CommentMessage>.broadcast

    let messageDisposable =
        let hotStream = 
            Observable.publish messageStream
            |> Observable.refCount

        let closingStream = 
            hotStream
            |> throttleTrailing updateDue

        hotStream
        |> Observable.buffer closingStream
        |> Observable.map
            (fun list ->
                let skipCount = (Seq.length list) - commentMessageBufferSize
                if skipCount > 0
                then Seq.skip skipCount list
                else list :> seq<_>
            )
        |> Observable.filter (Seq.isEmpty >> not)
        |> Observable.subscribe (fun entries ->
            try
                LuaScriptIO.write entries settingsVM.OutputFilePath.Value
            with
                | exn ->
                    settingsVM.StatusMessage.Value <-
                        sprintf "%s : %s"
                        <| resources.GetString("WriteFileError")
                        <| exn.Message
        )

    let disposable =
        Disposables.compose [
            // Plugin instance can dispose assemblyResolver since that only needed once.
            assemblyResolver;
            messageStream;
            messageDisposable;
        ]

    let mutable disposed = false

    let internalDispose disposing =
        if not disposed then
            if disposing then
                // Dispose managed resources.
                disposable.Dispose ()

            // Dispose unmanaged resources.
            disposed <- true

    interface IPlugin with
        override this.Name = "cytanb-vci-comment-source"

        override this.Description = ""

        override val Host = null with get, set

        override this.OnMessageReceived (message, messageMetadata) =
            if settingsVM.IsEnabled.Value then
                Option.iter
                <| fun (m: CommentMessage) ->
                    if m.Timestamp >= timestampThreshold then
                        Subject.onNext m messageStream |> ignore
                <| CommentMessageConverter.toCommentMessage message

        override this.OnLoaded () =
            try
                let p = this :> IPlugin
                let file = Path.Combine (p.Host.SettingsDirPath, p.Name + ".json")
                settingsFilePath <- Some file

                if File.Exists file then
                    use reader = new StreamReader (file)
                    settingsVM.Deserialize reader
            with
                | exn -> dprintfn "Failed to load settings: %A" exn

        override this.OnClosing () =
            Option.iter<SettingsDialog>
            <| fun d ->
                d.FinalClose ()
                dialog <- None
            <| dialog

            Option.iter
            <| fun (file: string) ->
                try
                    use writer = new StreamWriter (file)
                    settingsVM.Serialize writer
                with
                    | exn -> dprintfn "Failed to save settings: %A" exn
            <| settingsFilePath

        override this.ShowSettingView () =
            let d =
                match dialog with
                | None ->
                    let d = new SettingsDialog (DataContext = settingsVM)
                    let host = (this :> IPlugin).Host
                    d.Topmost <- host.IsTopmost
                    d.Top <- host.MainViewTop
                    d.Left <- host.MainViewLeft
                    dialog <- Some d
                    d
                | Some d -> d
            d.Show ()
            d.Focus () |> ignore

        override this.OnTopmostChanged (isTopmost) =
            Option.iter<SettingsDialog> (fun d ->
                d.Topmost <- isTopmost
            ) dialog

    interface System.IDisposable with
        override this.Dispose () =
            internalDispose true
            System.GC.SuppressFinalize (this)
    
    override this.Finalize () = internalDispose false
