Imports System.Net.Sockets
Imports System.IO
Imports Microsoft.VisualBasic.Devices
Imports System.Text
Imports System.Threading
Imports System.Xml
Imports System.Reflection
Imports System.Runtime.Remoting

Public Class Form1
    Public globTcp As TcpClient
    Public hold As Boolean = False
    Dim InitSize As Size
    Shared TrdLst As List(Of Thread)

    Private Sub Form1_Closing(sender As Object, e As EventArgs) Handles MyBase.FormClosing
        For Each trd As Thread In TrdLst
            trd.Abort()
        Next
        If globTcp IsNot Nothing Then globTcp.Close()

    End Sub
    Private Sub Listen(tcp As TcpClient)
        Dim bytes(1024) As Byte
        Dim st As NetworkStream = tcp.GetStream()
        Dim i As Int32
        Dim data As String
        ' Loop to receive all the data sent by the client.
        i = st.Read(bytes, 0, bytes.Length)
        While (i <> 0)
            ' Translate data bytes to a ASCII string.
            data = Nothing
            data = Encoding.UTF8.GetString(bytes, 0, i)
            RichTextBox1.BeginInvoke(New UpdateControl(AddressOf UpdateControlDelegate), RichTextBox1, data)
            InterpreteXml(data)
            i = st.Read(bytes, 0, bytes.Length)
        End While
    End Sub
    Private Delegate Sub UpdateControl(control As Control, txt As String)
    Private Sub UpdateControlDelegate(control As Control, txt As String)
        If TypeOf (control) Is RichTextBox Then
            Dim rt As RichTextBox = CType(control, RichTextBox)
            If Not hold Then
                If txt.Contains("<?xml") Then
                    rt.AppendText("" & vbNewLine)
                    rt.AppendText(New String("-", 100) & vbNewLine)
                    rt.AppendText("" & vbNewLine)
                    rt.AppendText(txt)
                Else
                    rt.AppendText(txt)
                End If

                rt.ScrollToCaret()
            End If
        ElseIf TypeOf (control) Is Label Then
            Dim lb As Label = CType(control, Label)
            lb.Text = txt
        End If


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim st As NetworkStream = globTcp.GetStream()
        Dim bytes As Byte() = Encoding.UTF8.GetBytes(RichTextBox2.Text)
        RichTextBox2.Clear()
        st.Write(bytes, 0, bytes.Length)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        hold = Not hold
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        For Each trd As Thread In TrdLst
            trd.Abort()
        Next
        TrdLst = New List(Of Thread)()
        If globTcp IsNot Nothing Then globTcp.Close()
        Try
            Dim tcp As New TcpClient(TextBox1.Text, CInt(TextBox2.Text)) '46000
            Debug.WriteLine("Connected")
            globTcp = tcp
            Dim trd As New Thread(AddressOf Listen)
            TrdLst.Add(trd)
            trd.Start(tcp)
        Catch ex As Exception
            Debug.WriteLine("Failed")
            MsgBox("Verbindung konnte nicht aufgebaut werden!", MsgBoxStyle.Critical, "Fehler")
        End Try
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        RichTextBox1.Clear()
    End Sub
    Dim _fullXml As String
    Private Sub InterpreteXml(xml As String)
        _fullXml &= xml
        If Not xml.Contains("</cawpacket>") Then
            Exit Sub
        End If
        Dim statusTxt As String = ""
        Using reader As XmlTextReader = New XmlTextReader(New StringReader(_fullXml))
            Dim xmlDoc As New XmlDocument
            xmlDoc.Load(reader)
            Dim root As XmlElement = xmlDoc.DocumentElement
            Dim nodes As XmlNodeList = root.GetElementsByTagName("module")
            If Not nodes Is Nothing Then
                For Each node As XmlNode In nodes
                    If statusTxt.Length = 0 Then
                        statusTxt &= node.Item("dialstatus").InnerText & "/"
                    Else
                        statusTxt &= node.Item("dialstatus").InnerText
                    End If
                Next
                status_lbl.BeginInvoke(New UpdateControl(AddressOf UpdateControlDelegate), status_lbl, statusTxt)
            End If
        End Using
        _fullXml = ""
    End Sub
    Dim initialized As Boolean = False
    Dim container1size As Size
    Dim container2size As Size
    Private Sub Form1_SizeChange(sender As Object, e As EventArgs) Handles MyBase.SizeChanged
        If initialized Then
            RichTextBox1.Size = New Size(container1size.Width + ((Size.Width - InitSize.Width) / 2), container1size.Height + Size.Height - InitSize.Height)
            RichTextBox2.Size = New Size(container2size.Width + ((Size.Width - InitSize.Width) / 2), container2size.Height + Size.Height - InitSize.Height)
            RichTextBox2.Location = New Point(RichTextBox1.Width + 19, RichTextBox2.Location.Y)
            Panel1.Location = New Point(Panel1.Location.X, RichTextBox1.Height + Panel1.Height)
            Button2.Location = New Point(RichTextBox1.Width + 150, RichTextBox2.Height + 50)
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim d As DateTime = Now
        Debug.WriteLine(d.ToShortDateString)
        TrdLst = New List(Of Thread)()
        InitSize = Size
        container1size = RichTextBox1.Size
        container2size = RichTextBox2.Size
        Text = "CAR-A-WAN Monitor (" & ProductVersion.ToString() & ")"
        initialized = True
    End Sub
End Class
