imports "geneExpression" from "phenotype_kit";
imports "STdeconvolve" from "Erica";

#' Make spatial matrix deconvolution
#' 
#' Reference-free cell-type deconvolution of pixel-resolution spatially resolved transcriptomics data
#' 
#' @param expr_mat a dataframe object or a GCModeller expression matrix object, the matrix
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
#' @details the generated expression matrix object could be save as csv matrix by use
#'     the function ``geneExpression::write.expr_matrix`` from the GCModeller package. 
#' 
const deconv_spatial = function(expr_mat, n_layers = 4, top_genes = 1000, alpha = 2.0, 
                                beta = 0.5,
                                iteration = 150,
                                prefix = "class",
                                make_gene_filters = TRUE, 
                                filter_range = [0.05, 0.95],
                                unify_scale = 10,
                                log_norm = TRUE) {

    if (is.character(expr_mat)) {
        # read the matrix file
        expr_mat = geneExpression::load.expr(expr_mat);
    }
    if (is.data.frame(expr_mat)) {
        # convert to expression matrix
        expr_mat = geneExpression::load.expr(expr_mat);
    }

    let corpus = expr_mat |> STCorpus(make_gene_filters = make_gene_filters, 
        min = filter_range[1], 
        max = filter_range[2],
        unify = unify_scale, logNorm = log_norm
    );
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
        cell_layers: matrix3,
        spots_class: __spot_class(cell_layers = matrix3),
        gibbs_LDA: lapply(lda, l -> as.list(l), names = `${prefix}_${1:length(lda)}`)
    }
}

#' Create spot class annotation
#' 
const __spot_class = function(cell_layers, color = "paper") {
    let spatial = NULL;

    print("extract the spatial spot information...");

    cell_layers = as.data.frame(cell_layers);
    spatial = rownames(cell_layers);
    spatial = strsplit(spatial, ",");

    let xy = lapply(spatial, si -> as.numeric(si));
    let x = sapply(xy, i -> i[1]);
    let y = sapply(xy, i -> i[2]);
    let labels = colnames(cell_layers);

    print("extract the cell labels for each spatial spots...");

    labels = cell_layers 
    |> as.list(byrow = TRUE) 
    |> sapply(function(r) {
        which.max(as.numeric(unlist(r)));
    });
    
    print("cell labels for each spatial spot:");
    print(labels);

    print("generates the spatial spots annotation outputs...");

    spatial_annotations(
        x = as.numeric(x),
        y = as.numeric(y),
        label = colnames(cell_layers)[labels],
        colors = color
    );
}
