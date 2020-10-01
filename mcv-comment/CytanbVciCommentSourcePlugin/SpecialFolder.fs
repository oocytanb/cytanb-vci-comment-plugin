namespace Cytanb

open System.Runtime.InteropServices

module SpecialFolder =
    [<DllImport("shell32.dll")>]
    extern int SHGetKnownFolderPath ([<MarshalAs(UnmanagedType.LPStruct)>] System.Guid rfid, uint32 dwFlags, nativeint hToken, nativeint& pszPath)

    let private g str = System.Guid.Parse str

    let KnownFolderPath guid = fun () ->
        let mutable pszPath = System.IntPtr.Zero
        try
            let hr = SHGetKnownFolderPath (guid, uint32 0, System.IntPtr.Zero, &pszPath)
            if hr >= 0
            then
                Some <| Marshal.PtrToStringAuto pszPath
            else None
        finally
            if pszPath <> System.IntPtr.Zero then
                Marshal.FreeCoTaskMem pszPath

    let RoamingAppData = KnownFolderPath <| g "F1B32785-6FBA-4FCF-9D55-7B8E7F157091"
    let LocalAppData = KnownFolderPath <| g "F1B32785-6FBA-4FCF-9D55-7B8E7F157091"
    let LocalAppDataLow = KnownFolderPath <| g "A520A1A4-1780-4FF6-BD18-167343C5AF16"
