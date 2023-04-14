Imports Microsoft.VisualBasic.MIME.application.rdf_xml

Public Class DataSet

    Public Property head As HeadSet
    Public Property results As ResultSet

End Class

Public Class ResultSet

    Public Property distinct As Boolean
    Public Property ordered As Boolean
    Public Property bindings As Dictionary(Of String, EntityProperty)()

End Class

Public Class HeadSet

    Public Property link As String()
    Public Property vars As String()

End Class