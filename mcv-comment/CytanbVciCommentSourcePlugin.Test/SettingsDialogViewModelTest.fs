namespace CytanbVciCommentSourcePlugin.Test

module SettingsDialogViewModelTest =
    open NUnit.Framework
    open FsCheck
    open FsCheck.NUnit

    open System.IO

    open CytanbVciCommentSourcePlugin

    [<SetUp>]
    let Setup () =
        ()

    [<Test>]
    let ItDefault () =
        use vm = new SettingsDialogViewModel ()

        not vm.IsEnabled.Value |> Check.Quick
        Path.GetFileName (vm.OutputFilePath.Value) = "main.lua" |> Check.Quick
        vm.StatusMessage.Value = "" |> Check.Quick

        vm.IsEnabled.Value <- true
        vm.OutputFilePath.Value <- "foo/bar.lua"
        vm.StatusMessage.Value <- "Hoge Piyo"

        vm.IsEnabled.Value |> Check.Quick
        vm.OutputFilePath.Value = "foo/bar.lua" |> Check.Quick
        vm.StatusMessage.Value = "Hoge Piyo" |> Check.Quick

        vm.LoadDefaultsCommand.Execute ()

        not vm.IsEnabled.Value |> Check.Quick
        Path.GetFileName (vm.OutputFilePath.Value) = "main.lua" |> Check.Quick
        vm.StatusMessage.Value = "" |> Check.Quick

    [<Test>]
    let ItSerialize () =
        use vm = new SettingsDialogViewModel ()
        vm.IsEnabled.Value <- true
        vm.OutputFilePath.Value <- "foo/bar.lua"
        vm.StatusMessage.Value <- "Hoge Piyo"

        use writer = new StringWriter ()
        vm.Serialize writer

        let reader = new StringReader (writer.ToString ())
        let decVM = new SettingsDialogViewModel ()
        decVM.Deserialize reader

        vm.IsEnabled.Value = decVM.IsEnabled.Value |> Check.Quick
        vm.OutputFilePath.Value = decVM.OutputFilePath.Value |> Check.Quick
        vm.StatusMessage.Value = decVM.StatusMessage.Value |> Check.Quick
