﻿// export R# package module type define for javascript/typescript language
//
// ref=Erica.STdeconvolve@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * Reference-free cell-type deconvolution of pixel-resolution spatially resolved transcriptomics data
 * 
*/
declare namespace STdeconvolve {
   /**
    * run LDA modelling
    * 
    * > Fit the optimal number of cell-types K for the LDA model
    * 
     * @param spatialDoc -
     * @param k -
     * @param alpha 
     * + default value Is ``2``.
     * @param beta 
     * + default value Is ``0.5``.
   */
   function fitLDA(spatialDoc: object, k: object, alpha?: number, beta?: number): object;
   /**
    * ### get deconvolve result matrix
    *  
    *  Pull out cell-type proportions across pixels (theta) and
    *  cell-type gene probabilities (beta) matrices from fitted 
    *  LDA models from fitLDA
    * 
    * 
     * @param LDA an LDA model from `topicmodels`. From list of models returned by
     *  fitLDA
     * @param corpus If corpus is NULL, then it will use the original corpus that
     *  the model was fitted to. Otherwise, compute deconvolved topics from this
     *  new corpus. Needs to be pixels x genes and nonnegative integer counts. 
     *  Each row needs at least 1 nonzero entry (default: NULL)
     * @param topGenes -
     * 
     * + default value Is ``25``.
     * @return A Deconvolve object that contains
     * 
     *  + beta: cell-type (rows) by gene (columns) distribution matrix.
     *    Each row is a probability distribution of a cell-type expressing 
     *    each gene in the corpus.
     *  + theta: pixel (rows) by cell-types (columns) distribution matrix.
     *    Each row is the cell-type composition for a given pixel.
   */
   function getBetaTheta(LDA: object, corpus: object, topGenes?: object): object;
   /**
    * Create document vector for run LDA mdelling
    * 
    * 
     * @param matrix row is pixels and column is gene features. each 
     *  pixel row is a document sample in LDA model
     * @param min -
     * 
     * + default value Is ``0.05``.
     * @param max -
     * 
     * + default value Is ``0.95``.
     * @param unify -
     * 
     * + default value Is ``10``.
     * @param logNorm -
     * 
     * + default value Is ``true``.
     * @return document model for run LDA modelling
   */
   function STCorpus(matrix: object, min?: number, max?: number, unify?: object, logNorm?: boolean): object;
}