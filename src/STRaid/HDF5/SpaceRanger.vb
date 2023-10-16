Imports HDF.PInvoke
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

Namespace HDF5

    ''' <summary>
    ''' Space ranger file format reader
    ''' </summary>
    Public Module SpaceRanger

        ''' <summary>
        ''' load the raw expression matrix which is associated with the barcode
        ''' </summary>
        ''' <param name="h5ad"></param>
        ''' <returns>
        ''' the result matrix object in format of sample id of the 
        ''' result matrix is the gene id and the row id in matrix is 
        ''' actually the spot xy data tag or the barcoede data
        ''' </returns>
        Public Function ReadST_spacerangerH5Matrix(h5ad As String) As Matrix
            Dim fileId As Long = H5F.open(h5ad, H5F.ACC_RDONLY)
            Dim shape As Integer() = ReadData.Read_dataset(fileId, "/matrix/shape").GetIntegers.ToArray
            Dim matrix As X = loadX(Of Integer)(fileId, "/matrix", shape(0))
            Dim barcodes As String() = ReadData.Read_dataset(fileId, "/matrix/barcodes").GetFixedLenStrings.ToArray
            Dim geneID As String() = ReadData.Read_dataset(fileId, "/matrix/features/id").GetFixedLenStrings.ToArray
            Dim pull As Matrix = matrix.ExportExpression(barcodes, geneID, source:=h5ad.BaseName)

            Return pull
        End Function
    End Module
End Namespace