from math import sqrt
import numpy as np
import pandas as pd
from scipy.spatial.distance import pdist, squareform, euclidean
import networkx as nx
from sklearn import cluster
from lens import apply_lens
import scipy


def davies_bouldin(dist_mu, sigma):
    DB = 0
    K = len(sigma)
    for i in range(K):
        D_i = 0
        for j in range(K):
            if j == i:
                continue
            R_ij = (sigma[i] + sigma[j]) / dist_mu[i, j]
            if R_ij > D_i:
                D_i = R_ij
        DB += D_i
    return DB / K


def covering_patches(lens_data, resolution=10, gain=0.5, equalize=True):
    cols = lens_data.columns
    xmin, xmax = lens_data[cols[0]].min(), lens_data[cols[0]].max()
    ymin, ymax = lens_data[cols[1]].min(), lens_data[cols[1]].max()
    patch_dict = {}

    if equalize:
        perc_step = 100.0 / resolution
        fence_posts_x = [np.percentile(lens_data[cols[0]], post) for post in np.arange(perc_step, 100, perc_step)]
        fence_posts_y = [np.percentile(lens_data[cols[1]], post) for post in np.arange(perc_step, 100, perc_step)]

        lower_bound_x = np.array([xmin] + fence_posts_x)
        upper_bound_x = np.array(fence_posts_x + [xmax])
        lower_bound_y = np.array([ymin] + fence_posts_y)
        upper_bound_y = np.array(fence_posts_y + [ymax])

        widths_x = upper_bound_x - lower_bound_x
        spill_over_x = gain * widths_x
        lower_bound_x -= spill_over_x
        upper_bound_x += spill_over_x
        widths_y = upper_bound_y - lower_bound_y
        spill_over_y = gain * widths_y
        lower_bound_y -= spill_over_y
        upper_bound_y += spill_over_y

        for i in range(resolution):
            for j in range(resolution):
                patch = list(lens_data[(lens_data[cols[0]] > lower_bound_x[i]) &
                                       (lens_data[cols[0]] < upper_bound_x[i]) &
                                       (lens_data[cols[1]] > lower_bound_y[j]) &
                                       (lens_data[cols[1]] < upper_bound_y[j])].index)
                key = ((round(lower_bound_x[i], 2), round(upper_bound_x[i], 2)),
                       (round(lower_bound_y[j], 2), round(upper_bound_y[j], 2)))
                patch_dict[key] = patch
        return patch_dict

    else:
        width_x = (xmax - xmin) / resolution
        width_y = (ymax - ymin) / resolution
        spill_over_x = gain * width_x
        spill_over_y = gain * width_y

        lower_bound_x = np.arange(xmin, xmax, width_x) - spill_over_x
        upper_bound_x = np.arange(xmin, xmax, width_x) + width_x + spill_over_x
        lower_bound_y = np.arange(ymin, ymax, width_y) - spill_over_y
        upper_bound_y = np.arange(ymin, ymax, width_y) + width_y + spill_over_y
        for i in range(resolution):
            for j in range(resolution):
                patch = list(lens_data[(lens_data[cols[0]] > lower_bound_x[i]) &
                                       (lens_data[cols[0]] < upper_bound_x[i]) &
                                       (lens_data[cols[1]] > lower_bound_y[j]) &
                                       (lens_data[cols[1]] < upper_bound_y[j])].index)
                key = ((round(lower_bound_x[i], 2), round(upper_bound_x[i], 2)),
                       (round(lower_bound_y[j], 2), round(upper_bound_y[j], 2)))
                patch_dict[key] = patch
        return patch_dict


def gap(data, refs=None, nrefs=20, ks=range(1,11), method=None):
    shape = data.shape
    if refs is None:
        tops = data.max(axis=0)
        bots = data.min(axis=0)
        dists = scipy.matrix(scipy.diag(tops-bots))

        rands = scipy.random.random_sample(size=(shape[0], shape[1], nrefs))
        for i in range(nrefs):
            rands[:, :, i] = rands[:, :, i]*dists+bots
    else:
        rands = refs
    gaps = scipy.zeros((len(ks),))
    for (i, k) in enumerate(ks):
        g1 = method(n_clusters=k).fit(data)
        (kmc, kml) = (g1.cluster_centers_, g1.labels_)
        disp = sum([euclidean(data[m, :], kmc[kml[m], :]) for m in range(shape[0])])

        refdisps = scipy.zeros((rands.shape[2],))
        for j in range(rands.shape[2]):
            g2 = method(n_clusters=k).fit(rands[:, :, j])
            (kmc, kml) = (g2.cluster_centers_, g2.labels_)
            refdisps[j] = sum([euclidean(rands[m, :, j], kmc[kml[m],:]) for m in range(shape[0])])
        gaps[i] = scipy.log(scipy.mean(refdisps))-scipy.log(disp)
    return gaps


def optimal_clustering(df, patch, method='kmeans', statistic='gap', max_K=5):
    if len(patch) == 1:
        return [patch]

    if statistic == 'db':
        if method == 'kmeans':
            if len(patch) <= 5:
                K_max = 2
            else:
                K_max = min(len(patch) / 2, max_K)
            clustering = {}
            db_index = []
            X = df.ix[patch, :]
            for k in range(2, K_max + 1):
                kmeans = cluster.KMeans(n_clusters=k).fit(X)
                clustering[k] = pd.DataFrame(kmeans.predict(X), index=patch)
                dist_mu = squareform(pdist(kmeans.cluster_centers_))
                sigma = []
                for i in range(k):
                    points_in_cluster = clustering[k][clustering[k][0] == i].index
                    sigma.append(sqrt(X.ix[points_in_cluster, :].var(axis=0).sum()))
                db_index.append(davies_bouldin(dist_mu, np.array(sigma)))
            db_index = np.array(db_index)
            k_optimal = np.argmin(db_index) + 2
            return [list(clustering[k_optimal][clustering[k_optimal][0] == i].index) for i in range(k_optimal)]

        elif method == 'agglomerative':
            if len(patch) <= 5:
                K_max = 2
            else:
                K_max = min(len(patch) / 2, max_K)
            clustering = {}
            db_index = []
            X = df.ix[patch, :]
            for k in range(2, K_max + 1):
                agglomerative = cluster.AgglomerativeClustering(n_clusters=k, linkage='average').fit(X)
                clustering[k] = pd.DataFrame(agglomerative.fit_predict(X), index=patch)
                tmp = [list(clustering[k][clustering[k][0] == i].index) for i in range(k)]
                centers = np.array([np.mean(X.ix[c, :], axis=0) for c in tmp])
                dist_mu = squareform(pdist(centers))
                sigma = []
                for i in range(k):
                    points_in_cluster = clustering[k][clustering[k][0] == i].index
                    sigma.append(sqrt(X.ix[points_in_cluster, :].var(axis=0).sum()))
                db_index.append(davies_bouldin(dist_mu, np.array(sigma)))
            db_index = np.array(db_index)
            k_optimal = np.argmin(db_index) + 2
            return [list(clustering[k_optimal][clustering[k_optimal][0] == i].index) for i in range(k_optimal)]

    elif statistic == 'gap':
        X = np.array(df.ix[patch, :])
        if method == 'kmeans':
            f = cluster.KMeans
        gaps = gap(X, ks=range(1, min(max_K, len(patch))), method=f)
        k_optimal = list(gaps).index(max(gaps))+1
        clustering = pd.DataFrame(f(n_clusters=k_optimal).fit_predict(X), index=patch)
        return [list(clustering[clustering[0] == i].index) for i in range(k_optimal)]

    else:
        raise 'error: only db and gat statistics are supported'


def mapper_graph(df, lens_data=None, lens='pca', resolution=10, gain=0.5, equalize=True, clust='kmeans', stat='gap',
                 max_K=5):
    """
    input: N x n_dim image of of raw data under lens function, as a dataframe
    output: (undirected graph, list of node contents, dictionary of patches)
    """
    if lens_data is None:
        lens_data = apply_lens(df, lens=lens)

    patch_clusterings = {}
    counter = 0
    patches = covering_patches(lens_data, resolution=resolution, gain=gain, equalize=equalize)
    for key, patch in patches.items():
        if len(patch) > 0:
            patch_clusterings[key] = optimal_clustering(df, patch, method=clust, statistic=stat, max_K=max_K)
            counter += 1
    print 'total of {} patches required clustering'.format(counter)

    all_clusters = []
    for key in patch_clusterings:
        all_clusters += patch_clusterings[key]
    num_nodes = len(all_clusters)
    print 'this implies {} nodes in the mapper graph'.format(num_nodes)

    A = np.zeros((num_nodes, num_nodes))
    for i in range(num_nodes):
        for j in range(i):
            overlap = set(all_clusters[i]).intersection(set(all_clusters[j]))
            if len(overlap) > 0:
                A[i, j] = 1
                A[j, i] = 1

    G = nx.from_numpy_matrix(A)
    total = []
    all_clusters_new = []
    mapping = {}
    cont = 0
    for m in all_clusters:
        total += m
    for n, m in enumerate(all_clusters):
        if len(m) == 1 and total.count(m) > 1:
            G.remove_node(n)
        else:
            all_clusters_new.append(m)
            mapping[n] = cont
            cont += 1
    H = nx.relabel_nodes(G, mapping)
    return H, all_clusters_new, patches
