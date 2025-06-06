#' RphenoGraph clustering
#'
#' R implementation of the PhenoGraph algorithm
#'
#' A simple R implementation of the [PhenoGraph](http://www.cell.com/cell/abstract/S0092-8674(15)00637-6) algorithm,
#' which is a clustering method designed for high-dimensional single-cell data analysis. It works by creating a graph ("network") representing
#' phenotypic similarities between cells by calclating the Jaccard coefficient between nearest-neighbor sets, and then identifying communities
#' using the well known [Louvain method](https://sites.google.com/site/findcommunities/) in this graph.
#'
#' @param data matrix; input data matrix
#' @param k integer; number of nearest neighbours (default:30)
#'
#' @return a list contains an igraph graph object for \code{graph_from_data_frame} and a communities object, the operations of this class contains:
#' \item{print}{returns the communities object itself, invisibly.}
#' \item{length}{returns an integer scalar.}
#' \item{sizes}{returns a numeric vector.}
#' \item{membership}{returns a numeric vector, one number for each vertex in the graph that was the input of the community detection.}
#' \item{modularity}{returns a numeric scalar.}
#' \item{algorithm}{returns a character scalar.}
#' \item{crossing}{returns a logical vector.}
#' \item{is_hierarchical}{returns a logical scalar.}
#' \item{merges}{returns a two-column numeric matrix.}
#' \item{cut_at}{returns a numeric vector, the membership vector of the vertices.}
#' \item{as.dendrogram}{returns a dendrogram object.}
#' \item{show_trace}{returns a character vector.}
#' \item{code_len}{returns a numeric scalar for communities found with the InfoMAP method and NULL for other methods.}
#' \item{plot}{for communities objects returns NULL, invisibly.}
#'
#' @references Jacob H. Levine and et.al. Data-Driven Phenotypic Dissection of AML Reveals Progenitor-like Cells that Correlate with Prognosis. Cell, 2015.
#' @examples
#' iris_unique <- unique(iris) # Remove duplicates
#' data <- as.matrix(iris_unique[,1:4])
#' Rphenograph_out <- Rphenograph(data, k = 45)
#' modularity(Rphenograph_out[[2]])
#' membership(Rphenograph_out[[2]])
#' iris_unique$phenograph_cluster <- factor(membership(Rphenograph_out[[2]]))
#' ggplot(iris_unique, aes(x=Sepal.Length, y=Sepal.Width, col=Species, shape=phenograph_cluster)) + geom_point(size = 3)+theme_bw()
#'
#' @importFrom igraph graph.data.frame cluster_louvain modularity membership
#' @import ggplot2
#' @useDynLib Rphenograph
#'
#' @export
Rphenograph <- function(data, k=30, n_threads = 8){
    if(is.data.frame(data))
        data <- as.matrix(data)

    if(!is.matrix(data))
        stop("Wrong input data, should be a data frame of matrix!")

    if(k<1){
        stop("k must be a positive integer!")
    }else if (k > nrow(data)-2){
        stop("k must be smaller than the total number of points!")
    }

    message("Run Rphenograph starts:","\n",
            "  -Input data of ", nrow(data)," rows and ", ncol(data), " columns","\n",
            "  -k is set to ", k)

    cat("  Finding nearest neighbors...")
    t1 <- system.time(neighborMatrix <- find_neighbors(data, k=k+1)[,-1])
    cat("DONE ~",t1[3],"s\n", " Compute jaccard coefficient between nearest-neighbor sets...")
    t2 <- system.time(links <- jaccard_coeff(neighborMatrix, n_threads));

    cat("DONE ~",t2[3],"s\n", " Build undirected graph from the weighted links...")
    links <- links[links[,1]>0, ]
    relations <- as.data.frame(links)
    colnames(relations)<- c("from","to","weight")
    t3 <- system.time(g <- graph.data.frame(relations, directed=FALSE))

    # Other community detection algorithms:
    #    cluster_walktrap, cluster_spinglass,
    #    cluster_leading_eigen, cluster_edge_betweenness,
    #    cluster_fast_greedy, cluster_label_prop
    cat("DONE ~",t3[3],"s\n", " Run louvain clustering on the graph ...")
    t4 <- system.time(community <- cluster_louvain(g))
    cat("DONE ~",t4[3],"s\n")

    message("Run Rphenograph DONE, totally takes ", sum(c(t1[3],t2[3],t3[3],t4[3])), "s.")
    cat("  Return a community class\n  -Modularity value:", modularity(community),"\n")
    cat("  -Number of clusters:", length(unique(membership(community))))

    return(list(g, community))
}
