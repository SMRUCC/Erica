﻿Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.NLP.LDA
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve
Imports SMRUCC.Rsharp.Runtime

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
    ''' <param name="make_gene_filters">
    ''' if your matrix contains no missing values, then you should set this parameter value to FALSE, 
    ''' otherwise all features will be removes: due to the reason of no missing value case the ratio
    ''' greater than the default <paramref name="max"/> filter threshold value.
    ''' </param>
    ''' <returns>
    ''' document model for run LDA modelling
    ''' </returns>
    ''' <example>
    ''' require(GCModeller);
    ''' 
    ''' imports "geneExpression" from "phenotype_kit";
    ''' 
    ''' let expr_mat = load.expr(file = "./expr.csv");
    ''' let corpus = STCorpus(expr_mat);
    ''' let LDA = fitLDA(corpus, k = 6);
    ''' let deconv = getBetaTheta(LDA, corpus, top.genes = 1000);
    ''' 
    ''' let matrix1 = singlecells(deconv, expr_mat);
    ''' let matrix2 = deconvolve(deconv, expr_mat);
    ''' </example>
    <ExportAPI("STCorpus")>
    Public Function STCorpus(matrix As Matrix,
                             Optional min As Double = 0.05,
                             Optional max As Double = 0.95,
                             Optional make_gene_filters As Boolean = True,
                             Optional unify As Integer = 10,
                             Optional logNorm As Boolean = True) As STCorpus

        Return matrix.CreateSpatialDocuments(min, max,
                                             unify:=unify,
                                             logNorm:=logNorm,
                                             make_gene_filters:=make_gene_filters)
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
                                    Optional n_threads As Integer? = Nothing,
                                    Optional env As Environment = Nothing) As LdaGibbsSampler

        If n_threads Is Nothing OrElse CInt(n_threads) < 1 Then
            n_threads = env.globalEnvironment.options.getOption("n_threads", [default]:=32)
        End If

        Return spatialDoc.LDAModelling(
            k, alpha, beta,
            iterations:=loops,
            println:=env.WriteLineHandler,
            n_threads:=n_threads
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
    ''' <param name="top_genes"></param>
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
    Public Function Deconvolve(LDA As LdaGibbsSampler, corpus As STCorpus, Optional top_genes As Integer = 25) As Deconvolve
        Return LDA.Deconvolve(corpus, top_genes)
    End Function

    ''' <summary>
    ''' get the single cell expression matrix
    ''' </summary>
    ''' <param name="deconv"></param>
    ''' <param name="expr"></param>
    ''' <returns>
    ''' An expression data matrix object that extends the gene feature 
    ''' sets with the multiple cell layers prefix
    ''' </returns>
    <ExportAPI("singlecells")>
    Public Function singleCellMatrix(deconv As Deconvolve, expr As Matrix, Optional prefix As String = "class") As Matrix
        Return deconv.GetSingleCellExpressionMatrix(expr, prefix)
    End Function

    ''' <summary>
    ''' get the spatial expression matrix
    ''' </summary>
    ''' <param name="deconv"></param>
    ''' <param name="expr"></param>
    ''' <returns></returns>
    <ExportAPI("deconvolve")>
    Public Function deconvMatrix(deconv As Deconvolve, expr As Matrix) As Matrix
        Return deconv.GetExpressionMatrix(expr)
    End Function
End Module
