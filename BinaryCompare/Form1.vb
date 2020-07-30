Imports System.IO
Imports System.Threading

Public Class Form1
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        con = True
        TextBox3.Clear()
        ' TextBox3.Text = FileVersionInfo.GetVersionInfo(TextBox1.Text).ToString

        Dim versionInfo = FileVersionInfo.GetVersionInfo(TextBox1.Text)

        TextBox3.AppendText(versionInfo.ProductName & " ")
        TextBox3.AppendText(versionInfo.ProductVersion)



        RichTextBox1.Clear()
        RichTextBox2.Clear()
        StartWriteDiff(TextBox1.Text, TextBox2.Text)
    End Sub


    Public Function StartWriteDiff(ByVal strFile1Path As String, ByVal strFile2Path As String) As Boolean
        Dim bSuccess As Boolean = True

        Dim fs1 As FileStream = Nothing
        Dim fs2 As FileStream = Nothing
        Dim fsDiff As FileStream = Nothing

        Try
            fs1 = New FileStream(strFile1Path, FileMode.Open, FileAccess.Read)
        Catch ex As Exception
            bSuccess = False

        End Try

        If bSuccess Then
            Try
                fs2 = New FileStream(strFile2Path, FileMode.Open, FileAccess.Read)
            Catch ex As Exception
                If fs1 IsNot Nothing Then
                    fs1.Close()
                End If
                bSuccess = False

            End Try
        End If

        If bSuccess Then
            Try

            Catch ex As Exception
                If fs1 IsNot Nothing Then
                    fs1.Close()
                End If
                If fs2 IsNot Nothing Then
                    fs2.Close()
                End If
                bSuccess = False

            End Try
        End If

        If bSuccess Then

            m_bStop = False

            Dim xx As New WriteDiffParams(fs1, fs2)
            WriteDiffThread(xx)

        End If

        Return bSuccess
    End Function


    Private Shared m_bStop As Boolean = False
    Private Structure WriteDiffParams
        Public Sub New(ByVal fs1 As FileStream, ByVal fs2 As FileStream)
            m_fs1 = fs1
            m_fs2 = fs2

        End Sub

        Public ReadOnly Property Fs1() As FileStream
            Get
                Return m_fs1
            End Get
        End Property

        Public ReadOnly Property Fs2() As FileStream
            Get
                Return m_fs2
            End Get
        End Property



        Private m_fs1 As FileStream
        Private m_fs2 As FileStream

    End Structure

    Private Sub WriteDiffThread(ByVal obj As Object)
        Dim p As WriteDiffParams = DirectCast(obj, WriteDiffParams)

        If p.Fs1.Length <= Int32.MaxValue Then
            Dim pFile1(p.Fs1.Length - 1) As Byte
            If p.Fs1.Read(pFile1, 0, pFile1.Length) = pFile1.Length Then
                If p.Fs2.Length <= Int32.MaxValue Then
                    Dim pFile2(p.Fs2.Length - 1) As Byte
                    If p.Fs2.Read(pFile2, 0, pFile2.Length) = pFile2.Length Then

                        Dim nPos2 As Int32 = 0
                        Dim bFoundFirstEqual As Boolean = False
                        Dim nPos1, nBytesToCompare, nStartOfEqualBlock2, i As Int32

                        nPos1 = 0
                        Do While nPos1 < pFile1.Length
                            If m_bStop Then
                                Exit Do
                            End If

                            nBytesToCompare = pFile1.Length - nPos1
                            If nBytesToCompare > 16 Then
                                nBytesToCompare = 16
                            End If

                            nStartOfEqualBlock2 = FindBlock(nPos1, nPos2, pFile1, pFile2, nBytesToCompare, bFoundFirstEqual)


                            If nStartOfEqualBlock2 >= 0 Then

                                For i = nPos2 To nStartOfEqualBlock2 - 1 Step 16
                                    WriteBlockAdded(i, pFile2, If((nStartOfEqualBlock2 - i) > 16, 16, (nStartOfEqualBlock2 - i)))
                                Next i

                                bFoundFirstEqual = True

                                nPos2 = nStartOfEqualBlock2 + nBytesToCompare
                            Else

                            End If
                            nPos1 += 16
                        Loop

                        i = nPos2
                        Do While i < pFile2.Length
                            If m_bStop Then
                                Exit Do
                            End If

                            If con = False Then
                                Exit Do
                            End If

                            WriteBlockAdded(i, pFile2, If((pFile2.Length - i) > 16, 16, (pFile2.Length - i)))
                            i += 16
                        Loop
                    End If
                End If
            End If
        End If

        p.Fs2.Close()
        p.Fs1.Close()

    End Sub

    Private Shared Function FindBlock(ByVal nPos1 As Int32, ByVal nPos2 As Int32, ByVal pFile1() As Byte, ByVal pFile2() As Byte, ByVal nBytesToCompare As Int32, ByVal bFoundFirstEqual As Boolean) As Int32
        Dim nStartOfEqualBlock2 As Int32 = -1

        Dim bFound As Boolean = False
        Dim i, j, nEqualBytes As Int32

        For i = nPos2 To pFile2.Length - 1

            nEqualBytes = 0
            j = i
            Do While j < (i + nBytesToCompare)
                If j < pFile2.Length Then
                    If pFile2(j) = pFile1(nPos1 + nEqualBytes) Then
                        nEqualBytes += 1
                        If nEqualBytes = nBytesToCompare Then
                            bFound = True
                            Exit Do
                        End If
                    Else
                        Exit Do
                    End If
                Else
                    Exit Do
                End If
                j += 1
            Loop

            If bFound Then
                nStartOfEqualBlock2 = i
                Exit For
            End If

            If bFoundFirstEqual AndAlso (i >= (nPos2 + 1024)) Then
                Exit For
            End If
        Next i

        Return nStartOfEqualBlock2
    End Function



    Public Sub WriteBlockAdded(ByVal nPos2 As Int32, ByVal pFile2() As Byte, ByVal nBytesAdded As Int32)

        If con = False Then
            Exit Sub
        End If

        RichTextBox1.AppendText(nPos2.ToString("X8") & " | ")
        RichTextBox2.AppendText("Patch(finaltarget, &H" & nPos2.ToString("X8") & ", New Byte() {")

        For i As Int32 = 0 To nBytesAdded - 1
            RichTextBox1.AppendText(pFile2(nPos2 + i).ToString("X2") & " ")
            RichTextBox2.AppendText("&h" & pFile2(nPos2 + i).ToString("X2") & ", ")
        Next i

        RichTextBox1.AppendText(vbNewLine)
        RichTextBox2.AppendText(")" & vbNewLine)

        RichTextBox2.Text = RichTextBox2.Text.Replace(", )", "})")

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        OpenFileDialog1.Filter = "All Files|*.*"
        OpenFileDialog1.FileName = ""
        If OpenFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            TextBox1.Text = OpenFileDialog1.FileName
        End If

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        OpenFileDialog1.Filter = "All Files|*.*"
        OpenFileDialog1.FileName = ""
        If OpenFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            TextBox2.Text = OpenFileDialog1.FileName
        End If
    End Sub



    Private Sub TextBox1_DragDrop(sender As Object, e As DragEventArgs) Handles TextBox1.DragDrop
        Dim MyFiles() As String
        MyFiles = e.Data.GetData(DataFormats.FileDrop)
        TextBox1.Text = MyFiles(0)
    End Sub

    Private Sub TextBox2_DragDrop(sender As Object, e As DragEventArgs) Handles TextBox2.DragDrop
        Dim MyFiles() As String
        MyFiles = e.Data.GetData(DataFormats.FileDrop)
        TextBox2.Text = MyFiles(0)
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.AllowDrop = True
        TextBox1.AllowDrop = True
        AddHandler TextBox1.DragEnter, AddressOf Main_DragEnter
        textbox1.AllowDrop = True
        AddHandler TextBox1.DragEnter, AddressOf Main_DragEnter

        TextBox2.AllowDrop = True
        AddHandler TextBox2.DragEnter, AddressOf Main_DragEnter
        TextBox2.AllowDrop = True
        AddHandler TextBox2.DragEnter, AddressOf Main_DragEnter
    End Sub

    Private Sub Main_DragEnter(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles Me.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.All
        End If
    End Sub

    Dim con As Boolean
    Private Sub RichTextBox1_TextChanged(sender As Object, e As EventArgs) Handles RichTextBox1.TextChanged
        If RichTextBox1.Lines.Length = 50 Then
            Dim result As DialogResult = MessageBox.Show("Over 800 differences found still want to continue?", "Binary Compare", MessageBoxButtons.YesNo)

            If (result = DialogResult.OK) Then
                con = True
            Else
                con = False
            End If
        End If
    End Sub
End Class
