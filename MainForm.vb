Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Text
Imports System.Windows.Forms

<Assembly: AssemblyTitle("小米/红米路由器 SSH 密码计算器")>
<Assembly: AssemblyProduct("Xiaomi Router SSH Calc")>
<Assembly: AssemblyVersion("0.0.0.0")>
<Assembly: AssemblyFileVersion("0.0.0.0")>
<Assembly: AssemblyInformationalVersion("0.0.0.0")>

Public Class MainForm
    Inherits Form

    ' 算法 Salt 常量与 SSH 配置
    Private Const R1D_SALT As String = "A2E371B0-B34B-48A5-8C40-A7133F3B5D88"
    Private Const OTHERS_SALT As String = "6d2df50a-250f-4a30-a5e6-d44fb0960aa0"
    Private Const DEFAULT_SSH_IP As String = "192.168.31.1"
    ' 兼容新版 OpenSSH (8.8+) 连接小米/红米路由器时所需的老旧 hostkey、公钥和密钥交换算法
    Private Const LEGACY_SSH_OPTIONS As String = "-o HostKeyAlgorithms=+ssh-rsa -o PubkeyAcceptedKeyTypes=+ssh-rsa -o PubkeyAcceptedAlgorithms=+ssh-rsa -o KexAlgorithms=+diffie-hellman-group14-sha1,diffie-hellman-group1-sha1"

    ' UI 控件定义
    Private picLogo As PictureBox
    Private lblHeaderTitle As Label
    Private lblHeaderSubtitle As Label
    Private lblVersionBadge As Label

    Private grpInput As GroupBox
    Private lblSnTag As Label
    Private txtSn As TextBox
    Private btnCalc As Button
    Private btnClear As Button
    Private lblSnHint As Label

    Private grpStatus As GroupBox
    Private lblStatusDot As Label
    Private lblStatusText As Label
    Private lblModeDetail As Label
    Private lblPwdTag As Label
    Private txtPwd As TextBox
    Private btnCopy As Button
    Private btnCopyCmd As Button

    Private lblLogTag As Label
    Private lnkClearLog As LinkLabel
    Private txtLog As TextBox

    Public Sub New(Optional initialSn As String = Nothing)
        InitializeComponents()
        Log("软件启动就绪，支持小米 / 红米全系列路由器 SSH 密码计算。")
        Log(String.Format("算法策略: 含 '/' 使用通用 Salt ({0}...)，无 '/' 使用 R1D 专属 Salt。", OTHERS_SALT.Substring(0, 8)))

        If Not String.IsNullOrEmpty(initialSn) Then
            txtSn.Text = initialSn.Trim()
            CalculatePassword()
        End If
    End Sub

    Private Sub InitializeComponents()
        Me.Text = "小米/红米路由器 SSH 密码计算器"
        Me.Size = New Size(600, 565)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.Font = New Font("Microsoft YaHei UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point)
        Me.BackColor = Color.FromArgb(248, 249, 250)

        ' 提取关联图标
        Try
            Dim appIcon As Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            If appIcon IsNot Nothing Then Me.Icon = appIcon
        Catch
        End Try

        ' ==================== 顶部 Header Banner ====================
        picLogo = New PictureBox() With {
            .Location = New Point(20, 16),
            .Size = New Size(46, 46),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.Transparent
        }
        LoadLogoImage()
        Me.Controls.Add(picLogo)

        lblHeaderTitle = New Label() With {
            .Text = "小米 / 红米路由器 SSH 密码计算器",
            .Font = New Font("Microsoft YaHei UI", 13.5F, FontStyle.Bold, GraphicsUnit.Point),
            .ForeColor = Color.FromArgb(33, 37, 41),
            .Location = New Point(74, 15),
            .AutoSize = True
        }
        Me.Controls.Add(lblHeaderTitle)

        lblHeaderSubtitle = New Label() With {
            .Text = "Mi / Redmi Router SSH Root Password Calculator",
            .Font = New Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
            .ForeColor = Color.FromArgb(108, 117, 125),
            .Location = New Point(76, 43),
            .AutoSize = True
        }
        Me.Controls.Add(lblHeaderSubtitle)

        lblVersionBadge = New Label() With {
            .Text = "v0.0.0.0",
            .Font = New Font("Microsoft YaHei UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(108, 117, 125),
            .BackColor = Color.FromArgb(233, 236, 239),
            .Location = New Point(494, 18),
            .Size = New Size(70, 22),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblVersionBadge)

        ' ==================== 输入分组框 ====================
        grpInput = New GroupBox() With {
            .Text = " 设备序列号 (SN) ",
            .Location = New Point(20, 72),
            .Size = New Size(544, 96)
        }
        Me.Controls.Add(grpInput)

        lblSnTag = New Label() With {
            .Text = "路由器 SN 序列号:",
            .ForeColor = Color.FromArgb(73, 80, 87),
            .Location = New Point(20, 24),
            .AutoSize = True
        }
        grpInput.Controls.Add(lblSnTag)

        txtSn = New TextBox() With {
            .Location = New Point(20, 48),
            .Size = New Size(335, 27),
            .Font = New Font("Microsoft YaHei UI", 10.0F, FontStyle.Regular),
            .BackColor = Color.White
        }
        AddHandler txtSn.KeyDown, AddressOf TxtSn_KeyDown
        grpInput.Controls.Add(txtSn)

        btnCalc = New Button() With {
            .Text = "计算密码",
            .Location = New Point(365, 46),
            .Size = New Size(96, 31),
            .BackColor = Color.FromArgb(13, 110, 253),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        btnCalc.FlatAppearance.BorderSize = 0
        AddHandler btnCalc.Click, AddressOf BtnCalc_Click
        grpInput.Controls.Add(btnCalc)

        btnClear = New Button() With {
            .Text = "清空",
            .Location = New Point(468, 46),
            .Size = New Size(58, 31),
            .BackColor = Color.FromArgb(233, 236, 239),
            .ForeColor = Color.FromArgb(33, 37, 41),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Microsoft YaHei UI", 9.0F, FontStyle.Regular),
            .Cursor = Cursors.Hand
        }
        btnClear.FlatAppearance.BorderSize = 0
        AddHandler btnClear.Click, AddressOf BtnClear_Click
        grpInput.Controls.Add(btnClear)

        lblSnHint = New Label() With {
            .Text = "提示: SN 位于路由器背面标签条形码下方；若含斜杠 '/' 则自动采用通用模式，否则为 R1D 专用模式。",
            .ForeColor = Color.FromArgb(108, 117, 125),
            .Font = New Font("Microsoft YaHei UI", 8.0F, FontStyle.Regular),
            .Location = New Point(20, 78),
            .AutoSize = True
        }
        grpInput.Controls.Add(lblSnHint)

        ' ==================== 状态与结果分组框 ====================
        grpStatus = New GroupBox() With {
            .Text = " 计算结果与当前状态 ",
            .Location = New Point(20, 178),
            .Size = New Size(544, 156)
        }
        Me.Controls.Add(grpStatus)

        lblStatusDot = New Label() With {
            .Text = "●",
            .Font = New Font("Microsoft YaHei UI", 16.0F, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .Location = New Point(20, 22),
            .Size = New Size(26, 28)
        }
        grpStatus.Controls.Add(lblStatusDot)

        lblStatusText = New Label() With {
            .Text = "准备就绪（等待输入路由器 SN 序列号）",
            .Font = New Font("Microsoft YaHei UI", 10.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(33, 37, 41),
            .Location = New Point(48, 26),
            .AutoSize = True
        }
        grpStatus.Controls.Add(lblStatusText)

        lblModeDetail = New Label() With {
            .Text = "匹配模式: 尚未计算 | 算法标准: MD5(SN + Salt) 前 8 位十六进制",
            .Font = New Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(108, 117, 125),
            .Location = New Point(49, 52),
            .AutoSize = True
        }
        grpStatus.Controls.Add(lblModeDetail)

        lblPwdTag = New Label() With {
            .Text = "SSH 初始密码 (root 账户):",
            .ForeColor = Color.FromArgb(73, 80, 87),
            .Location = New Point(20, 82),
            .AutoSize = True
        }
        grpStatus.Controls.Add(lblPwdTag)

        txtPwd = New TextBox() With {
            .Location = New Point(20, 105),
            .Size = New Size(245, 29),
            .Font = New Font("Consolas", 13.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(33, 37, 41),
            .BackColor = Color.White,
            .ReadOnly = True,
            .Text = "--------"
        }
        grpStatus.Controls.Add(txtPwd)

        btnCopy = New Button() With {
            .Text = "复制密码",
            .Location = New Point(275, 103),
            .Size = New Size(116, 34),
            .BackColor = Color.FromArgb(25, 135, 84),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            .Cursor = Cursors.Hand,
            .Enabled = False
        }
        btnCopy.FlatAppearance.BorderSize = 0
        AddHandler btnCopy.Click, AddressOf BtnCopy_Click
        grpStatus.Controls.Add(btnCopy)

        btnCopyCmd = New Button() With {
            .Text = "复制 SSH 命令",
            .Location = New Point(400, 103),
            .Size = New Size(126, 34),
            .BackColor = Color.FromArgb(233, 236, 239),
            .ForeColor = Color.FromArgb(33, 37, 41),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Microsoft YaHei UI", 9.0F, FontStyle.Regular),
            .Cursor = Cursors.Hand,
            .Enabled = False
        }
        btnCopyCmd.FlatAppearance.BorderSize = 0
        AddHandler btnCopyCmd.Click, AddressOf BtnCopyCmd_Click
        grpStatus.Controls.Add(btnCopyCmd)

        ' ==================== 日志区域 ====================
        lblLogTag = New Label() With {
            .Text = "运行日志:",
            .ForeColor = Color.FromArgb(73, 80, 87),
            .Location = New Point(22, 346),
            .AutoSize = True
        }
        Me.Controls.Add(lblLogTag)

        lnkClearLog = New LinkLabel() With {
            .Text = "清空日志",
            .LinkColor = Color.FromArgb(108, 117, 125),
            .ActiveLinkColor = Color.FromArgb(13, 110, 253),
            .Location = New Point(510, 346),
            .AutoSize = True,
            .Cursor = Cursors.Hand
        }
        AddHandler lnkClearLog.LinkClicked, Sub(s, e)
                                                txtLog.Clear()
                                                Log("日志已清空。")
                                            End Sub
        Me.Controls.Add(lnkClearLog)

        txtLog = New TextBox() With {
            .Location = New Point(20, 368),
            .Size = New Size(544, 142),
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.White,
            .Font = New Font("Consolas", 9.0F, FontStyle.Regular)
        }
        Me.Controls.Add(txtLog)
    End Sub

    Private Sub LoadLogoImage()
        Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
        Dim candidateFiles As String() = {
            Path.Combine(baseDir, "Resources\app_icon.png"),
            Path.Combine(baseDir, "app_icon.png"),
            Path.Combine(baseDir, "Resources\app.ico"),
            Path.Combine(baseDir, "app.ico")
        }

        For Each file In candidateFiles
            If IO.File.Exists(file) Then
                Try
                    If file.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) Then
                        Using ico As New Icon(file, New Size(64, 64))
                            picLogo.Image = ico.ToBitmap()
                            Return
                        End Using
                    Else
                        Using img As Image = Image.FromFile(file)
                            picLogo.Image = New Bitmap(img)
                            Return
                        End Using
                    End If
                Catch
                End Try
            End If
        Next

        picLogo.Image = GenerateFallbackLogo(48)
    End Sub

    Private Function GenerateFallbackLogo(size As Integer) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.HighQuality

            Using path As GraphicsPath = GetRoundedRectanglePath(New Rectangle(2, 2, size - 4, size - 4), 10)
                Using brush As New LinearGradientBrush(
                    New Point(0, 0), New Point(size, size),
                    Color.FromArgb(13, 110, 253), Color.FromArgb(10, 88, 202))
                    g.FillPath(brush, path)
                End Using
            End Using

            Using pen As New Pen(Color.White, 2.5F)
                pen.StartCap = LineCap.Round
                pen.EndCap = LineCap.Round

                Dim mid As Integer = size \ 2
                Dim pad As Integer = size \ 5

                ' 左上 减号
                g.DrawLine(pen, pad, mid - pad \ 2, mid - pad \ 2, mid - pad \ 2)

                ' 右上 乘号
                g.DrawLine(pen, mid + pad \ 2, pad, size - pad, mid - pad \ 2)
                g.DrawLine(pen, size - pad, pad, mid + pad \ 2, mid - pad \ 2)

                ' 左下 加号
                Dim cx As Integer = (pad + mid - pad \ 2) \ 2
                Dim cy As Integer = mid + pad \ 2
                g.DrawLine(pen, cx - pad \ 2, cy, cx + pad \ 2, cy)
                g.DrawLine(pen, cx, cy - pad \ 2, cx, cy + pad \ 2)

                ' 右下 等号
                Dim y1 As Integer = mid + pad \ 3
                Dim y2 As Integer = mid + pad
                g.DrawLine(pen, mid + pad \ 2, y1, size - pad, y1)
                g.DrawLine(pen, mid + pad \ 2, y2, size - pad, y2)
            End Using
        End Using
        Return bmp
    End Function

    Private Shared Function GetRoundedRectanglePath(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim diameter As Integer = radius * 2
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90)
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90)
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90)
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Private Sub Log(msg As String)
        Dim time As String = DateTime.Now.ToString("HH:mm:ss")
        txtLog.AppendText(String.Format("[{0}] {1}" & vbCrLf, time, msg))
    End Sub

    Private Sub TxtSn_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            CalculatePassword()
        End If
    End Sub

    Private Sub BtnCalc_Click(sender As Object, e As EventArgs)
        CalculatePassword()
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As EventArgs)
        txtSn.Clear()
        txtPwd.Text = "--------"
        btnCopy.Enabled = False
        btnCopy.Text = "复制密码"
        btnCopy.BackColor = Color.FromArgb(25, 135, 84)
        btnCopyCmd.Enabled = False
        btnCopyCmd.Text = "复制 SSH 命令"

        lblStatusDot.ForeColor = Color.Gray
        lblStatusText.Text = "已清空输入，等待重新输入 SN"
        lblModeDetail.Text = "匹配模式: 尚未计算 | 算法标准: MD5(SN + Salt) 前 8 位十六进制"
        txtSn.Focus()
        Log("已清空输入框和计算结果。")
    End Sub

    Private Sub CalculatePassword()
        Dim sn As String = txtSn.Text.Trim()
        If String.IsNullOrEmpty(sn) Then
            lblStatusDot.ForeColor = Color.FromArgb(220, 53, 69)
            lblStatusText.Text = "请输入路由器 SN 序列号！"
            lblModeDetail.Text = "提示：请查看路由器背部贴纸标签"
            MessageBox.Show(Me, "请输入路由器 SN 序列号！" & vbCrLf & "例如：12345/XXXXXXXX 或 纯数字/字母组合",
                            "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtSn.Focus()
            Return
        End If

        Try
            Dim salt As String
            Dim isR1D As Boolean = False
            If sn.Contains("/") Then
                salt = OTHERS_SALT
                isR1D = False
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
                btnCopy.Text = "复制密码"
                btnCopy.BackColor = Color.FromArgb(25, 135, 84)
                btnCopyCmd.Enabled = True
                btnCopyCmd.Text = "复制 SSH 命令"

                lblStatusDot.ForeColor = Color.FromArgb(25, 135, 84)

                If isR1D Then
                    lblStatusText.Text = "计算成功：已识别为初代 R1D 专用型号"
                    lblModeDetail.Text = String.Format("Salt: {0} | 算法: MD5(SN + Salt)[:8]", R1D_SALT)
                    Log(String.Format("输入 SN: {0} -> 匹配初代 R1D 模式 -> 密码: {1}", sn, pwd))
                Else
                    lblStatusText.Text = "计算成功：已识别为主流通用型号（含 '/' 规则）"
                    lblModeDetail.Text = String.Format("Salt: {0} | 算法: MD5(SN + Salt)[:8]", OTHERS_SALT)
                    Log(String.Format("输入 SN: {0} -> 匹配主流通用模式 -> 密码: {1}", sn, pwd))
                End If
            End Using
        Catch ex As Exception
            lblStatusDot.ForeColor = Color.FromArgb(220, 53, 69)
            lblStatusText.Text = "计算出错: " & ex.Message
            Log("计算异常: " & ex.ToString())
        End Try
    End Sub

    Private Sub BtnCopy_Click(sender As Object, e As EventArgs)
        If Not String.IsNullOrEmpty(txtPwd.Text) AndAlso txtPwd.Text <> "--------" Then
            Clipboard.SetText(txtPwd.Text)
            btnCopy.Text = "✓ 密码已复制"
            lblStatusText.Text = String.Format("密码 {0} 已成功复制到系统剪贴板！", txtPwd.Text)
            Log(String.Format("密码 [{0}] 已复制到剪贴板，可在 SSH 终端中直接粘贴。", txtPwd.Text))
        End If
    End Sub

    Private Sub BtnCopyCmd_Click(sender As Object, e As EventArgs)
        If Not String.IsNullOrEmpty(txtPwd.Text) AndAlso txtPwd.Text <> "--------" Then
            Dim cmd As String = String.Format("ssh {0} root@{1}", LEGACY_SSH_OPTIONS, DEFAULT_SSH_IP)
            Clipboard.SetText(cmd)
            btnCopyCmd.Text = "✓ 命令已复制"
            lblStatusText.Text = "SSH 连接命令（已启用 ssh-rsa 等老旧加密算法兼容）已复制！"
            Log(String.Format("已复制常用 SSH 命令（含老旧算法兼容）: '{0}'", cmd))
        End If
    End Sub
End Class

Public Module Program
    <STAThread>
    Public Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Dim cmdArgs As String() = Environment.GetCommandLineArgs()
        Dim initialSn As String = If(cmdArgs IsNot Nothing AndAlso cmdArgs.Length > 1, cmdArgs(1), Nothing)
        Application.Run(New MainForm(initialSn))
    End Sub
End Module
