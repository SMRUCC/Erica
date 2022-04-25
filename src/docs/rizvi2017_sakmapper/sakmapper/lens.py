import pandas as pd
from sklearn import decomposition, manifold, metrics


def apply_lens(df, lens='pca', dist='euclidean', n_dim=2, **kwargs):
    """
    input: N x F dataframe of observations
    output: N x n_dim image of input data under lens function
    """
    if n_dim != 2:
        raise 'error: image of data set must be two-dimensional'
    if dist not in ['euclidean', 'correlation']:
        raise 'error: only euclidean and correlation distance metrics are supported'
    if lens == 'pca' and dist != 'euclidean':
        raise 'error: PCA requires the use of euclidean distance metric'

    if lens == 'pca':
        df_lens = pd.DataFrame(decomposition.PCA(n_components=n_dim, **kwargs).fit_transform(df), df.index)
    elif lens == 'mds':
        D = metrics.pairwise.pairwise_distances(df, metric=dist)
        df_lens = pd.DataFrame(manifold.MDS(n_components=n_dim, **kwargs).fit_transform(D), df.index)
    elif lens == 'neighbor':
        D = metrics.pairwise.pairwise_distances(df, metric=dist)
        df_lens = pd.DataFrame(manifold.SpectralEmbedding(n_components=n_dim, **kwargs).fit_transform(D), df.index)
    else:
        raise 'error: only PCA, MDS, neighborhood lenses are supported'
    
    return df_lens