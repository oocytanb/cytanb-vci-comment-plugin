namespace Cytanb

open System.Reflection
open FSharp.Collections

module AssemblyVersionResolver =
    type ResolverHandler (callback) =
        let handler =
            let h = new System.ResolveEventHandler (callback)
            System.AppDomain.CurrentDomain.add_AssemblyResolve h
            h

        let mutable disposed = false

        let internalDispose disposing =
            if not disposed then
                if disposing then
                    // Dispose managed resources.
                    ()

                // Dispose unmanaged resources.
                System.AppDomain.CurrentDomain.remove_AssemblyResolve handler

                disposed <- true

        interface System.IDisposable with
            override this.Dispose () =
                internalDispose true
                System.GC.SuppressFinalize (this)
    
        override this.Finalize () = internalDispose false

    let private propertyInfoList =
        let asmType = typeof<AssemblyName>
        Array.map (asmType.GetProperty)
            [| "CultureName"; "Flags"; "Name"; "ProcessorArchitecture"; "Version" |]

    let private testAssemblyName (target: AssemblyName) (source: AssemblyName) =
        Array.fold (fun state (propertyInfo: PropertyInfo) ->
            state && (
                let targetValue = propertyInfo.GetValue target
                let sourceValue = propertyInfo.GetValue source
                isNull sourceValue || targetValue = sourceValue
            )
        ) true propertyInfoList

    let register assemblyBindingList =
        new ResolverHandler(fun _ e ->
            try
                let requesting = new AssemblyName (e.Name)
                let matched = List.filter (fun (matchName: AssemblyName, targetName: AssemblyName) -> testAssemblyName requesting matchName) assemblyBindingList
                match matched with
                | [] -> Unchecked.defaultof<Assembly>
                | (_, targetName) :: _ ->
                    System.AppDomain.CurrentDomain.Load targetName
            with
                | _ -> Unchecked.defaultof<Assembly>
        )
