Public Class SpotAnnotation

    Public Property X As Double
    Public Property Y As Double
    Public Property label As String
    Public Property color As String

    Public Overrides Function ToString() As String
        Return $"{label}[{CInt(X)},{CInt(Y)}]"
    End Function

    Public Shared Function LoadAnnotations(h5ad As AnnData, Optional useCellAnnotation As Boolean = False) As IEnumerable(Of SpotAnnotation)
        Dim spatial = h5ad.obsm.spatial
        Dim labels = h5ad.obs.class_labels
        Dim clusters = h5ad.obs.clusters _
            .Select(Function(i) labels(i)) _
            .ToArray
        Dim colors As String() = If(
            useCellAnnotation,
            h5ad.uns.annotation_colors,
            h5ad.uns.clusters_colors
        )
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
