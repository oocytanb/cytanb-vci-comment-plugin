namespace CytanbVciCommentSourcePlugin

open System.ComponentModel
open System.IO
open Reactive.Bindings
open FSharp.Control.Reactive
open FSharp.Data

open Cytanb

type SettingsData = JsonProvider<"""
{
    "version": 1,
    "isEnabled": false,
    "outputFilePath": "main.lua",
    "isNicoLiveAndShowroomNormalCommentEnabled": true
}
""">

type SettingsDialogViewModel () =
    static let version = 1

    static let defaultIsEnabled = false

    static let scriptWorkspacePath =
        try
            match SpecialFolder.LocalAppDataLow () with
            | None -> ""
            | Some(x) -> Path.Combine (
                            x,
                            "infiniteloop Co,Ltd",
                            "VirtualCast",
                            "EmbeddedScriptWorkspace"
                         )
        with
        | :? System.ArgumentException -> ""

    static let defaultOutputFilePath =
        let mainFile = "main.lua"
        try
            Path.Combine (scriptWorkspacePath, "cytanb-comment-source", mainFile)
        with
        | _ -> mainFile

    static let defaultIsNicoLiveAndShowroomNormalCommentEnabled = true

    let isEnabledProperty = new ReactivePropertySlim<bool> (defaultIsEnabled)

    let outputFilePathProperty = new ReactivePropertySlim<string> (defaultOutputFilePath)

    let isNicoLiveAndShowroomNormalCommentEnabledProperty =
        new ReactivePropertySlim<bool> (defaultIsNicoLiveAndShowroomNormalCommentEnabled)

    let showFileDialogCommand = new ReactiveCommand ()

    let showFileDialogCommandDisposable =         
        showFileDialogCommand
        |> Observable.map (fun _ ->
            let (initialDir, fileName) =
                let filePath = outputFilePathProperty.Value
                try
                    let dirPath = Path.GetDirectoryName filePath
                    let validDirPath =
                        if Directory.Exists dirPath then
                            dirPath
                        else
                            scriptWorkspacePath

                    let fileName = Path.GetFileName filePath

                    (validDirPath, fileName)
                with
                | _ -> ("", filePath)

            use dialog =
                new System.Windows.Forms.OpenFileDialog (
                    Title = "Select Lua script file",
                    Filter = "main lua file|main.lua|All files (*.*)|*.*",
                    InitialDirectory = initialDir,
                    FileName = fileName,
                    CheckFileExists = false,
                    CheckPathExists = false,
                    RestoreDirectory = true)

            match dialog.ShowDialog () with
            | System.Windows.Forms.DialogResult.OK -> Some dialog.FileName
            | _ -> None
        )
        |> Observable.filter (fun o -> o.IsSome)
        |> Observable.map (fun o -> o.Value)
        |> Observable.subscribe (fun filePath -> outputFilePathProperty.Value <- filePath)

    let loadDefaultsCommand = new ReactiveCommand ()

    let loadDefaultsCommandDisposable =
        loadDefaultsCommand
        |> Observable.subscribe (fun _ ->
            isEnabledProperty.Value <- defaultIsEnabled
            outputFilePathProperty.Value <- defaultOutputFilePath
            isNicoLiveAndShowroomNormalCommentEnabledProperty.Value
                <- defaultIsNicoLiveAndShowroomNormalCommentEnabled
        )

    let statusMessageProperty = new ReactivePropertySlim<string> ("")

    let ev = new Event<_, _> ()

    let disposable =
        Disposables.compose [
            isEnabledProperty;
            outputFilePathProperty;
            isNicoLiveAndShowroomNormalCommentEnabledProperty;
            showFileDialogCommand;
            showFileDialogCommandDisposable;
            loadDefaultsCommand;
            loadDefaultsCommandDisposable;
            statusMessageProperty;
        ]

    let mutable disposed = false

    let internalDispose disposing =
        if not disposed then
            if disposing then
                // Dispose managed resources.
                disposable.Dispose ()

            // Dispose unmanaged resources.
            disposed <- true

    member val IsEnabled = isEnabledProperty with get

    member val OutputFilePath = outputFilePathProperty with get

    member val ShowFileDialogCommand = showFileDialogCommand with get

    member val IsNicoLiveAndShowroomNormalCommentEnabled =
        isNicoLiveAndShowroomNormalCommentEnabledProperty with get

    member val IsNicoLiveNormalCommentEnabled =
        isNicoLiveAndShowroomNormalCommentEnabledProperty with get

    member val IsShowroomNormalCommentEnabled =
        isNicoLiveAndShowroomNormalCommentEnabledProperty with get

    member val LoadDefaultsCommand = loadDefaultsCommand with get

    member val StatusMessage = statusMessageProperty with get

    member this.Serialize writer =
        let j = JsonValue.Record [|
            ("version", JsonValue.Number <| decimal version);
            ("isEnabled", JsonValue.Boolean isEnabledProperty.Value);
            ("outputFilePath", JsonValue.String outputFilePathProperty.Value);
            ("isNicoLiveAndShowroomNormalCommentEnabled",
                JsonValue.Boolean isNicoLiveAndShowroomNormalCommentEnabledProperty.Value);
        |]
        j.WriteTo (writer, JsonSaveOptions.None)

    member this.Deserialize (reader: TextReader) =
        let j = SettingsData.Load reader
        if j.Version >= 1 then
            isEnabledProperty.Value <- try j.IsEnabled with | _ -> defaultIsEnabled

            outputFilePathProperty.Value <-
                try
                    match j.OutputFilePath with
                    | value when System.String.IsNullOrEmpty value -> defaultOutputFilePath
                    | value -> value
                with
                | _ -> defaultOutputFilePath

            isNicoLiveAndShowroomNormalCommentEnabledProperty.Value <-
                try j.IsNicoLiveAndShowroomNormalCommentEnabled with
                    | _ -> defaultIsNicoLiveAndShowroomNormalCommentEnabled

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        override this.PropertyChanged = ev.Publish

    interface System.IDisposable with
        override this.Dispose () =
            internalDispose true
            System.GC.SuppressFinalize (this)
        
    override this.Finalize () = internalDispose false
