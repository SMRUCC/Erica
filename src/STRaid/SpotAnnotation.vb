Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel.SchemaMaps

''' <summary>
''' The erica spatial table
''' </summary>
''' <remarks>
''' [x, y, class, color]
''' </remarks>
Public Class SpotAnnotation

    Public Property x As Double
    Public Property y As Double

    <Column("class")>
    Public Property label As String
    Public Property color As String

    Public Property barcode As String

    Public Overrides Function ToString() As String
        Return $"{label}[{CInt(X)},{CInt(Y)}]"
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="h5ad"></param>
    ''' <param name="useCellAnnotation">
    ''' nothing means auto
    ''' </param>
    ''' <returns></returns>
    Public Shared Function LoadAnnotations(h5ad As AnnData, Optional useCellAnnotation As Boolean? = Nothing) As IEnumerable(Of SpotAnnotation)
        Dim spatial = h5ad.obsm.spatial
        Dim labels = h5ad.obs.class_labels
        Dim clusters = h5ad.obs.clusters _
            .Select(Function(i) labels(i)) _
            .ToArray
        Dim colors As String()

        If useCellAnnotation Is Nothing Then
            colors = If(
                h5ad.uns.annotation_colors.IsNullOrEmpty,
                h5ad.uns.clusters_colors,
                h5ad.uns.annotation_colors
            )
        Else
            colors = If(
                useCellAnnotation,
                h5ad.uns.annotation_colors,
                h5ad.uns.clusters_colors
            )
        End If

        Dim x As Double() = spatial.Select(Function(a) CDbl(a.X)).ToArray
        Dim y As Double() = spatial.Select(Function(a) CDbl(a.Y)).ToArray

        colors = h5ad.obs.clusters _
            .Select(Function(i) colors(i)) _
            .ToArray

        Return colors _
            .Select(Function(color, i)
                        Return New SpotAnnotation With {
                            .color = color,
                            .label = clusters(i),
                            .X = x(i),
                            .Y = y(i)
                        }
                    End Function)
    End Function

End Class
