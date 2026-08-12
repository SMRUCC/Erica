Imports System.IO
Imports SMRUCC.Rsharp.RDataSet
Imports SMRUCC.Rsharp.RDataSet.Convertor
Imports SMRUCC.Rsharp.Runtime.Internal.Object

Module SeuratDiag

    Sub Diagnose(file As String)
        Console.WriteLine($"=== DIAGNOSE: {file} ===")
        Try
            Using stream = System.IO.File.OpenRead(file)
                Dim obj = Reader.ParseData(stream)
                Dim value = ConvertToR.ToRObject(obj.object)
                Call Dump(value, 0)
            End Using
        Catch ex As Exception
            Console.Error.WriteLine($"  [ERROR] {ex.GetType().Name}: {ex.Message}")
            Console.Error.WriteLine($"  [STACK] {ex.StackTrace}")
        End Try
    End Sub

    Sub Dump(value As Object, depth As Integer)
        Dim indent As String = New String(" "c, depth * 2)

        Try
            If TypeOf value Is list Then
                Dim l As list = DirectCast(value, list)
                Dim cls As String = ""
                Dim classObj As Object = l.getByName(".class")
                If classObj IsNot Nothing Then
                    cls = classObj.ToString()
                End If
                Console.WriteLine($"{indent}list[.class={cls}] length={l.length}")

                ' getNames may return Nothing for anonymous lists
                Dim names As String() = l.getNames
                If names Is Nothing OrElse names.Length = 0 Then
                    Console.WriteLine($"{indent}  (no named slots)")
                Else
                    For Each name In names
                        If name Is Nothing OrElse name = ".class" Then Continue For
                        Dim slotVal As Object = l.getByName(name)
                        Console.WriteLine($"{indent}  ${name} =>")
                        Call Dump(slotVal, depth + 2)
                    Next
                End If
            ElseIf TypeOf value Is dataframe Then
                Dim df As dataframe = DirectCast(value, dataframe)
                Dim colNameStr As String = "N/A"
                Try
                    colNameStr = String.Join(",", df.colnames)
                Catch
                    colNameStr = "(error reading colnames)"
                End Try
                Console.WriteLine($"{indent}dataframe rows={df.nrows} cols={df.ncols} colnames={colNameStr}")
            ElseIf TypeOf value Is vector Then
                Dim v As vector = DirectCast(value, vector)
                Console.WriteLine($"{indent}vector length={v.length} dataType={v.GetType.Name}")
            ElseIf value Is Nothing Then
                Console.WriteLine($"{indent}NULL")
            ElseIf TypeOf value Is Array Then
                Dim a As Array = DirectCast(value, Array)
                Dim elemType As String = "?"
                Try
                    elemType = If(a.GetType.GetElementType()?.Name, "Object")
                Catch
                End Try
                Console.WriteLine($"{indent}array length={a.Length} elementType={elemType}")
            Else
                Dim str As String = "null"
                Try
                    str = value.ToString()
                Catch
                End Try
                Console.WriteLine($"{indent}{value.GetType().Name} = {str}")
            End If
        Catch ex As Exception
            Console.Error.WriteLine($"{indent}[DUMP_ERROR] {ex.GetType().Name}: {ex.Message}")
        End Try
    End Sub

End Module
