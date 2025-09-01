Imports System.Drawing
Imports System.Xml.Serialization
Imports std = System.Math

<XmlType("Image", [Namespace]:="http://schemas.microsoft.com/deepzoom/2008")>
<XmlRoot("Image", [Namespace]:="http://schemas.microsoft.com/deepzoom/2008")>
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

    ''' <summary>
    ''' 计算指定层级的图像维度（宽度或高度）
    ''' </summary>
    ''' <param name="originalDimension">原始维度（宽度或高度）</param>
    ''' <param name="level">目标层级</param>
    ''' <returns>指定层级的维度</returns>
    Public ReadOnly Property MaxZoomLevels As Integer
        Get
            ' 将原始维度连续除以2（level次），并使用Ceiling确保最小为1像素
            Dim dimension As Integer = std.Min(Size.Width, Size.Height)

            For i As Integer = 1 To Integer.MaxValue
                dimension = CInt(std.Ceiling(dimension / 2.0))

                If dimension = 1 Then
                    Return i
                End If
            Next

            ' very very large image to zoom
            Return Integer.MaxValue
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return Size.ToString & $", max-zoom-levels: {MaxZoomLevels}"
    End Function

    ''' <summary>
    ''' 计算指定层级、列、行的瓦片位置和大小
    ''' </summary>
    ''' <param name="level">层级</param>
    ''' <param name="col">列号 (从0开始)</param>
    ''' <param name="row">行号 (从0开始)</param>
    ''' <returns>表示瓦片位置和大小的矩形</returns>
    ''' <remarks>
    ''' 1. 图像金字塔与层级（Level）
    ''' 
    ''' DZI将高分辨率图像处理为一系列层级（Level）的金字塔结构。层级0（Level 0）是分辨率最高的原始图像。每增加一个层级，
    ''' 图像的宽度和高度就缩减为上一层级的一半（采用Ceiling向上取整），直至最终变为1x1像素 。
    ''' 
    ''' 最大层级的计算公式为：MaxLevel = Ceiling(Log2(Max(Width, Height)))。这意味着对于一张 4224x3168 的图片，其最大层级为 13。
    ''' 
    ''' 2. 瓦片（Tile）网格
    ''' 
    ''' 在每个层级上，图像会被分割成多个尺寸为 TileSize x TileSize的瓦片（除了最右边和最下边的瓦片，它们可能因为不能整除而更小）。
    ''' 每个层级瓦片的列数（Columns）和行数（Rows）计算公式为：
    ''' 
    ''' + Columns = Ceiling(LevelWidth / TileSize)
    ''' + Rows = Ceiling(LevelHeight / TileSize)
    ''' 
    ''' 3.瓦片重叠（Overlap）
    ''' 
    ''' DZI 允许设置 Overlap（通常为1像素）。重叠是指相邻瓦片之间会重复一部分像素区域。这样做主要是为了在图像拼接渲染时消除瓦片
    ''' 间的缝隙或瑕疵，确保视觉上的连续性。
    ''' 计算瓦片位置和尺寸时，非边缘的瓦片会向四个方向延伸 Overlap个像素。位于图像边缘（左上、右上、左下、右下、上边界、下边界、
    ''' 左边界、右边界）的瓦片，其重叠区域需要特殊处理，不能超出图像的实际边界。
    ''' </remarks>
    Public Function DecodeTile(level As Integer, col As Integer, row As Integer) As Rectangle
        ' 1. 计算当前层级的图像尺寸
        Dim levelWidth As Integer = CInt(std.Ceiling(Size.Width / std.Pow(2, MaxZoomLevels - level)))
        Dim levelHeight As Integer = CInt(std.Ceiling(Size.Height / std.Pow(2, MaxZoomLevels - level)))

        ' 2. 计算当前层级瓦片的列数和行数
        Dim totalCols As Integer = CInt(std.Ceiling(levelWidth / CDbl(TileSize)))
        Dim totalRows As Integer = CInt(std.Ceiling(levelHeight / CDbl(TileSize)))

        ' 参数检查
        If col < 0 OrElse col >= totalCols Then
            Throw New ArgumentOutOfRangeException("col", $"列号col={col}必须在0到{totalCols - 1}之间。")
        End If
        If row < 0 OrElse row >= totalRows Then
            Throw New ArgumentOutOfRangeException("row", $"行号row={row}必须在0到{totalRows - 1}之间。")
        End If

        ' 3. 计算当前瓦片的起始坐标（考虑Overlap）
        Dim startX As Integer = col * TileSize
        Dim startY As Integer = row * TileSize

        ' 4. 调整起始坐标：非0的列和行需要减去Overlap，以免重复计算重叠区域
        ' 这样能确保瓦片坐标原点正确对齐图像边界
        If col > 0 Then
            startX -= Overlap
        End If
        If row > 0 Then
            startY -= Overlap
        End If

        ' 5. 计算当前瓦片的实际宽度和高度（考虑图像边界和Overlap）
        Dim tileWidth As Integer = TileSize
        Dim tileHeight As Integer = TileSize

        ' 如果当前瓦片是最后一列，计算剩余宽度
        If col = totalCols - 1 Then
            tileWidth = levelWidth - (col * TileSize)
        End If
        ' 如果当前瓦片是最后一行，计算剩余高度
        If row = totalRows - 1 Then
            tileHeight = levelHeight - (row * TileSize)
        End If

        ' 6. 调整瓦片尺寸：加上重叠部分，但确保不超出图像边界
        ' 右边追加Overlap（如果不是最后一列）
        If col < totalCols - 1 Then
            tileWidth += Overlap
        End If
        ' 下边追加Overlap（如果不是最后一行）
        If row < totalRows - 1 Then
            tileHeight += Overlap
        End If
        ' 左边追加Overlap（如果不是第一列）-> 已在步骤4通过startX调整处理
        ' 上边追加Overlap（如果不是第一行）-> 已在步骤4通过startY调整处理

        ' 7. 确保瓦片不超出当前层级的图像实际边界
        If startX + tileWidth > levelWidth Then
            tileWidth = levelWidth - startX
        End If
        If startY + tileHeight > levelHeight Then
            tileHeight = levelHeight - startY
        End If

        ' 8. 返回最终计算的矩形区域
        Return New Rectangle(startX, startY, tileWidth, tileHeight)
    End Function
End Class
