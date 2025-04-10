% Generated by roxygen2: do not edit by hand
% Please edit documentation in R/functions.R
\name{combineTopics}
\alias{combineTopics}
\title{Aggregate cell-types in the same cluster into a single cell-type}
\usage{
combineTopics(mtx, clusters, type)
}
\arguments{
\item{mtx}{either a beta (cell-type gene distribution matrix) or a
theta (pixel-cell type distribution matrix)}

\item{clusters}{factor of the cell-types (names) and their assigned cluster (levels)}

\item{type}{either "t" or "b". Affects the adjustment to the combined
cell-type vectors. "b" divides summed cell-type vectors by number of aggregated cell-types}
}
\value{
matrix where cell-types are now cell-type-clusters
}
\description{
Note: for the beta matrix, each row is a cell-type, each column
is a gene. The cell-type row is a distribution of terms that sums to 1. So
combining cell-type row vectors, these should be adjusted such that the
rowSum == 1. As in, take average of the terms after combining. However, the
theta matrix (after inversion) will have cell-type rows and each column is a
document. Because the cell-type can be represented at various proportions in
each doc and these will not necessarily add to 1, should not take average.
Just sum cell-type row vectors together. This way, each document column still
adds to 1 when considering the proportion of each cell-type-cluster in the document.
}
