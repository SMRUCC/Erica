Imports System.Runtime.InteropServices
Imports HDF.PInvoke
Imports HDF.PInvoke.H5O.hdr_info_t

Public Class ReadData

    Dim dims As ULong()
    Dim bytearray_elements As Integer
    Dim byte_size As Integer
    Dim dataBytes As Byte()

    Public Iterator Function GetSingles() As IEnumerable(Of Single)
        Dim buf As Byte() = New Byte(byte_size - 1) {}

        For i As Integer = 0 To dataBytes.Length - 1 Step byte_size
            Call Array.ConstrainedCopy(dataBytes, i, buf, Scan0, byte_size)
            Yield BitConverter.ToSingle(buf, Scan0)
        Next
    End Function

    Public Shared Function Load(path As String)
        Dim fileId = H5F.open(path, H5F.ACC_RDONLY)
        Dim d1 = Read_dataset(fileId, "/X/data").GetSingles.ToArray

        Pause()
    End Function

    Private Shared Function Read_dataset(ByVal hdf5file As Long, ByVal dsname As String) As ReadData
        Dim dsID = H5D.open(hdf5file, dsname, H5P.DEFAULT)
        Dim spaceID = H5D.get_space(dsID)
        Dim typeID = H5D.get_type(dsID)
        Dim rank = H5S.get_simple_extent_ndims(spaceID)
        Dim dims(rank - 1) As ULong
        Dim maxDims(rank - 1) As ULong
        H5S.get_simple_extent_dims(spaceID, dims, maxDims)
        Dim sizeData = H5T.get_size(typeID)
        Dim size = sizeData.ToInt32()
        Dim bytearray_elements = 1
        For i = 0 To dims.Length - 1
            bytearray_elements *= dims(i)
        Next
        Dim dataBytes As Byte() = New Byte(bytearray_elements * CULng(size) - 1) {}

        Dim pinnedArray As GCHandle = GCHandle.Alloc(dataBytes, GCHandleType.Pinned)

        H5D.read(dsID, typeID, H5S.ALL, H5S.ALL, H5P.DEFAULT, pinnedArray.AddrOfPinnedObject())
        pinnedArray.Free()

        Return New ReadData With {
            .bytearray_elements = bytearray_elements,
            .byte_size = size,
            .dataBytes = dataBytes,
            .dims = dims
        }
    End Function
End Class

