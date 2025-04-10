% Generated by roxygen2: do not edit by hand
% Please edit documentation in R/misc.R
\name{SPOTlightPredict}
\alias{SPOTlightPredict}
\title{Wrapper around functions in \code{SPOTlight::spotlight_deconvolution()}
that take place subsequently after training the NMF model.}
\usage{
SPOTlightPredict(nmfRef, stCounts, min_cont = 0, normCtTopicProfiles = FALSE)
}
\arguments{
\item{nmfRef}{the list returned from \code{SPOTlight::train_nmf()}. For \code{SPOTlight::spotlight_deconvolution()},
this returned list actually has this nmfRef input list as its first element, i.e. nmfRef[\link{1}]}

\item{stCounts}{ST count matrix to deconvolve, genes x spots}

\item{min_cont}{param of \code{mixture_deconvolution_nmf()}. remove topics less than this percent in a spot}

\item{normCtTopicProfiles}{if TRUE, uses the normalized cell-type topic-proportions instead of the
unnormalized cell-type topic coefficients that is typically used by SPOTlight. (default: FALSE)}
}
\value{
a list that contains
\itemize{
\item betaTopics: t(w), # topic x gene weights that sum to 1 (all genes and topics)
\item betaCt: ct_beta, # CellType x gene weights that sum to 1 (all genes and topics)
\item ctTopicProps: ct_topics, # topic proportion for each cell type
\item thetaTopics: t(topics_in_spots_norm), # proportion of each topic in each spot (all topics)
\item thetaCt: ct_in_spots_clean)) # spot x cell type proportions (all spots and cell types)
}
}
\description{
Allows for applying a trained NMF model to additional
ST datasets without retraining a new NMF in spotlight, which can take
a very long time (hours depending on number of genes and clusters in scRNAseq ref).\preformatted{Additionally, other useful matrices and information can also be collected
that are not returned by `SPOTlight::spotlight_deconvolution()`.

This does come with a caveat that one must be careful about:
WARNING: SPOTlight functions crash the R session if you use a corpus with gene(s)
that is not in the NMF W. I think this happens in `SPOTlight::predict_spatial_mixtures()`
during the nnls step at the end. Because it generates the full W matrix after selecting
genes in the ST mtrx. When the NMF was trained, there was a step that made it only
train on the intersecting genes with the ST data set. So under normal circumstances this would not be noticed
because all genes should match between W and ST mtrx. But if one were to use a different
ST mtrx on the trained NMF, then I bet it would crash because the matrices end up not being comparable shapes.
}
}
