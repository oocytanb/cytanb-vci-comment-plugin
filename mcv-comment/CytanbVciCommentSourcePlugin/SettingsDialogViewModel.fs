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
    "outputFilePath": "main.lua"

}
""">

type SettingsDialogViewModel () =
    static let version = 1

    static let defaultIsEnabled = false

    let defaultOutputFilePath =
        try
            Path.Combine (
                (
                    match SpecialFolder.LocalAppDataLow () with
                    | None -> ""
                    | Some(x) -> Path.Combine (x, "infiniteloop Co,Ltd", "VirtualCast", "EmbeddedScriptWorkspace")
                ),
                "cytanb-comment-source",
                "main.lua")
        with
        | :? System.ArgumentException -> ""

    let isEnabledProperty = new ReactivePropertySlim<bool> (defaultIsEnabled)

    let outputFilePathProperty = new ReactivePropertySlim<string> (defaultOutputFilePath)

    let showFileDialogCommand = new ReactiveCommand ()

    let showFileDialogCommandDisposable =         
        showFileDialogCommand
        |> Observable.map (fun _ ->
            let (initialDir, fileName) =
                let filePath = outputFilePathProperty.Value
                try
                    let dirPath = Path.GetDirectoryName filePath
                    let fileName = Path.GetFileName filePath
                    (dirPath, fileName)
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
        )

    let statusMessageProperty = new ReactivePropertySlim<string> ("")

    let ev = new Event<_, _> ()

    let disposable =
        Disposables.compose [
            isEnabledProperty;
            outputFilePathProperty;
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

    member val LoadDefaultsCommand = loadDefaultsCommand with get

    member val StatusMessage = statusMessageProperty with get

    member this.Serialize writer =
        let j = JsonValue.Record [|
            ("version", JsonValue.Number <| decimal version);
            ("isEnabled", JsonValue.Boolean isEnabledProperty.Value);
            ("outputFilePath", JsonValue.String outputFilePathProperty.Value);
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

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        override this.PropertyChanged = ev.Publish

    interface System.IDisposable with
        override this.Dispose () =
            internalDispose true
            System.GC.SuppressFinalize (this)
        
    override this.Finalize () = internalDispose false
