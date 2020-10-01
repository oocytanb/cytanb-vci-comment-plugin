namespace CytanbVciCommentSourcePlugin

open Cytanb.Util

type SettingsDialogXaml = FsXaml.XAML<"SettingsDialog.xaml">

type SettingsDialog () as this =
    inherit SettingsDialogXaml ()

    let mutable finalCloseState = false

    do
        this.DataContext <- new SettingsDialogViewModel ()

    member this.FinalClose () =
        finalCloseState <- true
        this.Close ()

    override this.OnClosing e =
        if not finalCloseState then
            e.Cancel <- true
            this.Visibility <- System.Windows.Visibility.Hidden

        base.OnClosing e
