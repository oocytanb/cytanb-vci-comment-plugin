namespace CytanbVciCommentSourcePlugin

open System.Globalization
open System.Text.RegularExpressions
open System.IO
open FSharp.Collections

open Cytanb.Util

module LuaScriptIO =
    let private resourceName = "CytanbVciCommentSourcePlugin.resources.comment-source-main.lua"

    let private templateContents =
        try
            let asm = System.Reflection.Assembly.GetExecutingAssembly ()
            use reader = new StreamReader (asm.GetManifestResourceStream resourceName)
            reader.ReadToEnd ()
        with
            | _ -> ""

    let utf8EncodingWithoutBOM = System.Text.UTF8Encoding false

    let maxMessageFieldSize = 512

    let escapeLuaString (str: string) =
        let rs =
            try
                let ns = str.Normalize ()
                Regex.Replace (ns, """['"\\]""", """\$0""")
            with
            | _ -> ""

        String.map
        <| fun ch ->
            match CharUnicodeInfo.GetUnicodeCategory ch with
            | UnicodeCategory.Control -> ' '
            | _ -> ch
        <| rs

    let private commentMessageToStatement { Message = m; Name = n; CommentSource = s; Timestamp = t } =
        sprintf """commentMessageQueue.Offer({message = '%s', name = '%s', commentSource = '%s', timestamp = %d})"""
        <| truncate maxMessageFieldSize (escapeLuaString m)
        <| truncate maxMessageFieldSize (escapeLuaString n)
        <| truncate maxMessageFieldSize (escapeLuaString s)
        <| t

    let write commentMessages (fileName: string) =
        use writer = new StreamWriter (fileName, false, utf8EncodingWithoutBOM, NewLine = "\n")
        writer.WriteLine (templateContents)
        writer.WriteLine """if commentMessageQueue then"""
        Seq.iter
        <| fun entry ->
            let statement = "    " + (commentMessageToStatement entry)
            writer.WriteLine statement
        <| commentMessages
        writer.WriteLine """end"""
        