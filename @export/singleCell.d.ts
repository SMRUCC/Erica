// export R# package module type define for javascript/typescript language
//
// ref=Erica.singleCells@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * single cell data toolkit
 * 
*/
declare namespace singleCell {
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
   module read {
      /**
       * read h5ad object from a specific hdf5 file
       * 
       * 
        * @param h5adfile -
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
   */
   function pca_annotation(h5ad: object): object;
   /**
   */
   function umap_annotation(h5ad: object): object;
   /**
     * @param q default value Is ``0.2``.
   */
   function expression_list(raw: object, q?: number): object;
}
