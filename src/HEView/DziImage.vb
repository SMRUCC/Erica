Imports System.Drawing
Imports System.Xml.Serialization

<XmlType("Image", [Namespace]:="http://schemas.microsoft.com/deepzoom/2008")>
Public Class DziImage

    <XmlAttribute> Public Property Format As String
    <XmlAttribute> Public Property Overlap As Integer
    <XmlAttribute> Public Property TileSize As Integer

    Public Property Size As SizeInt

    Public Class SizeInt
        <XmlAttribute> Public Property Width As Integer
        <XmlAttribute> Public Property Height As Integer

        Public Overrides Function ToString() As String
            Return $"{{width:{Width}, height:{Height}}}"
        End Function
    End Class

    Public Overrides Function ToString() As String
        Return Size.ToString
    End Function

    Public Function DecodeOffsets() As Point

    End Function

End Class
