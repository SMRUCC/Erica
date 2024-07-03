#' Non-negative Matrix Factorization (NMF) on single cells data
#' 
#' @details NMF has become a powerful tool in the analysis of single-cell genomics 
#' data due to its ability to decompose high-dimensional data into a lower-dimensional 
#' representation that captures biologically meaningful patterns. In the context of 
#' single-cell transcriptomics, for example, NMF can be used to identify distinct
#' cell types or states by clustering the gene expression profiles of individual 
#' cells.
#' 
#' Here's how NMF is applied in single-cell genomics data analysis:
#' 
#' 1. **Data Representation**: Single-cell transcriptomic data is typically presented 
#'    as a matrix, where rows represent genes and columns represent individual cells. 
#'    The matrix entries denote the expression levels of genes in cells, often in the 
#'    form of counts or normalized expression values.
#' 2. **Dimensionality Reduction**: Due to the high dimensionality of single-cell data 
#'    (tens of thousands of genes per cell), dimensionality reduction techniques like 
#'    NMF are essential to extract relevant information. NMF reduces the gene expression
#'    matrix to a lower-dimensional space by factorizing it into two non-negative 
#'    matrices: W (basis matrix) and H (coefficients matrix). The basis matrix W represents
#'    underlying features (potential cell types or states), while the coefficient matrix
#'    H captures the membership strength of each cell to these features.
#' 3. **Biological Interpretation**: The non-negative nature of NMF ensures that the 
#'    identified features (rows in W) and the cell memberships (columns in H) are 
#'    composed of non-negative combinations of genes and cells, respectively. This property 
#'    makes the results more interpretable in a biological context, as the features often 
#'    correspond to distinct biological states or pathways.
#' 4. **Clustering and Subtyping**: NMF can be used to cluster cells based on their 
#'    expression profiles. The columns of the coefficient matrix H can be treated as 
#'    cluster assignments, with each column representing a cell's membership in the 
#'    discovered features. This allows researchers to identify and annotate different 
#'    cell types or states present in the dataset.
#' 5. **Differential Expression Analysis**: By comparing the basis matrices obtained 
#'    from NMF applied to different conditions or treatments, researchers can identify 
#'    differentially expressed genes that are associated with specific cell states or 
#'    transitions.
#' 6. **Imputation and Noise Reduction**: NMF can also be used to impute missing values 
#'    in single-cell data and reduce noise, improving the overall quality of the dataset.
#' 7. **Integration and Alignment**: NMF can be applied to integrate data from 
#'    different samples or experiments, aligning them to a common feature space 
#'    and facilitating comparison across conditions.
#' 
#' In summary, NMF is a versatile method for the analysis of single-cell genomics data,
#' providing a means to uncover biologically relevant patterns, reduce noise, and 
#' integrate information across different samples or conditions. Its non-negative 
#' constraints make it particularly suitable for gene expression data, where the 
#' interpretation of the factors corresponds to underlying biological processes.
#' 
const single_nmf = function(x, dims = 9, max_iters = 1000, n_threads = 8) {
    options(n_threads = n_threads);

    let labels = `N_${rownames(x)}`;
    let embedding = nmf(x, rank = dims,  max_iterations = max_iters);  

    # embedding result
    let w = as.data.frame(embedding$W);
    let h = as.data.frame(embedding$H);

    rownames(w) <- labels;
    rownames(h) <- `x${1:ncol(w)}`;

    list(embedding = w, features = h, 
        cost = embedding$cost);
}