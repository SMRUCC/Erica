// export R# package module type define for javascript/typescript language
//
//    imports "phenograph" from "Erica";
//
// ref=Erica.phenograph@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * PhenoGraph algorithm
 *  
 *  Jacob H. Levine and et.al. Data-Driven Phenotypic Dissection of AML Reveals Progenitor-like Cells that Correlate with Prognosis. Cell, 2015.
 * 
*/
declare namespace phenograph {
   /**
    * set cluster colors of the phenograph result
    * 
    * 
     * @param g -
     * @param colorSet -
     * 
     * + default value Is ``'viridis:turbo'``.
     * @param env -
     * 
     * + default value Is ``null``.
   */
   function cluster_colors(g: object, colorSet?: any, env?: object): any;
   /**
     * @param eq default value Is ``0.85``.
   */
   function correlation_graph(x: object, y: object, eq?: number): any;
   /**
     * @param eq default value Is ``0.5``.
     * @param gt default value Is ``0``.
   */
   function graph_tree(x: object, eq?: number, gt?: number): any;
   /**
    * PhenoGraph algorithm
    * 
    * 
     * @param x -
     * @param y 
     * + default value Is ``null``.
     * @param k -
     * 
     * + default value Is ``30``.
     * @param link_cutoff -
     * 
     * + default value Is ``0``.
     * @param knn_cutoff 
     * + default value Is ``0``.
     * @param score 
     * + default value Is ``null``.
     * @param subcomponents_filter removes small subnetwork
     * 
     * + default value Is ``0``.
     * @param knn2 
     * + default value Is ``16``.
     * @param joint_cutoff 
     * + default value Is ``0``.
     * @param env 
     * + default value Is ``null``.
   */
   function phenograph(x: object, y?: object, k?: object, link_cutoff?: number, knn_cutoff?: number, score?: object, subcomponents_filter?: object, knn2?: object, joint_cutoff?: number, env?: object): object;
   /**
    * create a new score metric for KNN method in phenograph algorithm
    * 
    * 
     * @param metric 1. cosine: the cosine similarity score
     *  2. jaccard: the jaccard similarity score
     *  3. pearson: the pearson correlation score(WGCNA co-expression weight actually)
     *  4. spearman: the spearman correlation score(WGCNA spearman weight score)
     * 
     * + default value Is ``["cosine","jaccard","pearson","spearman"]``.
     * @param env -
     * 
     * + default value Is ``null``.
   */
   function score_metric(metric?: any, env?: object): object;
   /**
   */
   function setInteraction(g: object, geneIds: string, metaboliteIds: string): object;
   /**
    * 
    * 
     * @param x -
     * @param mapping the spatial spot mapping result which is evaluated from the ``correlation_graph`` function.
     * @param axis -
     * 
     * + default value Is ``["x","y"]``.
     * @param env -
     * 
     * + default value Is ``null``.
   */
   function slice_matrix(x: object, mapping: object, axis?: any, env?: object): any;
}
