Imports System.Drawing
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar.Tqdm
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Scripting.Runtime

Public Class DziImageBuffer

    Public ReadOnly Property tile As Rectangle
    Public ReadOnly Property bitmap As BitmapBuffer
    ''' <summary>
    ''' the tile xy
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property xy As Integer()

    Sub New(tile As Rectangle, xy As Integer(), bitmap As BitmapBuffer)
        _tile = tile
        _xy = xy
        _bitmap = bitmap
    End Sub

    Public Shared Iterator Function LoadBuffer(dzi As DziImage,
                                               level As Integer,
                                               dir As IFileSystemEnvironment) As IEnumerable(Of DziImageBuffer)

        Dim imagefiles As String() = dir.EnumerateFiles("/", "*.jpg", "*.png", "*.jpeg", "*.bmp").ToArray

        Call $"load image buffers from dzi zoom level: {level}".info

        For Each file As String In TqdmWrapper.Wrap(imagefiles, wrap_console:=App.EnableTqdm)
            Dim image As Image = Image.FromStream(dir.OpenFile(file, IO.FileMode.Open, IO.FileAccess.Read))
            Dim bitmap As BitmapBuffer = BitmapBuffer.FromImage(image)
            Dim xy = file.BaseName.Split("_"c).AsInteger
            Dim tile As Rectangle = dzi.DecodeTile(level, xy(0), xy(1))

            Yield New DziImageBuffer(tile, xy, bitmap)
        Next
    End Function

End Class
