''' <summary>
''' 细胞匹配结果类
''' </summary>
Public Class CellMatchResult

    Public Property CellA As CellScan
    Public Property CellB As CellScan
    Public Property Distance As Double
    Public Property Morphology As Double

    ''' <summary>
    ''' the total match score
    ''' </summary>
    ''' <returns></returns>
    Public Property MatchScore As Double

    Public Overrides Function ToString() As String
        Return $"Match: A({CellA.physical_x:F2}, {CellA.physical_y:F2}) <-> B({CellB.physical_x:F2}, {CellB.physical_y:F2}) | Dist: {Distance:F2} | Score: {MatchScore:F4}"
    End Function
End Class