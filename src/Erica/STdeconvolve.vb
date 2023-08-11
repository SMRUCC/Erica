Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.NLP.LDA
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Invokes

''' <summary>
''' Reference-free cell-type deconvolution of pixel-resolution spatially resolved transcriptomics data
''' </summary>
<Package("STdeconvolve")>
Module STdeconvolve

    ''' <summary>
    ''' Create document vector for run LDA mdelling
    ''' </summary>
    ''' <param name="matrix">
    ''' row is pixels and column is gene features. each 
    ''' pixel row is a document sample in LDA model
    ''' </param>
    ''' <param name="min"></param>
    ''' <param name="max"></param>
    ''' <param name="unify"></param>
    ''' <param name="logNorm"></param>
    ''' <returns>
    ''' document model for run LDA modelling
    ''' </returns>
    <ExportAPI("STCorpus")>
    Public Function STCorpus(matrix As Matrix,
                             Optional min As Double = 0.05,
                             Optional max As Double = 0.95,
                             Optional unify As Integer = 10,
                             Optional logNorm As Boolean = True) As STCorpus

        Return matrix.CreateSpatialDocuments(min, max, unify, logNorm)
    End Function

    ''' <summary>
    ''' run LDA modelling
    ''' </summary>
    ''' <remarks>
    ''' Fit the optimal number of cell-types K for the LDA model
    ''' </remarks>
    ''' <param name="spatialDoc"></param>
    ''' <param name="k"></param>
    ''' <param name="alpha#"></param>
    ''' <param name="beta#"></param>
    ''' <returns></returns>
    <ExportAPI("fitLDA")>
    Public Function LdaGibbsSampler(spatialDoc As STCorpus, k As Integer,
                                    Optional alpha# = 2.0,
                                    Optional beta# = 0.5,
                                    Optional loops As Integer = 200,
                                    Optional env As Environment = Nothing) As LdaGibbsSampler

        Return spatialDoc.LDAModelling(
            k, alpha, beta,
            iterations:=loops,
            println:=env.WriteLineHandler
        )
    End Function

    ''' <summary>
    ''' ### get deconvolve result matrix
    ''' 
    ''' Pull out cell-type proportions across pixels (theta) and
    ''' cell-type gene probabilities (beta) matrices from fitted 
    ''' LDA models from fitLDA
    ''' </summary>
    ''' <param name="LDA">
    ''' an LDA model from `topicmodels`. From list of models returned by
    ''' fitLDA
    ''' </param>
    ''' <param name="corpus">
    ''' If corpus is NULL, then it will use the original corpus that
    ''' the model was fitted to. Otherwise, compute deconvolved topics from this
    ''' new corpus. Needs to be pixels x genes and nonnegative integer counts. 
    ''' Each row needs at least 1 nonzero entry (default: NULL)
    ''' </param>
    ''' <param name="topGenes"></param>
    ''' <returns>
    ''' A Deconvolve object that contains
    '''
    ''' + beta: cell-type (rows) by gene (columns) distribution matrix.
    '''   Each row is a probability distribution of a cell-type expressing 
    '''   each gene in the corpus.
    ''' + theta: pixel (rows) by cell-types (columns) distribution matrix.
    '''   Each row is the cell-type composition for a given pixel.
    ''' </returns>
    <ExportAPI("getBetaTheta")>
    Public Function Deconvolve(LDA As LdaGibbsSampler, corpus As STCorpus, Optional topGenes As Integer = 25) As Deconvolve
        Return LDA.Deconvolve(corpus, topGenes)
    End Function
End Module
