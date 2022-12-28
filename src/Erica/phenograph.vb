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
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.DataStructures
Imports Microsoft.VisualBasic.Data.csv.IO
Imports Microsoft.VisualBasic.Data.visualize.Network.Analysis
Imports Microsoft.VisualBasic.Data.visualize.Network.FileStream.Generic
Imports Microsoft.VisualBasic.Data.visualize.Network.Graph
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.Analysis.SingleCell.PhenoGraph
Imports SMRUCC.Rsharp.Runtime.Interop
Imports REnv = SMRUCC.Rsharp.Runtime

''' <summary>
''' PhenoGraph algorithm
''' 
''' Jacob H. Levine and et.al. Data-Driven Phenotypic Dissection of AML Reveals Progenitor-like Cells that Correlate with Prognosis. Cell, 2015.
''' </summary>
<Package("phenograph")>
Module phenograph

    ''' <summary>
    ''' PhenoGraph algorithm
    ''' </summary>
    ''' <param name="matrix"></param>
    ''' <param name="k"></param>
    ''' <param name="link_cutoff"></param>
    ''' <param name="subcomponents_filter">
    ''' removes small subnetwork
    ''' </param>
    ''' <returns></returns>
    <ExportAPI("phenograph")>
    Public Function phenograph(matrix As Matrix,
                               Optional k As Integer = 30,
                               Optional link_cutoff As Double = 0,
                               Optional knn_cutoff As Double = 0,
                               Optional score As ScoreMetric = Nothing,
                               Optional subcomponents_filter As Integer = 0) As NetworkGraph

        Dim sampleId = matrix.sampleID.SeqIterator.ToArray
        Dim dataset As DataSet() = matrix.expression _
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
    ''' </param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("score_metric")>
    Public Function scoreMetric(<RRawVectorArgument(GetType(String))>
                                Optional metric As Object = "cosine|jaccard|pearson",
                                Optional env As Environment = Nothing) As ScoreMetric
        Dim strs As String() = REnv.asVector(Of String)(metric)

        If strs.IsNullOrEmpty Then
            Return Nothing
        Else
            Select Case strs.First.ToLower
                Case "cosine" : Return New Cosine
                Case "jaccard" : Return New Jaccard
                Case "pearson" : Return New Pearson
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
    Public Function ClusterColors(g As NetworkGraph,
                                  <RRawVectorArgument>
                                  Optional colorSet As Object = "viridis:turbo",
                                  Optional env As Environment = Nothing) As Object

        Dim palette = RColorPalette.getColorSet(colorSet, [default]:="Paper")
        Dim groupDesc = Communities.GetCommunitySet(g) _
            .OrderByDescending(Function(v) v.Value.Length) _
            .ToArray
        Dim colors As LoopArray(Of SolidBrush) = Designer _
            .GetColors(palette, n:=groupDesc.Where(Function(c) c.Value.Length > 4).Count) _
            .Select(Function(c) New SolidBrush(c)) _
            .ToArray

        For Each group In groupDesc
            Dim color As Brush = ++colors

            For Each v In group.Value
                v.data.color = color
            Next
        Next

        Return g
    End Function
End Module
