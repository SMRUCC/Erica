% Generated by roxygen2: do not edit by hand
% Please edit documentation in R/import.R
\name{winsorize}
\alias{winsorize}
\title{Function to winsorize the gene counts}
\usage{
winsorize(x, qt = 0.05)
}
\arguments{
\item{x}{gene count matrix}

\item{qt}{quantile for winsorization (defaul: 0.05)}
}
\value{
winsorized gene count matrix
}
\description{
Function to winsorize the gene counts
}
