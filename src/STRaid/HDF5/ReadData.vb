Imports System.Runtime.InteropServices
Imports System.Text
Imports HDF.PInvoke

Friend Class ReadData

    Friend dims As ULong()
    Friend bytearray_elements As Integer
    Friend byte_size As Integer
    Friend dataBytes As Byte()
    Friend classID As H5T.class_t

    Public Iterator Function GetFixedLenStrings() As IEnumerable(Of String)
        Dim buf As Byte() = New Byte(byte_size - 1) {}

        For i As Integer = 0 To dataBytes.Length - 1 Step byte_size
            Call Array.ConstrainedCopy(dataBytes, i, buf, Scan0, byte_size)
            Yield Encoding.ASCII.GetString(buf)
        Next
    End Function

    Public Iterator Function GetSingles() As IEnumerable(Of Single)
        Dim buf As Byte() = New Byte(byte_size - 1) {}

        For i As Integer = 0 To dataBytes.Length - 1 Step byte_size
            Call Array.ConstrainedCopy(dataBytes, i, buf, Scan0, byte_size)
            Yield BitConverter.ToSingle(buf, Scan0)
        Next
    End Function

    Public Iterator Function GetIntegers() As IEnumerable(Of Integer)
        Dim buf As Byte() = New Byte(byte_size - 1) {}

        For i As Integer = 0 To dataBytes.Length - 1 Step byte_size
            Call Array.ConstrainedCopy(dataBytes, i, buf, Scan0, byte_size)
            Yield BitConverter.ToInt32(buf, Scan0)
        Next
    End Function

    Public Iterator Function GetLongs() As IEnumerable(Of Long)
        Dim buf As Byte() = New Byte(byte_size - 1) {}

        For i As Integer = 0 To dataBytes.Length - 1 Step byte_size
            Call Array.ConstrainedCopy(dataBytes, i, buf, Scan0, byte_size)
            Yield BitConverter.ToInt64(buf, Scan0)
        Next
    End Function

    Public Iterator Function GetDoubles() As IEnumerable(Of Double)
        Dim buf As Byte() = New Byte(byte_size - 1) {}

        For i As Integer = 0 To dataBytes.Length - 1 Step byte_size
            Call Array.ConstrainedCopy(dataBytes, i, buf, Scan0, byte_size)
            Yield BitConverter.ToDouble(buf, Scan0)
        Next
    End Function

    Friend Shared Function HasDataSet(ByVal hdf5file As Long, ByVal dsname As String) As Boolean
        Dim dsID = H5D.open(hdf5file, dsname, H5P.DEFAULT)
        Dim exists = dsID > 0

        Return exists
    End Function

    Friend Shared Function Read_dataset(ByVal hdf5file As Long, ByVal dsname As String) As ReadData
        Dim dsID = H5D.open(hdf5file, dsname, H5P.DEFAULT)

        If dsID < 0 Then
            ' missing dataset
            Return New ReadData With {
                .bytearray_elements = 0,
                .byte_size = 0,
                .dataBytes = {},
                .dims = {0}
            }
        End If

        Dim spaceID = H5D.get_space(dsID)
        Dim typeID = H5D.get_type(dsID)
        Dim classID = H5T.get_class(typeID)
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
            .dims = dims,
            .classID = classID
        }
    End Function

    Friend Shared Function Read_strings(fileId As Long, dsname As String) As String()
        Dim dataID As Long = H5D.open(fileId, dsname, H5P.DEFAULT)

        If dataID < 0 Then
            ' no such dataset
            Return {}
        End If

        Dim typeId As Long = H5D.get_type(dataID)
        Dim spaceId As Long = H5D.get_space(dataID)
        Dim count As Long = H5S.get_simple_extent_npoints(spaceId)

        Call H5S.close(spaceId)

        If Not H5T.is_variable_str(typeId) > 0 Then
            Throw New InvalidProgramException($"target data set('{dsname}') is not a variable length string!")
        End If

        Dim dest = New IntPtr(count - 1) {}
        Dim handle = GCHandle.Alloc(dest, GCHandleType.Pinned)
        H5D.read(dataID, typeId, H5S.ALL, H5S.ALL, H5P.DEFAULT, handle.AddrOfPinnedObject())

        Dim attrStrings = New List(Of String)()
        Dim i = 0

        While i < dest.Length
            Dim attrLength = 0
            While Marshal.ReadByte(dest(i), attrLength) <> 0
                Threading.Interlocked.Increment(attrLength)
            End While

            Dim buffer = New Byte(attrLength - 1) {}
            Marshal.Copy(dest(i), buffer, 0, buffer.Length)
            Dim stringPart = Encoding.UTF8.GetString(buffer)

            attrStrings.Add(stringPart)

            H5.free_memory(dest(i))
            Threading.Interlocked.Increment(i)
        End While

        handle.Free()

        Return attrStrings.ToArray
    End Function
End Class

