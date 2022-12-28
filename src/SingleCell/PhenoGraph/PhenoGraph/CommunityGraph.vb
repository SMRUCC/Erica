#Region "Microsoft.VisualBasic::c32248ffcda705ad851a8f98fb42d8c2, analysis\PhenoGraph\PhenoGraph\CommunityGraph.vb"

' Author:
' 
'       asuka (amethyst.asuka@gcmodeller.org)
'       xie (genetics@smrucc.org)
'       xieguigang (xie.guigang@live.com)
' 
' Copyright (c) 2018 GPL3 Licensed
' 
' 
' GNU GENERAL PUBLIC LICENSE (GPL3)
' 
' 
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
' 
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
' 
' You should have received a copy of the GNU General Public License
' along with this program. If not, see <http://www.gnu.org/licenses/>.



' /********************************************************************************/

' Summaries:

' Module CommunityGraph
' 
'     Function: AsGraph, (+2 Overloads) CreatePhenoGraph
' 
' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Data.GraphTheory.KdTree
Imports Microsoft.VisualBasic.Data.visualize.Network.Analysis
Imports Microsoft.VisualBasic.Data.visualize.Network.Graph
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports Microsoft.VisualBasic.Math.Scripting.Rscript
Imports Microsoft.VisualBasic.Serialization.JSON
Imports any = Microsoft.VisualBasic.Scripting

''' <summary>
''' implementation of the PhenoGraph algorithm
''' 
''' A simple R implementation of the [PhenoGraph](http://www.cell.com/cell/abstract/S0092-8674(15)00637-6) algorithm, 
''' which is a clustering method designed for high-dimensional single-cell data analysis. It works by creating a graph ("network") representing 
''' phenotypic similarities between cells by calclating the Jaccard coefficient between nearest-neighbor sets, and then identifying communities 
''' using the well known [Louvain method](https://sites.google.com/site/findcommunities/) in this graph. 
''' </summary>
Public Module CommunityGraph

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="data"></param>
    ''' <param name="k">
    ''' a large of the knn value will generate a better cluster result
    ''' </param>
    ''' <param name="link_cutoff">
    ''' it is not a good idea to set any coeff cutoff value?
    ''' </param>
    ''' <returns></returns>
    ''' 
    <Extension>
    Public Function CreatePhenoGraph(data As DataSet(),
                                     Optional k As Integer = 60,
                                     Optional link_cutoff As Double = 0,
                                     Optional knn_cutoff As Double = 0,
                                     Optional score As ScoreMetric = Nothing) As NetworkGraph

        Dim propertyNames As String() = data.PropertyNames
        Dim matrix As New List(Of Double())

        For Each row As DataSet In data
            Call matrix.Add(row(keys:=propertyNames))
        Next

        Dim dataMat As New NumericMatrix(matrix.ToArray)
        Dim graph As NetworkGraph = dataMat.CreatePhenoGraph(
            k:=k,
            link_cutoff:=link_cutoff,
            knn_cutoff:=knn_cutoff,
            score:=score
        )

        For Each v As Node In graph.vertex
            v.label = data(Integer.Parse(v.label)).ID
        Next

        Return graph
    End Function

    ''' <summary>
    ''' Jacob H. Levine and et.al. Data-Driven Phenotypic Dissection of AML Reveals 
    ''' Progenitor-like Cells that Correlate with Prognosis. Cell, 2015.
    ''' </summary>
    ''' <param name="data"></param>
    ''' <param name="k"></param>
    ''' <returns></returns>
    ''' 
    <Extension>
    Public Function CreatePhenoGraph(data As GeneralMatrix,
                                     Optional k As Integer = 30,
                                     Optional link_cutoff As Double = 0,
                                     Optional knn_cutoff As Double = 0,
                                     Optional score As ScoreMetric = Nothing) As NetworkGraph
        If k < 1 Then
            Throw New ArgumentException("k must be a positive integer!")
        ElseIf k > data.RowDimension - 2 Then
            Throw New ArgumentException("k must be smaller than the total number of points!")
        End If

        message("Run Rphenograph starts:", "\n",
        "  -Input data of ", nrow(data), " rows and ", ncol(data), " columns", "\n",
        "  -k is set to ", k, "\n",
        "  -knn_cutoff is ", knn_cutoff, "\n",
        "  -knn_kernel use ", any.ToString(score, null:="default_KD-tree"), "\n\n")

        cat("  Finding nearest neighbors...")

        ' t1 <- system.time(neighborMatrix <- find_neighbors(data, k=k+1)[,-1])
        Dim t1 As Value(Of Double) = App.ElapsedMilliseconds
        Dim neighborMatrix As Integer()()

        If Not score Is Nothing Then
            neighborMatrix = New KNN(score, knn_cutoff) _
                .FindNeighbors(data, k:=k + 1) _
                .Select(Function(row) row.indices) _
                .ToArray
        Else
            neighborMatrix = ApproximateNearNeighbor _
                .FindNeighbors(data, k:=k + 1) _
                .Select(Function(row) row.indices) _
                .ToArray
        End If

        cat("DONE ~", (t1 = App.ElapsedMilliseconds - CDbl(t1)) / 1000, "s\n", " Compute jaccard coefficient between nearest-neighbor sets...")
        ' t2 <- system.time(links <- jaccard_coeff(neighborMatrix))
        Dim t2 As Value(Of Double) = App.ElapsedMilliseconds
        Dim links As GeneralMatrix = jaccard_coeff(neighborMatrix, symmetrize:=False)
        cat("DONE ~", (t2 = App.ElapsedMilliseconds - CDbl(t2)) / 1000, "s\n", " Build undirected graph from the weighted links...")

        ' take rows
        ' colnames(relations)<- c("from","to","weight")
        ' which its coefficient should be greater than ZERO
        link_cutoff = New DoubleRange(0, 1).ScaleMapping(link_cutoff, links.ColumnVector(2).Range)
        links = links(links(2, byRow:=False) > link_cutoff)

        Dim t3 As Value(Of Double) = App.ElapsedMilliseconds
        Dim g = DirectCast(links, NumericMatrix).AsGraph()
        cat("DONE ~", (t3 = App.ElapsedMilliseconds - CDbl(t3)) / 1000, "s\n", " Run louvain clustering on the graph ...")
        Dim t4 As Value(Of Double) = App.ElapsedMilliseconds

        ' Other community detection algorithms: 
        '    cluster_walktrap, cluster_spinglass, 
        '    cluster_leading_eigen, cluster_edge_betweenness, 
        '    cluster_fast_greedy, cluster_label_prop  
        Dim community As NetworkGraph = Communities.Analysis(g)
        Dim qsub As Double() = Nothing

        cat("DONE ~", (t4 = App.ElapsedMilliseconds - CDbl(t4)) / 1000, "s\n")

        message("Run Rphenograph DONE, totally takes ", {CDbl(t1), CDbl(t2), CDbl(t3), CDbl(t4)}.Sum / 1000, "s.", "\n")
        cat("  Return a community class\n  -Modularity value:", Communities.Modularity(community, qsub), "\n")
        cat("  -Number of clusters:", Communities.Community(g).Values.Distinct.Count, "\n")
        cat("  -Modularity of each clusters: ", qsub.GetJson, "\n")
        cat("\n")

        Return community
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="links">
    ''' ["from","to","weight"]
    ''' </param>
    ''' <returns></returns>
    ''' 
    <Extension>
    Private Function AsGraph(links As NumericMatrix) As NetworkGraph
        Dim g As New NetworkGraph
        Dim from As String
        Dim [to] As String
        Dim weight As Double

        VBDebugger.Mute = True

        For Each row As Vector In links.RowVectors
            from = row(0)
            [to] = row(1)
            weight = row(2)

            If g.GetElementByID(from) Is Nothing Then
                Call g.CreateNode(from)
            End If
            If g.GetElementByID([to]) Is Nothing Then
                Call g.CreateNode([to])
            End If

            Call g.CreateEdge(
                u:=g.GetElementByID(from),
                v:=g.GetElementByID([to]),
                weight:=weight
            )
        Next

        VBDebugger.Mute = False

        Return g
    End Function
End Module
