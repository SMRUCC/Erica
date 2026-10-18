Imports SMRUCC.Rsharp.Runtime.Interop

<Assembly: RPackageModule>

Public Class zzz

    Public Shared Sub onLoad()
        Call singleCells.Main()
        Call GRN.Main()
    End Sub
End Class
