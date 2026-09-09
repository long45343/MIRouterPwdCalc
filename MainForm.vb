Imports System
Imports System.Drawing
Imports System.Security.Cryptography
Imports System.Text
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private txtSn As TextBox
    Private txtPwd As TextBox
    Private btnCalc As Button
    Private btnCopy As Button
    Private lblStatus As Label

    Private Const R1D_SALT As String = "A2E371B0-B34B-48A5-8C40-A7133F3B5D88"
    Private Const OTHERS_SALT As String = "6d2df50a-250f-4a30-a5e6-d44fb0960aa0"

    Public Sub New()
        InitializeComponents()
    End Sub

    Private Sub InitializeComponents()
        Me.Text = "小米/红米路由器 SSH 密码计算器"
        Me.Size = New Size(430, 240)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Microsoft YaHei UI", 9.0F, FontStyle.Regular)

        Dim lblSn As New Label() With {
            .Text = "设备 SN 序列号:",
            .Location = New Point(25, 25),
            .Size = New Size(120, 20)
        }

        txtSn = New TextBox() With {
            .Location = New Point(25, 50),
            .Size = New Size(260, 25)
        }
        AddHandler txtSn.KeyDown, AddressOf TxtSn_KeyDown

        btnCalc = New Button() With {
            .Text = "计算密码",
            .Location = New Point(295, 48),
            .Size = New Size(95, 28)
        }
        AddHandler btnCalc.Click, AddressOf BtnCalc_Click

        Dim lblPwd As New Label() With {
            .Text = "SSH 初始密码 (root):",
            .Location = New Point(25, 95),
            .Size = New Size(150, 20)
        }

        txtPwd = New TextBox() With {
            .Location = New Point(25, 120),
            .Size = New Size(260, 25),
            .ReadOnly = True,
            .BackColor = SystemColors.Window
        }

        btnCopy = New Button() With {
            .Text = "复制密码",
            .Location = New Point(295, 118),
            .Size = New Size(95, 28),
            .Enabled = False
        }
        AddHandler btnCopy.Click, AddressOf BtnCopy_Click

        lblStatus = New Label() With {
            .Text = "准备就绪（请输入路由器背面的 SN）",
            .ForeColor = Color.Gray,
            .Location = New Point(25, 160),
            .Size = New Size(365, 25)
        }

        Me.Controls.Add(lblSn)
        Me.Controls.Add(txtSn)
        Me.Controls.Add(btnCalc)
        Me.Controls.Add(lblPwd)
        Me.Controls.Add(txtPwd)
        Me.Controls.Add(btnCopy)
        Me.Controls.Add(lblStatus)
    End Sub

    Private Sub TxtSn_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Calculate()
        End If
    End Sub

    Private Sub BtnCalc_Click(sender As Object, e As EventArgs)
        Calculate()
    End Sub

    Private Sub Calculate()
        Dim sn As String = txtSn.Text.Trim()
        If String.IsNullOrEmpty(sn) Then
            MessageBox.Show(Me, "请输入路由器 SN 序列号！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtSn.Focus()
            Return
        End If

        Dim salt As String
        Dim isR1D As Boolean = False
        If sn.Contains("/") Then
            salt = OTHERS_SALT
        Else
            salt = R1D_SALT
            isR1D = True
        End If

        Dim inputStr As String = sn & salt
        Dim inputBytes As Byte() = Encoding.UTF8.GetBytes(inputStr)

        Using md5 As MD5 = MD5.Create()
            Dim hashBytes As Byte() = md5.ComputeHash(inputBytes)
            Dim sb As New StringBuilder()
            For i As Integer = 0 To Math.Min(3, hashBytes.Length - 1)
                sb.Append(hashBytes(i).ToString("x2"))
            Next
            Dim pwd As String = sb.ToString()

            txtPwd.Text = pwd
            btnCopy.Enabled = True

            If isR1D Then
                lblStatus.Text = "已匹配 R1D 模式，密码已生成。"
            Else
                lblStatus.Text = "已匹配主流通用模式，密码已生成。"
            End If
            lblStatus.ForeColor = Color.Green
        End Using
    End Sub

    Private Sub BtnCopy_Click(sender As Object, e As EventArgs)
        If Not String.IsNullOrEmpty(txtPwd.Text) Then
            Clipboard.SetText(txtPwd.Text)
            lblStatus.Text = "密码已复制到剪贴板！"
            lblStatus.ForeColor = Color.Blue
        End If
    End Sub
End Class

Public Module Program
    <STAThread>
    Public Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New MainForm())
    End Sub
End Module
