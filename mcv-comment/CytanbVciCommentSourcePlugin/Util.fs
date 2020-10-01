namespace Cytanb

open FSharp.Control.Reactive

module Util =
    type MaybeBuilder () =
        member this.Bind (x, f) =
            match x with
            | None -> None
            | Some y -> f y

        member this.Return (x) = Some x
   
    let maybe = new MaybeBuilder ()

    let inline (|?) lhs rhs = if isNull lhs then rhs else lhs

    let mapHeadChar f str =
        String.mapi (fun i ch -> if i = 0 then f ch else ch) str

    let inline headCharToLower str = mapHeadChar System.Char.ToLower str

    let inline headCharToUpper str = mapHeadChar System.Char.ToUpper str

    let inline truncate maxLength (str: string) = 
        if str.Length > maxLength
        then str.Substring(0, maxLength)
        else str

    let inline dprintfn fmt = Printf.ksprintf System.Diagnostics.Debug.WriteLine fmt

    let inline currentThreadId () = System.Threading.Thread.CurrentThread.ManagedThreadId

    let private emptyDisposable =
        Disposable.create
            <| fun () -> ()

    let throttleTrailingOn scheduler dueTime source =
        fun (observer: System.IObserver<_>) ->
            let lockObj = obj ()
            let mutable scheduling = false
            let mutable trailing = None
            let timerDisposable = Disposable.Serial

            let trailingAction abortCondition = fun () ->
                lock lockObj
                <| fun () ->
                    let feed = trailing
                    trailing <- None
                    if abortCondition feed then
                        scheduling <- false
                        Disposable.setInnerDisposalOf timerDisposable emptyDisposable
                    feed
                |> Option.iter observer.OnNext

            let consume = trailingAction <| fun _ -> true

            let periodicAction = trailingAction Option.isNone

            Observable.subscribeWithCallbacks
            <| fun value ->
                let needScheduling =
                    lock lockObj (fun () ->
                        if scheduling
                        then
                            trailing <- Some value
                            false
                        else
                            true
                    )

                if needScheduling then
                    observer.OnNext value

                    lock lockObj
                    <| fun () ->
                        scheduling <- true
                        Schedule.periodicAction dueTime periodicAction scheduler
                        |> Disposable.setInnerDisposalOf timerDisposable
            <| fun exn ->
                consume ()
                observer.OnError exn
            <| fun () ->
                consume ()
                observer.OnCompleted ()
            <| source
            
            |> Disposable.compose timerDisposable
        
        |> System.Reactive.Linq.Observable.Create

    let throttleTrailing dueTime source = throttleTrailingOn System.Reactive.Concurrency.Scheduler.Default dueTime source
