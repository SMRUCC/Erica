% Generated by roxygen2: do not edit by hand
% Please edit documentation in R/functions.R
\name{filterTheta}
\alias{filterTheta}
\title{Function to filter out cell-types in pixels below a certain proportion}
\usage{
filterTheta(theta, perc.filt = 0.05)
}
\arguments{
\item{theta}{pixel (rows) by cell-types (columns) distribution matrix. Each row
is the cell-type composition for a given pixel}

\item{perc.filt}{proportion threshold to remove cell-types in pixels (default: 0.05)}
}
\value{
A filtered pixel (rows) by cell-types (columns) distribution matrix.
}
\description{
Sets cell-types in each pixel to 0 that are below a given proportion.
Then renormalizes the pixel proportions to sum to 1.
Cell-types that result in 0 in all pixels after this filtering step are removed.
}
