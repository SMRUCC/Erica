Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Math2D.ConcaveHull
Imports Microsoft.VisualBasic.Imaging.Filters
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.LinearAlgebra
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
    ''' <summary>
    ''' 细胞椭圆长轴
    ''' </summary>
    ''' <returns></returns>
    Public Property r1 As Double
    ''' <summary>
    ''' 细胞椭圆短轴
    ''' </summary>
    ''' <returns></returns>
    Public Property r2 As Double
    ''' <summary>
    ''' 椭圆角度
    ''' </summary>
    ''' <returns></returns>
    Public Property theta As Double
    Public Property density As Double
    Public Property average_dist As Double
    ''' <summary>
    ''' the average grayscale weighted by area
    ''' </summary>
    ''' <returns></returns>
    Public Property weight As Double

    Public Overrides Function ToString() As String
        Return $"({physical_x},{physical_y}) weight:{weight}, morphology:[r1:{r1},r2:{r2},theta:{theta}]"
    End Function

    Protected Overridable Function Clone() As CellScan
        Return New CellScan With {
            .area = area,
            .average_dist = average_dist,
            .density = density,
            .r2 = r2,
            .moranI = moranI,
            .physical_x = physical_x,
            .physical_y = physical_y,
            .points = points,
            .pvalue = pvalue,
            .ratio = ratio,
            .scan_x = scan_x,
            .scan_y = scan_y,
            .tile_id = tile_id,
            .weight = weight,
            .r1 = r1,
            .x = x,
            .y = y
        }
    End Function

    ''' <summary>
    ''' make ostu binary processing of the input image and then run CCL for extract the cell shapes from the binary image.
    ''' </summary>
    ''' <param name="grayscale">
    ''' should be a grayscale image
    ''' </param>
    ''' <param name="offset"></param>
    ''' <returns>
    ''' a collection of the cell object
    ''' </returns>
    ''' 
    Public Shared Iterator Function CellLookups(grayscale As BitmapBuffer,
                                                Optional offset As Point = Nothing,
                                                Optional global_ostuThreshold As Integer = -1,
                                                Optional verbose As Boolean = True) As IEnumerable(Of CellScan)

        Dim bin As BitmapBuffer = grayscale.ostuFilter(flip:=False, verbose:=verbose, threshold:=global_ostuThreshold)
        Dim CELLS = CCLabeling.Process(bin, background:=Color.White, 0).ToArray

        For Each shape As Polygon2D In CELLS
            Dim rect As RectangleF = shape.GetRectangle
            ' Dim fit As EllipseFitResult = EllipseFitResult.FitEllipse(shape.AsEnumerable.ToArray, strict:=False)
            Dim fit As EllipseFitResult = SimpleCellMorphology.CalculateMetrics(shape.AsEnumerable.ToArray)

            If fit Is Nothing Then
                Continue For
            End If

            Dim center As PointF = rect.Centre
            Dim weights As Double() = shape _
                .AsEnumerable _
                .Select(Function(p)
                            ' RGB is identical in a grayscale image
                            ' just use the R channel at here
                            Return grayscale.GetPixel(p.X, p.Y).R / 255
                        End Function) _
                .ToArray

            Yield New CellScan With {
                .area = fit.Area,
                .ratio = fit.SemiMinorAxis / fit.SemiMajorAxis,
                .scan_x = shape.xpoints,
                .scan_y = shape.ypoints,
                .x = center.X,
                .y = center.Y,
                .physical_x = .x + offset.X,
                .physical_y = .y + offset.Y,
                .points = shape.length,
                .r2 = fit.SemiMinorAxis,
                .r1 = fit.SemiMajorAxis,
                .theta = fit.RotationAngle,
                .weight = weights.Average
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

        Return raw.ConcaveHull(r:=1)
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
            Dim transformedCell As CellScan = originalCell.Clone

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
    ''' 仿射变换
    ''' </summary>
    Public Shared Function ApplyAffineTransform(cells As CellScan(), transform As AffineTransform) As CellScan()
        If cells Is Nothing OrElse cells.Length = 0 Then
            Return New CellScan() {}
        Else
            Dim transformedCells(cells.Length - 1) As CellScan

            For i As Integer = 0 To cells.Length - 1
                transformedCells(i) = cells(i).Clone

                With transform.ApplyToPoint(New PointF(transformedCells(i).physical_x, transformedCells(i).physical_y))
                    transformedCells(i).physical_x = .X
                    transformedCells(i).physical_y = .Y
                End With
            Next

            Return transformedCells
        End If
    End Function
End Class
