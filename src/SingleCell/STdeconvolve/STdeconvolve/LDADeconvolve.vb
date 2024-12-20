Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Data.NLP.LDA
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.Parallel
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports std = System.Math

''' <summary>
''' ## Reference-free cell-type deconvolution of multi-cellular pixel-resolution spatially resolved transcriptomics data
''' 
''' do spatial data deconvolve via NLP LDA algorithm
''' 
''' Recent technological advancements have enabled spatially resolved 
''' transcriptomic profiling but at multi-cellular pixel resolution, 
''' thereby hindering the identification of cell-type-specific spatial 
''' patterns and gene expression variation. To address this challenge, 
''' we developed STdeconvolve as a reference-free approach to deconvolve 
''' underlying cell-types comprising such multi-cellular pixel 
''' resolution spatial transcriptomics (ST) datasets. Using simulated as 
''' well as real ST datasets from diverse spatial transcriptomics 
''' technologies comprising a variety of spatial resolutions such as 
''' Spatial Transcriptomics, 10X Visium, DBiT-seq, and Slide-seq, we show 
''' that STdeconvolve can effectively recover cell-type transcriptional 
''' profiles and their proportional representation within pixels without 
''' reliance on external single-cell transcriptomics references. 
''' STdeconvolve provides comparable performance to existing reference-based 
''' methods when suitable single-cell references are available, as well 
''' as potentially superior performance when suitable single-cell references 
''' are not available. STdeconvolve is available as an open-source R 
''' software package with the source code available at 
''' https://github.com/JEFworks-Lab/STdeconvolve.
''' </summary>
''' <remarks>
''' https://www.biorxiv.org/content/10.1101/2021.06.15.448381v2
''' doi: https://doi.org/10.1101/2021.06.15.448381
''' </remarks>
Public Module LDADeconvolve

    ''' <summary>
    ''' [1] Create document vector for run LDA mdelling
    ''' </summary>
    ''' <param name="matrix">
    ''' row is pixels and column is gene features. each 
    ''' pixel row is a document sample in LDA model
    ''' </param>
    ''' <param name="min"></param>
    ''' <param name="max"></param>
    ''' <returns>
    ''' document model for run LDA modelling
    ''' </returns>
    <Extension>
    Public Function CreateSpatialDocuments(matrix As Matrix,
                                           Optional min As Double = 0.05,
                                           Optional max As Double = 0.95,
                                           Optional unify As Integer = 10,
                                           Optional make_gene_filters As Boolean = True,
                                           Optional logNorm As Boolean = True) As STCorpus
        If make_gene_filters Then
            ' reduce the gene features in pixels [5% ~ 95%]
            matrix = matrix.Project(
                sampleNames:=matrix.sampleID - matrix.GeneFilter(min, max))
        End If

        ' and then unify the count matrix via log scale and a given unify levels
        matrix = matrix.UnifyMatrix(unify, logNorm)

        Return matrix.Documentaries
    End Function

    ''' <summary>
    ''' [2] run LDA modelling
    ''' </summary>
    ''' <param name="spatialDoc"></param>
    ''' <param name="k"></param>
    ''' <param name="iterations">number of total iterations </param>
    ''' <param name="burnIn">number of burn-in iterations </param>
    ''' <param name="thinInterval">update statistics interval </param>
    ''' <param name="sampleLag">sample interval (-1 for just one sample at the end) </param>
    ''' <returns></returns>
    <Extension>
    Public Function LDAModelling(spatialDoc As STCorpus, k As Integer,
                                 Optional alpha# = 2.0,
                                 Optional beta# = 0.5,
                                 Optional iterations As Integer = 500,
                                 Optional burnIn As Integer = 100,
                                 Optional thinInterval As Integer = 20,
                                 Optional sampleLag As Integer = 10,
                                 Optional n_threads As Integer = 4,
                                 Optional println As Action(Of Object) = Nothing) As LdaGibbsSampler
        ' 2. Create a LDA sampler
        Dim ldaGibbsSampler As New LdaGibbsSampler(
            documents:=spatialDoc.Document(),
            V:=spatialDoc.VocabularySize(),
            log:=println
        )

        If n_threads <= 1 Then
            VectorTask.n_threads = 1
        Else
            VectorTask.n_threads = n_threads
        End If

        ' 3. Train LDA model via gibbs sampling
        Call ldaGibbsSampler _
            .configure(iterations, burnIn, thinInterval, sampleLag) _
            .gibbs(k, alpha, beta)

        Return ldaGibbsSampler
    End Function

    ''' <summary>
    ''' [3] get deconvolve result matrix
    ''' </summary>
    ''' <param name="LDA"></param>
    ''' <param name="corpus"></param>
    ''' <param name="topGenes"></param>
    ''' <returns></returns>
    <Extension>
    Public Function Deconvolve(LDA As LdaGibbsSampler, corpus As STCorpus, Optional topGenes As Integer = 25) As Deconvolve
        ' 4. The phi matrix Is a LDA model, you can use LdaInterpreter to explain it.
        Dim phi = LDA.Phi()
        Dim topicMap = LdaInterpreter.translate(
            phi:=phi,
            vocabulary:=corpus.Vocabulary,
            limit:=std.Min(topGenes, corpus.VocabularySize)
        )
        Dim t As DataFrameRow() = LDA.Theta _
            .Select(Function(dist, i)
                        ' each pixel Is defined as a mixture of 𝐾 cell types 
                        ' represented As a multinomial distribution Of cell-type 
                        ' probabilities
                        Return New DataFrameRow With {
                            .geneID = corpus.m_pixels(i),
                            .experiments = dist _
                                .Select(Function(d)
                                            Return If(d < 0, 0, std.Sqrt(d))
                                        End Function) _
                                .ToArray
                        }
                    End Function) _
            .ToArray

        Return New Deconvolve With {
            .topicMap = topicMap,
            .theta = New Matrix With {
                .expression = t,
                .sampleID = Enumerable _
                    .Range(1, LDA.K) _
                    .Select(Function(i) $"class_{i}") _
                    .ToArray,
                .tag = "theta"
            }
        }
    End Function

End Module
