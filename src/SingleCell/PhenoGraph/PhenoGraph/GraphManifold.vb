Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.CommandLine.InteropService.Pipeline
Imports Microsoft.VisualBasic.Data.GraphTheory.KdTree
Imports Microsoft.VisualBasic.Data.visualize.Network.Graph
Imports Microsoft.VisualBasic.DataMining.UMAP
Imports Microsoft.VisualBasic.Language
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

Public Module GraphManifold

    ''' <summary>
    ''' This approach first constructs a single-cell k-nearest-neighbor 
    ''' graph for each timepoint ti, with nodes representing cells and 
    ''' edges linking neighbors in a low-dimensional subspace;
    ''' </summary>
    ''' <param name="singleCells"></param>
    ''' <param name="dimensions"></param>
    ''' <param name="k"></param>
    ''' <returns></returns>
    <Extension>
    Public Function KnnGraph(singleCells As Matrix,
                             Optional dimensions As Integer = 3,
                             Optional k As Integer = 6) As NetworkGraph

        Dim labels As String() = Nothing
        Dim umap As Umap = Manifold(singleCells, dimensions, labels)
        Dim projection As Double()() = umap.GetEmbedding
        Dim matrix As TagVector() = labels _
            .Select(Function(id, r)
                        Return New TagVector With {
                            .index = id,
                            .vector = projection(r),
                            .tag = id
                        }
                    End Function) _
            .ToArray

        ' do knn search for build graph
        Dim g As New NetworkGraph
        Dim i As i32 = Scan0

        For Each cellLabel As String In labels
            Call g.CreateNode(cellLabel)
        Next

        For Each node In ApproximateNearNeighbor.FindNeighbors(matrix, k:=k)
            Dim from As Node = g.GetElementByID(labels(++i))
            Dim j As i32 = Scan0

            For Each index As Integer In node.indices
                Call g.CreateEdge(
                    u:=from,
                    v:=g.GetElementByID(labels(index)),
                    weight:=node.weights(++j)
                )
            Next
        Next

        Return g
    End Function

    ''' <summary>
    ''' then joins the graphs by identifying neighboring cells in 
    ''' pairs of adjacent time points, using a coordinate system 
    ''' learned from the future (ti+1) timepoint (see methods). 
    ''' </summary>
    ''' <param name="t1"></param>
    ''' <param name="t2"></param>
    ''' <returns></returns>
    Public Function Join(t1 As NetworkGraph, t2 As NetworkGraph) As NetworkGraph

    End Function

    Private Function Manifold(singleCells As Matrix, dimensions As Integer, ByRef labels As String()) As Umap
        Dim matrix As Double()()
        Dim report As RunSlavePipeline.SetProgressEventHandler = AddressOf RunSlavePipeline.SendProgress

        labels = singleCells.rownames
        matrix = singleCells.expression _
            .Select(Function(r)
                        Return r.experiments
                    End Function) _
            .ToArray

        Dim umap As New Umap(
            distance:=AddressOf DistanceFunctions.CosineForNormalizedVectors,
            dimensions:=dimensions,
            progressReporter:=report
        )
        Dim nEpochs As Integer

        Call Console.WriteLine("Initialize fit..")

        nEpochs = umap.InitializeFit(matrix)

        Console.WriteLine("- Done")
        Console.WriteLine()
        Console.WriteLine("Calculating..")

        For i As Integer = 0 To nEpochs - 1
            Call umap.Step()

            If (100 * i / nEpochs) Mod 5 = 0 Then
                Console.WriteLine($"- Completed {i + 1} of {nEpochs} [{CInt(100 * i / nEpochs)}%]")
            End If
        Next

        Return umap
    End Function

End Module
