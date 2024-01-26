#Region "Microsoft.VisualBasic::83e1032723f0b7a0bd2c47553b8d70c7, R#\phenotype_kit\phenograph.vb"

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

' Module phenograph
' 
'     Function: phenograph
' 
' /********************************************************************************/

#End Region

Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.DataStructures
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Data.GraphTheory.KNearNeighbors
Imports Microsoft.VisualBasic.Data.visualize.Network.Analysis
Imports Microsoft.VisualBasic.Data.visualize.Network.FileStream.Generic
Imports Microsoft.VisualBasic.Data.visualize.Network.Graph
Imports Microsoft.VisualBasic.DataMining.BinaryTree
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.Parallel
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.Analysis.SingleCell.PhenoGraph
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SMRUCC.Rsharp.Runtime.Interop
Imports SMRUCC.Rsharp.Runtime.Vectorization
Imports Matrix = SMRUCC.genomics.Analysis.HTS.DataFrame.Matrix

''' <summary>
''' PhenoGraph algorithm
''' 
''' Jacob H. Levine and et.al. Data-Driven Phenotypic Dissection of AML Reveals Progenitor-like Cells that Correlate with Prognosis. Cell, 2015.
''' </summary>
<Package("phenograph")>
Module phenograph

    ''' <summary>
    ''' Run PhenoGraph algorithm
    ''' </summary>
    ''' <param name="x">This parameter should be an expression matrix data object</param>
    ''' <param name="k">The KNN parameter for the phenograph analysis on the input matrix <paramref name="x"/></param>
    ''' <param name="link_cutoff"></param>
    ''' <param name="subcomponents_filter">
    ''' removes small subnetwork
    ''' </param>
    ''' <returns></returns>
    ''' <example>
    ''' require(GCModeller);
    ''' 
    ''' imports "geneExpression" from "phenotype_kit";
    ''' 
    ''' let expr_mat = load.expr(file = "./expr.csv");
    ''' let clusters = phenograph(expr_mat, k = 16);
    ''' 
    ''' require(igraph);
    ''' 
    ''' # get the cluster result
    ''' print(as.data.frame(V(clusters)));
    ''' print(as.data.frame(E(clusters)));
    ''' </example>
    <ExportAPI("phenograph")>
    <RApiReturn(GetType(NetworkGraph))>
    Public Function phenograph(x As Matrix,
                               Optional y As Matrix = Nothing,
                               Optional k As Integer = 30,
                               Optional link_cutoff As Double = 0,
                               Optional knn_cutoff As Double = 0,
                               Optional score As ScoreMetric = Nothing,
                               Optional subcomponents_filter As Integer = 0,
                               Optional knn2 As Integer = 16,
                               Optional joint_cutoff As Double = 0,
                               Optional n_threads As Integer = 32,
                               Optional env As Environment = Nothing) As Object

        VectorTask.n_threads = n_threads

        Dim p1 As NetworkGraph = x.phenograph1(k, link_cutoff, knn_cutoff, score, subcomponents_filter)
        Dim p2 As NetworkGraph

        If y Is Nothing Then
            Return p1
        ElseIf score Is Nothing Then
            Return Internal.debug.stop("analysis of two expression data matrix required of the 'score' metric not null!", env)
        Else
            p2 = y.phenograph1(k, link_cutoff, knn_cutoff, score, subcomponents_filter)
        End If

        Dim xset = x.expression.ToDictionary(Function(gene) gene.geneID)
        Dim yset = y.Project(x.sampleID).expression.ToDictionary(Function(gene) gene.geneID)
        Dim p2v = p2.vertex.Select(Function(v) v.label).ToArray
        Dim knn = p1.vertex _
            .AsParallel _
            .Select(Function(v)
                        Dim xv = xset(v.label)
                        Dim cor = p2v.Select(Function(y2)
                                                 Dim yv = yset(y2)
                                                 Dim cor2 = score.eval(xv.experiments, yv.experiments)

                                                 Return (y2, cor2)
                                             End Function) _
                                     .Where(Function(i) i.cor2 > joint_cutoff) _
                                     .OrderByDescending(Function(i) i.cor2) _
                                     .Take(knn2) _
                                     .ToArray

                        Return (x:=v.label, y:=cor)
                    End Function) _
            .ToArray

        Dim graph As New NetworkGraph

        For Each v In p1.vertex.JoinIterates(p2.vertex)
            Call graph.CreateNode(v.label, v.data)
        Next
        For Each link In p1.graphEdges.JoinIterates(p2.graphEdges)
            Call graph.CreateEdge(
                u:=graph.GetElementByID(link.U.label),
                v:=graph.GetElementByID(link.V.label),
                weight:=link.weight,
                data:=link.data
            )
        Next
        For Each cross In knn
            For Each hit In cross.y
                Call graph.CreateEdge(
                    u:=graph.GetElementByID(cross.x),
                    v:=graph.GetElementByID(hit.y2),
                    weight:=hit.cor2
                )
            Next
        Next

        Return Communities.Analysis(graph)
    End Function

    <Extension>
    Private Function phenograph1(x As Matrix,
                                 k As Integer,
                                 link_cutoff As Double,
                                 knn_cutoff As Double,
                                 score As ScoreMetric,
                                 subcomponents_filter As Integer) As NetworkGraph

        Dim sampleId = x.sampleID.SeqIterator.ToArray
        Dim dataset As DataSet() = x.expression _
            .Select(Function(gene)
                        Return New DataSet With {
                            .ID = gene.geneID,
                            .Properties = sampleId _
                                .ToDictionary(Function(id) id.value,
                                              Function(i)
                                                  Return gene.experiments(i)
                                              End Function)
                        }
                    End Function) _
            .ToArray
        Dim graph As NetworkGraph = CommunityGraph.CreatePhenoGraph(
            data:=dataset,
            k:=k,
            link_cutoff:=link_cutoff,
            score:=score,
            knn_cutoff:=knn_cutoff,
            subcomponents_filter:=subcomponents_filter
        )

        Return graph
    End Function

    ''' <summary>
    ''' create a new score metric for KNN method in phenograph algorithm
    ''' </summary>
    ''' <param name="metric">
    ''' 1. cosine: the cosine similarity score
    ''' 2. jaccard: the jaccard similarity score
    ''' 3. pearson: the pearson correlation score(WGCNA co-expression weight actually)
    ''' 4. spearman: the spearman correlation score(WGCNA spearman weight score)
    ''' </param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("score_metric")>
    Public Function scoreMetric(<RRawVectorArgument(GetType(String))>
                                Optional metric As Object = "cosine|jaccard|pearson|spearman",
                                Optional env As Environment = Nothing) As ScoreMetric

        Dim strs As String() = CLRVector.asCharacter(metric)

        If strs.IsNullOrEmpty Then
            Return Nothing
        Else
            Select Case strs.First.ToLower
                Case "cosine" : Return New Cosine
                Case "jaccard" : Return New Jaccard
                Case "pearson" : Return New Pearson
                Case "spearman" : Return New Spearman
                Case Else
                    Return Nothing
            End Select
        End If
    End Function

    <ExportAPI("setInteraction")>
    Public Function setIteractions(g As NetworkGraph,
                                   geneIds As String(),
                                   metaboliteIds As String()) As NetworkGraph

        Dim geneIndex As Index(Of String) = geneIds.Indexing
        Dim metaboliteIndex As Index(Of String) = metaboliteIds.Indexing

        For Each link As Edge In g.graphEdges
            Dim t1 As String = If(link.U.label Like geneIndex, "gene", If(link.U.label Like metaboliteIndex, "metabolite", "unknown"))
            Dim t2 As String = If(link.V.label Like geneIndex, "gene", If(link.V.label Like metaboliteIndex, "metabolite", "unknown"))
            Dim type As String

            If t1 = "gene" AndAlso t2 = "gene" Then
                type = "gene interaction"
            ElseIf t1 = "metabolite" AndAlso t2 = "metabolite" Then
                type = "metabolite interaction"
            ElseIf t1 <> "unknown" AndAlso t2 <> "unknown" Then
                type = "cross interaction"
            Else
                type = "un-assigned"
            End If

            link.data(NamesOf.REFLECTION_ID_MAPPING_INTERACTION_TYPE) = type
        Next

        For Each v As Node In g.vertex
            If v.label Like geneIndex Then
                v.data("group") = "gene"
            ElseIf v.label Like metaboliteIndex Then
                v.data("group") = "metabolite"
            End If
        Next

        Dim clusters = Communities.GetCommunitySet(g)
        Dim metaboSet As i32 = 1
        Dim geneSet As i32 = 1
        Dim crossSet As i32 = 1

        For Each cluster In clusters
            If cluster.Value.All(Function(v) v.data("group") = "metabolite") Then
                Dim tag As String = $"metaboSet_{++metaboSet}"

                For Each v In cluster.Value
                    v.data(NamesOf.REFLECTION_ID_MAPPING_NODETYPE) = tag
                Next
            ElseIf cluster.Value.All(Function(v) v.data("group") = "gene") Then
                Dim tag As String = $"geneSet_{++geneSet}"

                For Each v In cluster.Value
                    v.data(NamesOf.REFLECTION_ID_MAPPING_NODETYPE) = tag
                Next
            Else
                Dim tag As String = $"crossSet_{++crossSet}"

                For Each v In cluster.Value
                    v.data(NamesOf.REFLECTION_ID_MAPPING_NODETYPE) = tag
                Next
            End If
        Next

        Return g
    End Function

    ''' <summary>
    ''' set cluster colors of the phenograph result
    ''' </summary>
    ''' <param name="g"></param>
    ''' <param name="colorSet"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("cluster_colors")>
    <RApiReturn(GetType(NetworkGraph))>
    Public Function ClusterColors(g As NetworkGraph,
                                  <RRawVectorArgument>
                                  Optional colorSet As Object = "viridis:turbo",
                                  Optional env As Environment = Nothing) As Object

        Dim palette = RColorPalette.getColorSet(colorSet, [default]:="Paper")
        Dim groupDesc = Communities.GetCommunitySet(g) _
            .OrderByDescending(Function(v) v.Value.Length) _
            .ToArray
#Disable Warning
        Dim colors As LoopArray(Of SolidBrush) = Designer _
            .GetColors(palette, n:=groupDesc.Where(Function(c) c.Value.Length > 4).Count) _
            .Select(Function(c) New SolidBrush(c)) _
            .ToArray
#Enable Warning

        For Each group In groupDesc
            Dim color As Brush = ++colors

            For Each v In group.Value
                v.data.color = color
            Next
        Next

        Return g
    End Function

    <ExportAPI("graph_tree")>
    <RApiReturn(GetType(ClusterTree))>
    Public Function GraphTree(x As Matrix, Optional eq As Double = 0.5, Optional gt As Double = 0) As Object
        Return x.CreateGraph(eq, gt)
    End Function

    <ExportAPI("correlation_graph")>
    Public Function CorrelationGraph(x As Matrix, y As Matrix, Optional eq As Double = 0.85) As Object
        Dim spatialMapping = SpatialGraph.CorrelationGraph(x, y, eq).ToArray
        Dim mapList As New list With {.slots = New Dictionary(Of String, Object)}
        Dim i As i32 = 1
        Dim uniq As String
        Dim maps As list

        For Each mapping As (spotX As String(), spotY As String()) In spatialMapping
            uniq = mapping.spotX _
                .JoinIterates(mapping.spotY) _
                .JoinBy("+") _
                .MD5
            maps = New list With {
                .slots = New Dictionary(Of String, Object) From {
                    {"x", mapping.spotX},
                    {"y", mapping.spotY}
                }
            }

            Call mapList.slots.Add(uniq, maps)
        Next

        Return mapList
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="x"></param>
    ''' <param name="mapping">
    ''' the spatial spot mapping result which is evaluated from the ``correlation_graph`` function.
    ''' </param>
    ''' <param name="axis"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("slice_matrix")>
    <RApiReturn(GetType(Matrix))>
    Public Function SliceMatrix(x As Matrix, mapping As list,
                                <RRawVectorArgument(GetType(String))>
                                Optional axis As Object = "x|y",
                                Optional env As Environment = Nothing) As Object

        Dim axis_i As String = CLRVector.asCharacter(axis).FirstOrDefault("x")

        If Not (axis_i = "x" OrElse axis_i = "y") Then
            Return Internal.debug.stop("the axis value for the mapping should be value x or y!", env)
        End If

        Dim spots As New List(Of DataFrameRow)
        Dim spotIndex = x.expression.ToDictionary(Function(i) i.geneID)
        Dim w As Integer = x.sampleID.Length

        For Each guid As String In mapping.getNames
            Dim maps As list = mapping.getByName(guid)
            Dim spotId As String()

            If axis_i = "x" Then
                spotId = CLRVector.asCharacter(maps!x)
            Else
                spotId = CLRVector.asCharacter(maps!y)
            End If

            Dim v = spotId.Select(Function(id) spotIndex(id).CreateVector).Sum(width:=w)
            Dim slice As New DataFrameRow With {
                .geneID = guid,
                .experiments = v.ToArray
            }

            Call spots.Add(slice)
        Next

        Return New Matrix With {
            .expression = spots.ToArray,
            .sampleID = x.sampleID
        }
    End Function
End Module
