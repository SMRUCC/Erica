// export R# package module type define for javascript/typescript language
//
// ref=Erica.STdata@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * spatial transcriptomics data toolkit
 * 
*/
declare namespace STdata {
   module as {
      /**
      */
      function STmatrix(h5Matrix:object, spots:object): object;
      /**
      */
      function STRaid(h5Matrix:object, spots:object): object;
   }
   module read {
      /**
       * load the spatial mapping data of the spot barcode 
       *  associated with the spot spaital information
       * 
       * 
        * @param file -
      */
      function spatial_spots(file:string): object;
      /**
       * load the raw expression matrix which is associated
       *  with the barcode
       * 
       * > the expressin data just associated with the barcode, 
       * >  no spot spatial information.
       * 
        * @param h5ad -
        * @return the result matrix object in format of sample id of the 
        *  result matrix is the gene id and the row id in matrix is 
        *  actually the spot xy data tag or the barcoede data
      */
      function ST_spacerangerH5Matrix(h5ad:string): object;
   }
   /**
    * 
    * 
     * @param matrix should be a transposed matrix of the output from ``as.STmatrix``.
     * @param nsamples -
     * 
     * + default value Is ``32``.
   */
   function sampling(matrix:object, nsamples?:object): object;
   module write {
      /**
      */
      function straid(straid:object, file:string): any;
   }
}
