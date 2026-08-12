''' <summary>
''' Represents a spatial image in a Seurat object (used for spatial
''' transcriptomics data like 10x Visium).
''' </summary>
Public Class SeuratImage

    ''' <summary>
    ''' The image name (e.g. "slice1")
    ''' </summary>
    Public Property Name As String

    ''' <summary>
    ''' Spatial coordinates for each spot/cell.
    ''' Keys: cell/spot barcode names
    ''' Values: coordinate arrays or data
    ''' </summary>
    Public Property Coordinates As Dictionary(Of String, Double())

    ''' <summary>
    ''' Tissue positions data.frame equivalent:
    ''' columns typically include: "tissue", "row", "col", "imagerow", "imagecol"
    ''' </summary>
    Public Property TissuePositions As Dictionary(Of String, Array)

    ''' <summary>
    ''' Scale factors for converting between pixel and tissue coordinates.
    ''' Typical keys: "spot_diameter_fullres", "tissue_hires_scalef",
    ''' "fiducial_diameter_fullres", "tissue_lowres_scalef"
    ''' </summary>
    Public Property ScaleFactors As Dictionary(Of String, Double)

    ''' <summary>
    ''' Number of spots/cells in this image.
    ''' </summary>
    Public Property nSpots As Integer

    Public Overrides Function ToString() As String
        Return $"SeuratImage[{Name}] spots={nSpots}"
    End Function

End Class
