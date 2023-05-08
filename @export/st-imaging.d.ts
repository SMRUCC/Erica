// export R# package module type define for javascript/typescript language
//
// ref=Erica.stImaging@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * do heatmap imaging of the STdata spots
 * 
*/
declare namespace st-imaging {
   /**
    * 
    * 
   */
   function new_render(h5ad:object): object;
   module as {
      /**
       * 
       * 
        * @param raw the sample id should be the barcodes, and the row id is the gene id
        * @param spots -
      */
      function STrender(raw:object, spots:object): object;
   }
   /**
    * get gene layer raw data
    * 
    * 
     * @param imaging -
     * @param geneId -
   */
   function gene_layer(imaging:object, geneId:string): object;
   /**
    * do imaging render of a specific gene expression layer
    * 
    * 
     * @param render -
     * @param geneId -
   */
   function imaging(render:object, geneId:string): any;
   /**
     * @param size default value Is ``'3000,3000'``.
     * @param colorMaps default value Is ``null``.
     * @param env default value Is ``null``.
   */
   function spot_heatmap(spots:object, size?:any, colorMaps?:object, env?:object): any;
   /**
     * @param size default value Is ``'3000,3000'``.
     * @param spot_radius default value Is ``13``.
     * @param colorMaps default value Is ``null``.
     * @param env default value Is ``null``.
   */
   function plot_spots(spots:object, matrix:any, geneId:string, size?:any, spot_radius?:number, colorMaps?:object, env?:object): any;
}
