// export R# package module type define for javascript/typescript language
//
//    imports "singleCell" from "Erica";
//
// ref=Erica.singleCells@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * single cell data toolkit
 * 
*/
declare namespace singleCell {
   /**
    * Extract the gene id set with non-missing expression status under 
    *  the given threshold value **`q`**.
    * 
    * 
     * @param raw -
     * @param q the percentage threshold value for assign a gene a missing feature
     * 
     * + default value Is ``0.2``.
   */
   function expression_list(raw: object, q?: number): object;
   /**
    * extract the raw expression data matrix from the h5ad object
    * 
    * 
     * @param h5ad -
     * @param env -
     * 
     * + default value Is ``null``.
   */
   function HTS_matrix(h5ad: any, env?: object): object;
   /**
    * Extract the PCA matrix from h5ad
    * 
    * 
     * @param h5ad -
   */
   function pca_annotation(h5ad: object): object;
   module read {
      /**
       * read h5ad object from a specific hdf5 file
       * 
       * > this function only works on Windows platform.
       * 
        * @param h5adfile The file path to the h5ad rawdata file
        * @param loadExpr0 -
        * 
        * + default value Is ``true``.
      */
      function h5ad(h5adfile: string, loadExpr0?: boolean): object;
   }
   /**
    * export the spatial maps data
    * 
    * 
     * @param h5ad -
     * @param useCellAnnotation -
     * 
     * + default value Is ``null``.
   */
   function spatialMap(h5ad: object, useCellAnnotation?: boolean): object;
   /**
    * Extract the UMAP matrix from h5ad
    * 
    * 
     * @param h5ad -
   */
   function umap_annotation(h5ad: object): object;
}
