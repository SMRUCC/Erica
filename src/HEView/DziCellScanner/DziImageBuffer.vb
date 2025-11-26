Imports System.Drawing
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar.Tqdm
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Scripting.Runtime

Public Class DziImageBuffer

    Public ReadOnly Property tile As Rectangle
    ''' <summary>
    ''' the raw color image buffer
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property bitmap As BitmapBuffer
    ''' <summary>
    ''' the raw color image buffer its grayscale image
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property grayscale As BitmapBuffer
    ''' <summary>
    ''' the tile xy
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property xy As Integer()

    Sub New(tile As Rectangle, xy As Integer(), bitmap As BitmapBuffer)
        _tile = tile
        _xy = xy
        _bitmap = bitmap
        _grayscale = New BitmapBuffer(bitmap).Grayscale
    End Sub

    ''' <summary>
    ''' load tile image files from disk to memory 
    ''' </summary>
    ''' <param name="dzi"></param>
    ''' <param name="level"></param>
    ''' <param name="dir"></param>
    ''' <param name="skipBlank"></param>
    ''' <returns></returns>
    ''' <remarks>
    ''' the grayscale image for each tile also created in the dzi tile constructor function
    ''' </remarks>
    Public Shared Iterator Function LoadBuffer(dzi As DziImage, level As Integer, dir As IFileSystemEnvironment,
                                               Optional skipBlank As Boolean = False,
                                               Optional flipBackground As Boolean = False,
                                               Optional tolerance As Integer = 9) As IEnumerable(Of DziImageBuffer)

        Dim imagefiles As String() = dir.EnumerateFiles("/", "*.jpg", "*.png", "*.jpeg", "*.bmp").ToArray

        Call $"load image buffers from dzi zoom level: {level}".info

        For Each file As String In TqdmWrapper.Wrap(imagefiles, wrap_console:=App.EnableTqdm)
            Dim image As Image = Image.FromStream(dir.OpenFile(file, IO.FileMode.Open, IO.FileAccess.Read))
            Dim bitmap As BitmapBuffer = BitmapBuffer.FromImage(image)

            If skipBlank Then
                Dim pixels = bitmap.GetARGBStream
                Dim z As UInteger = pixels(Scan0)
                Dim isblank As Boolean = pixels.All(Function(i) i = z)

                If isblank Then
                    Continue For
                End If
            End If
            If flipBackground Then
                Dim pixels As Color() = bitmap.GetPixelsAll.ToArray

                For i As Integer = 0 To pixels.Length - 1
                    If pixels(i).Equals(Color.Black, tolerance:=tolerance) OrElse pixels(i).IsTransparent Then
                        pixels(i) = Color.White
                    End If
                Next

                bitmap = New BitmapBuffer(pixels, bitmap.Size)
            End If

            Dim xy = file.BaseName.Split("_"c).AsInteger
            Dim tile As Rectangle = dzi.DecodeTile(level, xy(0), xy(1))

            Yield New DziImageBuffer(tile, xy, bitmap)
        Next
    End Function

    ''' <summary>
    ''' make grayscale image with global scale across the tiles
    ''' </summary>
    ''' <param name="tiles"></param>
    ''' <returns></returns>
    Public Shared Function GlobalScales(tiles As DziImageBuffer()) As DziImageBuffer()
        Dim tileList As BitmapBuffer() = New BitmapBuffer(tiles.Length - 1) {}

        For i As Integer = 0 To tileList.Length - 1
            tileList(i) = tiles(i).bitmap
        Next

        tileList = tileList.GlobalTileScales.ToArray

        For i As Integer = 0 To tileList.Length - 1
            With tiles(i)
                tiles(i) = New DziImageBuffer(.tile, .xy, tileList(i))
            End With
        Next

        Return tiles
    End Function

End Class
