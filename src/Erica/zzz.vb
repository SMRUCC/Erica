Imports Erica.DataPack
Imports Microsoft.VisualBasic.Data.IO.MessagePack
Imports SMRUCC.Rsharp.Runtime.Interop

<Assembly: RPackageModule>

Public Class zzz

    Public Shared Sub onLoad()
        Call singleCells.Main()
    End Sub
End Class
