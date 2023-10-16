imports "geneExpression" from "phenotype_kit";
imports "STdeconvolve" from "Erica";

#' Make spatial matrix deconvolution
#' 
#' Reference-free cell-type deconvolution of pixel-resolution spatially resolved transcriptomics data
#' 
#' @param m a dataframe object or a GCModeller expression matrix object, the matrix
#'    object should be in format of spatial spot in rows and the gene features in
#'    columns.
#' @param n_layers an integer value for specific the cell layer class number
#' @param top_genes set the number of top genes when export the expression matrix 
#'    in each cell layers
#' 
#' @return A tuple list object that contains multiple result object inside:
#' 
#'     1. single_cells: an expression matrix object that contains the single cell data
#'     2. deconv_spatial: an expression matrix object that contains the cell 
#'           layer deconvolution result data
#'     3. cell_layers: the cell layer composition data
#'     4. gibbs_LDA: the result of the LDA gibbs sampling result outputs, the top
#'           features in each cell class
#' 
const deconv_spatial = function(expr_mat, n_layers = 4, top_genes = 1000, alpha = 2.0, 
                                beta = 0.5,
                                iteration = 150,
                                prefix = "class") {

    if (is.character(expr_mat)) {
        # read the matrix file
        expr_mat = geneExpression::load.expr(expr_mat);
    }
    if (is.data.frame(expr_mat)) {
        # convert to expression matrix
        expr_mat = geneExpression::load.expr(expr_mat);
    }

    let corpus = STCorpus(expr_mat);
    let LDA = fitLDA(corpus, k = n_layers, alpha = alpha,
            beta = beta,
            loops = iteration);
    let deconv = getBetaTheta(LDA, corpus, top.genes = top_genes);

    let matrix1 = singlecells(deconv, expr_mat, prefix = prefix);
    let matrix2 = deconvolve(deconv, expr_mat);
    let matrix3 = [deconv]::theta;
    let lda = [deconv]::topicMap; 

    {
        single_cells: matrix1,
        deconv_spatial: matrix2,
        cell_layers: matrix3
        gibbs_LDA: lapply(lda, l -> as.list(l), names = `${prefix}_${1:length(lda)}`)
    }
}

