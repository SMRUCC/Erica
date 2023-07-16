Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Math.Distributions
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.GCModeller.Workbench.ExperimentDesigner
Imports SMRUCC.Rsharp.Runtime.Internal.[Object]
Imports SMRUCC.Rsharp.Runtime.Interop
Imports STImaging
Imports STRaid
Imports Matrix = SMRUCC.genomics.Analysis.HTS.DataFrame.Matrix

''' <summary>
''' spatial transcriptomics data toolkit
''' </summary>
<Package("STdata")>
Public Module STdata

    ''' <summary>
    ''' load the raw expression matrix which is associated
    ''' with the barcode
    ''' </summary>
    ''' <param name="h5ad"></param>
    ''' <returns>
    ''' the result matrix object in format of sample id of the 
    ''' result matrix is the gene id and the row id in matrix is 
    ''' actually the spot xy data tag or the barcoede data
    ''' </returns>
    ''' <remarks>
    ''' the expressin data just associated with the barcode, 
    ''' no spot spatial information.
    ''' </remarks>
    <ExportAPI("read.ST_h5ad")>
    Public Function ReadST_spacerangerH5Matrix(h5ad As String) As Matrix
        Return LoadDisk.ReadST_spacerangerH5Matrix(h5ad)
    End Function

    ''' <summary>
    ''' load the spatial mapping data of the spot barcode 
    ''' associated with the spot spaital information
    ''' </summary>
    ''' <param name="file"></param>
    ''' <returns></returns>
    <ExportAPI("read.spatial_spots")>
    Public Function ReadSpatialSpots(file As String) As SpaceSpot()
        Return ST_spaceranger _
            .LoadTissueSpots(file.SolveStream.LineTokens) _
            .ToArray
    End Function

    <ExportAPI("as.STRaid")>
    Public Function CombineSTRaid(h5Matrix As Matrix, spots As SpaceSpot()) As STRaid.STRaid
        Dim spotIndex As New Dictionary(Of String, Point)

        For i As Integer = 0 To spots.Length - 1
            Call spotIndex.Add(spots(i).barcode, spots(i).GetPoint)
        Next

        Return New STRaid.STRaid With {
            .matrix = h5Matrix,
            .spots = h5Matrix.expression _
                .Select(Function(r) spotIndex(r.geneID)) _
                .ToArray
        }
    End Function

    <ExportAPI("as.STmatrix")>
    Public Function CreateSpatialMatrix(h5Matrix As Matrix, spots As SpaceSpot()) As Matrix
        Return CombineSTRaid(h5Matrix, spots).GetSpatialMatrix
    End Function

    <ExportAPI("write.straid")>
    Public Function WriteMatrix(straid As STRaid.STRaid, file As String) As Object
        Using buffer As Stream = file.Open(FileMode.OpenOrCreate, doClear:=True, [readOnly]:=False)
            Return Global.STRaid.STRaid.Write(straid, file:=buffer)
        End Using
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="matrix">
    ''' should be a transposed matrix of the output from ``as.STmatrix``. 
    ''' </param>
    ''' <param name="nsamples"></param>
    ''' <returns></returns>
    <ExportAPI("sampling")>
    <RApiReturn("sampleinfo", "matrix")>
    Public Function Sampling(matrix As Matrix, Optional nsamples As Integer = 32) As Object
        Dim N As Integer = matrix.sampleID.Length / 5
        Dim repeats = Bootstraping.Samples(matrix.sampleID, bags:=nsamples, N:=N).ToArray
        Dim samplelist = repeats _
            .Select(Function(group)
                        Dim tag As String = $"{matrix.tag}.{group.i + 1}"
                        Dim submat = matrix.Project(group.value)
                        Dim v As Double() = submat.expression _
                            .Select(Function(g) g.experiments.Average) _
                            .ToArray

                        Return (tag, v)
                    End Function) _
            .ToArray
        Dim sampleinfo = samplelist _
            .Select(Function(i, j)
                        Return New SampleInfo With {
                            .batch = 1,
                            .color = "",
                            .ID = i.tag,
                            .injectionOrder = j + 1,
                            .sample_info = matrix.tag,
                            .sample_name = i.tag,
                            .shape = 1
                        }
                    End Function) _
            .ToArray

        matrix = New Matrix With {
            .expression = samplelist _
                .Select(Function(i) New DataFrameRow With {.experiments = i.v, .geneID = i.tag}) _
                .ToArray,
            .sampleID = matrix.rownames
        }
        matrix = matrix.T

        Return New list With {
            .slots = New Dictionary(Of String, Object) From {
                {"sampleinfo", sampleinfo},
                {"matrix", matrix}
            }
        }
    End Function
End Module