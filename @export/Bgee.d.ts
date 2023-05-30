// export R# package module type define for javascript/typescript language
//
//    imports "Bgee" from "Erica";
//
// ref=Erica.Bgee@Erica, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/**
 * the bgee database toolkit
 * 
*/
declare namespace Bgee {
   /**
   */
   function anatomicalIDs(bgee: object): object;
   /**
    * 
    * 
     * @param bgee -
     * @param geneSet -
     * @param development_stage this function returns the enrichment result for all existed development stages
     * 
     * + default value Is ``'*'``.
     * @return the return data value is depends on the parameter 
     *  value of **`development_stage`**.
     *  
     *  + for a specific development_stage id, this function 
     *    just returns a enrichment result dataframe
     *  + for default value, a list that contains enrichment 
     *    result for each development_stage will be returned
   */
   function bgee_calls(bgee: object, geneSet: string, development_stage?: string): object;
   /**
   */
   function development_stage(bgee: object): object;
   /**
   */
   function geneIDs(bgee: object): object;
   /**
     * @param env default value Is ``null``.
   */
   function metabolomicsMapping(uniprot: object, geneExpressions: object, env?: object): object;
   /**
     * @param advance default value Is ``false``.
     * @param quality default value Is ``'*'``.
     * @param pip_stream default value Is ``false``.
     * @param env default value Is ``null``.
   */
   function parseTsv(file: string, advance?: boolean, quality?: string, pip_stream?: boolean, env?: object): object;
   module read {
      /**
      */
      function backgroundPack(file: string): object;
   }
   /**
    * create tissue and cell background based on bgee database
    * 
    * 
     * @param bgee -
     * @param env 
     * + default value Is ``null``.
   */
   function tissue_background(bgee: object, env?: object): object;
   module write {
      /**
        * @param env default value Is ``null``.
      */
      function backgroundPack(background: any, file: string, env?: object): boolean;
   }
}
