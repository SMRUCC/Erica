Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Math2D.ConcaveHull
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Math.MachineVision.CCL
Imports std = System.Math

Public Class CellScan : Implements Layout2D

    Public Property tile_id As String
    Public Property x As Double
    Public Property y As Double

    ' 20250904 PointF can not be serialized in bson handler
    ' decompose the PointF as physical_x and physical_y

    Public Property physical_x As Double Implements Layout2D.X
    Public Property physical_y As Double Implements Layout2D.Y
    Public Property area As Double
    Public Property ratio As Double
    Public Property scan_x As Double()
    Public Property scan_y As Double()
    Public Property moranI As Double
    Public Property pvalue As Double
    Public Property points As Integer
    Public Property width As Double
    Public Property height As Double
    Public Property density As Double
    Public Property average_dist As Double
    ''' <summary>
    ''' the average grayscale weighted by area
    ''' </summary>
    ''' <returns></returns>
    Public Property weight As Double

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="grid"></param>
    ''' <param name="offset"></param>
    ''' <param name="binary_processing">
    ''' make ostu binary processing of the input image in this function?
    ''' </param>
    ''' <returns></returns>
    Public Shared Iterator Function CellLookups(grid As BitmapBuffer,
                                                Optional offset As Point = Nothing,
                                                Optional binary_processing As Boolean = True) As IEnumerable(Of CellScan)

        Dim bin As BitmapBuffer = If(binary_processing, grid.ostuFilter(flip:=False), grid)
        Dim CELLS = CCLabeling.Process(bin, background:=Color.White, 0).ToArray

        For Each shape As Polygon2D In CELLS
            Dim rect As RectangleF = shape.GetRectangle

            If rect.Width = 0.0 OrElse rect.Height = 0.0 Then
                Continue For
            End If

            Yield New CellScan With {
                .area = rect.Width * rect.Height,
                .ratio = std.Max(rect.Width, rect.Height) / std.Min(rect.Width, rect.Height),
                .scan_x = shape.xpoints,
                .scan_y = shape.ypoints,
                .x = rect.X,
                .y = rect.Y,
                .physical_x = .x + offset.X,
                .physical_y = .y + offset.Y,
                .points = shape.length,
                .height = rect.Height,
                .width = rect.Width
            }
        Next
    End Function

    Public Function GetShape(Optional physical As Boolean = False) As PointF()
        Dim raw As PointF()

        If physical Then
            Dim offset_x = Me.physical_x - x
            Dim offset_y = Me.physical_y - y

            raw = scan_x _
                .Select(Function(xi, i)
                            Return New PointF(xi + offset_x, scan_y(i) + offset_y)
                        End Function) _
                .ToArray
        Else
            raw = scan_x _
                .Select(Function(xi, i)
                            Return New PointF(xi, scan_y(i))
                        End Function) _
                .ToArray
        End If

        Return raw.ConcaveHull
    End Function

    ''' <summary>
    ''' Perform geometric transformations (scaling, rotation, translation) on cellular matrix data.
    ''' </summary>
    ''' <param name="cells">原始细胞数组</param>
    ''' <param name="transform">变换参数</param>
    ''' <returns>变换后的细胞数组</returns>
    Public Shared Function ApplyTransform(cells As CellScan(), transform As Transform) As CellScan()
        If cells Is Nothing OrElse cells.Length = 0 Then
            Return New CellScan() {}
        End If

        Dim transformedCells(cells.Length - 1) As CellScan

        ' 预先计算三角函数值以提高性能
        Dim cosTheta As Double = std.Cos(transform.theta)
        Dim sinTheta As Double = std.Sin(transform.theta)

        For i As Integer = 0 To cells.Length - 1
            Dim originalCell As CellScan = cells(i)
            Dim transformedCell As New CellScan(originalCell)

            ' 复制原始属性
            transformedCell.tile_id = originalCell.tile_id

            ' 应用变换：先缩放，再旋转，最后平移
            Dim x As Double = originalCell.physical_x
            Dim y As Double = originalCell.physical_y

            ' 1. 缩放变换
            x = x * transform.scalex
            y = y * transform.scaley

            ' 2. 旋转变换
            ' 注意：标准数学坐标系中的旋转公式
            Dim xRotated As Double = x * cosTheta - y * sinTheta
            Dim yRotated As Double = x * sinTheta + y * cosTheta

            ' 3. 平移变换
            transformedCell.physical_x = xRotated + transform.tx
            transformedCell.physical_y = yRotated + transform.ty

            transformedCells(i) = transformedCell
        Next

        Return transformedCells
    End Function

    ''' <summary>
    ''' 使用变换矩阵应用复合变换（更高效的方式）
    ''' </summary>
    Public Shared Function ApplyTransformWithMatrix(cells As CellScan(), transform As Transform) As CellScan()
        If cells Is Nothing OrElse cells.Length = 0 Then
            Return New CellScan() {}
        End If

        Dim transformedCells(cells.Length - 1) As CellScan

        ' 构建复合变换矩阵：缩放 × 旋转 × 平移
        Dim cosTheta As Double = std.Cos(transform.theta)
        Dim sinTheta As Double = std.Sin(transform.theta)

        ' 变换矩阵的各个分量
        Dim m11 As Double = transform.scalex * cosTheta   ' 缩放和旋转的复合
        Dim m12 As Double = -transform.scaley * sinTheta
        Dim m21 As Double = transform.scalex * sinTheta
        Dim m22 As Double = transform.scaley * cosTheta
        Dim tx As Double = transform.tx
        Dim ty As Double = transform.ty

        For i As Integer = 0 To cells.Length - 1
            Dim originalCell As CellScan = cells(i)
            Dim transformedCell As New CellScan(originalCell)

            transformedCell.tile_id = originalCell.tile_id

            Dim x As Double = originalCell.physical_x
            Dim y As Double = originalCell.physical_y

            ' 应用复合变换矩阵
            transformedCell.physical_x = x * m11 + y * m12 + tx
            transformedCell.physical_y = x * m21 + y * m22 + ty

            transformedCells(i) = transformedCell
        Next

        Return transformedCells
    End Function
End Class
