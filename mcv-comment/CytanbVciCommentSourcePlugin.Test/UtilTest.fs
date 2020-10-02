namespace CytanbVciCommentSourcePlugin.Test

module UtilTest =
    open NUnit.Framework
    open FSharp.Control.Reactive
    open FSharp.Control.Reactive.Testing

    open Cytanb.Util

    [<SetUp>]
    let Setup () =
        ()

    [<Test>]
    let ItNullishCoalescingOperator () =
        Assert.AreEqual ("foo", "foo" |? "bar")
        Assert.AreEqual ("bar", Unchecked.defaultof<string> |? "bar")

    [<Test>]
    let ItMaybe () =
        Assert.AreEqual (Some 300, maybe {
            let! x = Some 100
            let! y = Some 200
            return x + y
        })

        Assert.AreEqual (None, maybe {
            let! x = None
            let! y = Some 200
            return x + y
        })

        Assert.AreEqual (None, maybe {
            let! x = Some 100
            let! y = None
            return x + y
        })

    [<Test>]
    let ItHeadCharToLower () =
        Assert.AreEqual ("", headCharToLower "")
        Assert.AreEqual ("abcde", headCharToLower "abcde")
        Assert.AreEqual ("abCdE", headCharToLower "AbCdE")

    [<Test>]
    let ItHeadCharToUpper () =
        Assert.AreEqual ("", headCharToUpper "")
        Assert.AreEqual ("Abcde", headCharToUpper "abcde")
        Assert.AreEqual ("AbCdE", headCharToUpper "AbCdE")

    [<Test>]
    let ItTruncate () =
        Assert.AreEqual ("", truncate 0 "")
        Assert.AreEqual ("", truncate 5 "")
        Assert.AreEqual ("", truncate 0 "abcde")
        Assert.AreEqual ("a", truncate 1 "abcde")
        Assert.AreEqual ("abcde", truncate 5 "abcde")
        Assert.AreEqual ("abcde", truncate 6 "abcde")

    [<Test>]
    let ItCurrentThreadId () =
        let tid = currentThreadId ()

        let task = System.Threading.Tasks.Task.Run (fun () -> currentThreadId ())
        task.Wait ()
        let taskTid = task.Result

        Assert.AreNotEqual (tid, taskTid)

    [<Test>]
    let ItThrottleTrailing () =
        TestSchedule.usage <| fun sch ->
            let timerOn millis ch =
                System.DateTimeOffset ((System.TimeSpan.FromMilliseconds millis).Ticks, System.TimeSpan.Zero)
                |> Observable.timerOn sch
                |> Observable.mapTo ch

            let obs =
                (timerOn 0. 'A')
                |> Observable.merge (timerOn 3. 'B')
                |> Observable.merge (timerOn 6. 'C')
                |> Observable.merge (timerOn 12. 'D')
                |> Observable.merge (timerOn 16. 'E')
                |> Observable.merge (timerOn 34. 'F')
                |> Observable.merge (timerOn 37. 'G')
                |> Observable.merge (timerOn 42. 'H')
                |> throttleTrailingOn sch (System.TimeSpan.FromMilliseconds 10.)
                |> TestSchedule.subscribeTestObserverStart sch

            ['A'; 'C'; 'E'; 'F'; 'H'] = TestObserver.nexts obs
        |> Assert.True
