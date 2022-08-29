Imports Erica.DataPack
Imports Microsoft.VisualBasic.Data.IO.MessagePack
Imports SMRUCC.Rsharp.Runtime.Interop

<Assembly: RPackageModule>

Public Class zzz

    Public Shared Sub onLoad()
        Call singleCells.Main()
        Call MsgPackSerializer.DefaultContext.RegisterSerializer(New BackgroundSchema)
    End Sub
End Class
