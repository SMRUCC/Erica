Imports System.IO
Imports SMRUCC.Rsharp.RDataSet
Imports SMRUCC.Rsharp.RDataSet.Convertor
Imports SMRUCC.Rsharp.Runtime.Internal.Object

Module SeuratDiag

    Sub Diagnose(file As String)
        Console.WriteLine($"=== DIAGNOSE: {file} ===")
        Using stream = System.IO.File.OpenRead(file)
            Dim obj = Reader.ParseData(stream)
            Dim value = ConvertToR.ToRObject(obj.object)
            Call Dump(value, 0)
        End Using
    End Sub

    Sub Dump(value As Object, depth As Integer)
        Dim indent As String = New String(" "c, depth * 2)
        If TypeOf value Is list Then
            Dim l As list = value
            Dim cls = If(l.getByName(".class"), "")
            Console.WriteLine($"{indent}list[.class={cls}] length={l.length}")
            For Each name In l.getNames
                If name = ".class" Then Continue For
                Console.WriteLine($"{indent}  ${name} =>")
                Call Dump(l(name), depth + 2)
            Next
        ElseIf TypeOf value Is dataframe Then
            Dim df As dataframe = value
            Console.WriteLine($"{indent}dataframe rows={df.nrows} cols={df.ncols} colnames={df.colnames.JoinBy(",")}")
        ElseIf TypeOf value Is vector Then
            Dim v As vector = value
            Console.WriteLine($"{indent}vector length={v.length} dataType={v.GetType.Name}")
        ElseIf value Is Nothing Then
            Console.WriteLine($"{indent}NULL")
        ElseIf TypeOf value Is Array Then
            Dim a As Array = value
            Console.WriteLine($"{indent}array length={a.Length} elementType={a.GetType.GetElementType.Name}")
        Else
            Console.WriteLine($"{indent}{value.GetType.Name} = {value.ToString}")
        End If
    End Sub

End Module
