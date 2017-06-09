﻿Imports System.Threading
Imports System.IO

Public Class frmPS4TwitchHelper

    Private Declare Function GetWindowRect Lib "user32" Alias "GetWindowRect" (ByVal hwnd As Integer, Byref lpRect As RECT) As Integer
    Private Declare Function MoveWindow Lib "user32" Alias "MoveWindow" (ByVal hWnd As Integer, ByVal X As Integer, ByVal Y As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer, ByVal repait As Boolean) as  Boolean
    Private Declare Function GetPixel Lib "gdi32.dll" Alias "GetPixel" (ByVal hdc As IntPtr, ByVal nXPos As Integer, ByVal nYPos As Integer) As UInteger



    Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    Private lastuser As String = ""
    Private lastcmd As string = ""



    Private _whiteFont As New Font("Courier New", 16)
    Private _blackFont As New Font("Courier New", 16)

    Private WithEvents updtext As New System.Windows.Forms.Timer()
    Private WithEvents updTimer As New System.Windows.Forms.Timer()
    Private WithEvents refTimerPost As New System.Windows.Forms.Timer()
    
    
        Const MOUSEEVENTF_LEFTDOWN As Integer = 2
        Const MOUSEEVENTF_LEFTUP As Integer = 4

    Private Declare Function OpenProcess Lib "kernel32.dll" (ByVal dwDesiredAcess As UInt32, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Int32) As IntPtr
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByVal lpNumberOfBytesWritten As Integer) As Boolean
    Private Declare Function CloseHandle Lib "kernel32.dll" (ByVal hObject As IntPtr) As Boolean
    Private Declare Function VirtualAllocEx Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As IntPtr, ByVal flAllocationType As Integer, ByVal flProtect As Integer) As IntPtr
    Private Declare Function VirtualProtectEx Lib "kernel32.dll" (hProcess As IntPtr, lpAddress As IntPtr, ByVal lpSize As IntPtr, ByVal dwNewProtect As UInt32, ByRef dwOldProtect As UInt32) As Boolean
    Private Declare Function VirtualFreeEx Lib "kernel32.dll" (hProcess As IntPtr, lpAddress As IntPtr, ByVal dwSize As Integer, ByVal dwFreeType As Integer) As Boolean
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As Integer, ByVal lpThreadAttributes As Integer, ByVal dwStackSize As Integer, ByVal lpStartAddress As Integer, ByVal lpParameter As Integer, ByVal dwCreationFlags As Integer, ByRef lpThreadId As Integer) As Integer
    Private Declare Sub mouse_event Lib "user32.dll" (ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer, ByVal dwExtraInfo As IntPtr)

    Public Const PROCESS_VM_READ = &H10
    Public Const TH32CS_SNAPPROCESS = &H2
    Public Const MEM_COMMIT = 4096
    Public Const MEM_RELEASE = &H8000
    Public Const PAGE_READWRITE = 4
    Public Const PAGE_EXECUTE_READWRITE = &H40
    Public Const PROCESS_CREATE_THREAD = (&H2)
    Public Const PROCESS_VM_OPERATION = (&H8)
    Public Const PROCESS_VM_WRITE = (&H20)
    Public Const PROCESS_ALL_ACCESS = &H1F0FFF

    Private _targetProcess As Process = Nothing 'to keep track of it. not used yet.
    Private _targetProcessHandle As IntPtr = IntPtr.Zero 'Used for ReadProcessMemory

    Dim rct As New Rect
    Dim rphnd as Integer
    Dim lastEmber As Integer = 0

    Dim showchat As Boolean = False
    Dim showcmdtut As Boolean = False
    Dim prevshowcmdtut As Boolean = false
    Dim prevtime As String = ""
    Dim draw As Boolean = false
    Dim showqueue As Boolean = true
    Dim showtime As Boolean = True
    Dim showdate As Boolean = True
    Dim reportloads As Boolean = True


    Dim reportlocks As Boolean = false
    Dim lastlockstate() As Boolean = {False, False, False, false, 
        False, false, False, false, False, false, False, false}
    Dim reportedlockstate As Boolean = false

    Dim loadstart As DateTime



    Dim cmdhistory As New List(Of String)

    Dim textoverlay As New List(Of String)
    Dim texttoggle As Boolean = false

    Dim gamecap As New Drawing.Bitmap(1270, 710)

    Dim pixX As Integer
    Dim pixY As Integer

    Dim loadscreen As Boolean = False
    Dim prevloadscreen As Boolean = false


    Dim ignorelist As New List(Of String)
    Dim modlist As New List(Of String)

    Private rpBase As Int32 = 0
    Private rpCtrlWrap As Int32 = 0
    Private wow64 As Int32 = 0

    Private ctrlPtr As Int32



    Private Sub refTimerPost_Tick() Handles refTimerPost.Tick
        Dim Elems As HtmlElementCollection

        Try
            Elems = wb.Document.GetElementsByTagName("button")
            Elems(Elems.Count-1).InvokeMember("click")
        Catch ex As Exception

        End Try

    End Sub

    Private Sub updTimer_Tick() Handles updTimer.Tick
        'Dim starttime As DateTime = now
        Dim Elems As HtmlElementCollection
        Dim ember As Integer

        Dim entry(2) As String

        Try
            Elems = wb.Document.GetElementsByTagName("li")
            For Each elem As HtmlElement In Elems
                If elem.GetAttribute("id").Contains("ember") Then
                    ember = parseEmber(elem.GetAttribute("id"))
                    If ember > lastEmber Then
                        lastEmber = ember

                        entry = parseChat(elem.InnerText)

                        ProcessCMD(entry)
                    End If
                End If
            Next




            'Dim starttime As DateTime = now
            'Dim a As New Drawing.Bitmap(width, height)
            'Dim b As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(a)
            'b.CopyFromScreen(New Drawing.Point(MousePosition.X, MousePosition.y), New Drawing.Point(0, 0), a.Size)
            'Label1.Text = MousePosition.X & ", " & MousePosition.Y & " = " & 
            'hex(GetColorR(pix)) & " " & hex(GetColorG(pix)) & " " & hex(GetColorB(pix))
            'Label1.Text = MousePosition.X & ", " & MousePosition.Y & " = " & a.GetPixel(0,0).GetBrightness



            drawStuff



            'Console.WriteLine((Now - starttime).TotalMilliseconds)
            

            '988, 295

            'chathistory(0) = "Pixel(988,295) val = " & hex(c.ToArgb)

            'Label1.Text = MousePosition.X & ", " & MousePosition.Y & " = 0"
            'Label1.Text = MousePosition.X & ", " & MousePosition.Y & " = " & hex(getpixelcolor(MousePosition.X, MousePosition.Y))

        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try
    End Sub

    Public Function ScanForProcess(ByVal windowCaption As String, Optional automatic As Boolean = False) As Boolean
        Dim _allProcesses() As Process = Process.GetProcesses
        For Each pp As Process In _allProcesses
            If pp.MainWindowTitle.ToLower.equals(windowCaption.ToLower) Then
                'found it! proceed.
                Return TryAttachToProcess(pp, automatic)
            End If
        Next
        Return False
    End Function
    Public Function TryAttachToProcess(ByVal proc As Process, Optional automatic As Boolean = False) As Boolean
        If Not (_targetProcessHandle = IntPtr.Zero) Then
            DetachFromProcess()
        End If

        _targetProcess = proc
        _targetProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, False, _targetProcess.Id)
        If _targetProcessHandle = 0 Then
            If Not automatic Then 'Showing 2 message boxes as soon as you start the program is too annoying.
                MessageBox.Show("Failed to attach to process.")
            End If

            Return False
        Else
            'if we get here, all connected and ready to use ReadProcessMemory()
            Return True
            'MessageBox.Show("OpenProcess() OK")
        End If

    End Function
    Public Sub DetachFromProcess()
        If Not (_targetProcessHandle = IntPtr.Zero) Then
            _targetProcess = Nothing
            Try
                CloseHandle(_targetProcessHandle)
                _targetProcessHandle = IntPtr.Zero
                'MessageBox.Show("MemReader::Detach() OK")
            Catch ex As Exception
                MessageBox.Show("Warning: MemoryManager::DetachFromProcess::CloseHandle error " & Environment.NewLine & ex.Message)
            End Try
        End If
    End Sub

    Private Sub findDllAddresses()
        For Each dll As ProcessModule In _targetProcess.Modules
            Select Case dll.ModuleName.ToLower
                Case "remoteplay.exe"
                    rpBase = dll.BaseAddress

                Case "rpctrlwrapper.dll"
                    rpCtrlWrap = dll.BaseAddress

                Case "wow64.dll" 'Find steam_api.dll for ability to directly add SteamIDs as nodes
                    wow64 = dll.BaseAddress
            End Select
        Next
    End Sub


    Private Sub DrawTextWithOutline(ByVal text As String, ByVal pt As Point, clr As brush)
        Using g As Graphics = Me.CreateGraphics
            g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y) 'left
            g.DrawString(text, _blackFont, Brushes.Black, pt.X, pt.Y + 1) 'top
            g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y) 'right
            g.DrawString(text, _blackFont, Brushes.Black, pt.X, pt.Y + 1) 'bottom
            g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y - 1) 'top left
            g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y + 1) 'bottom left
            g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y - 1) 'top right
            g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y + 1) 'bottom right
            g.DrawString(text, _whiteFont, clr, pt)
        End Using
    End Sub
    Private Sub DrawTextWithOutline(ByVal text As String, ByVal pt As Point)
        Using g As Graphics = Me.CreateGraphics
            g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y) 'left
            g.DrawString(text, _blackFont, Brushes.Black, pt.X, pt.Y + 1) 'top
            g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y) 'right
            g.DrawString(text, _blackFont, Brushes.Black, pt.X, pt.Y + 1) 'bottom
            g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y - 1) 'top left
            g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y + 1) 'bottom left
            g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y - 1) 'top right
            g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y + 1) 'bottom right
            g.DrawString(text, _whiteFont, Brushes.White, pt)
        End Using
    End Sub
    Private Sub DrawLine(Byval col As Color, Byval xstart As integer, byval ystart As integer, byval xend As integer, byval yend As integer, byval width As Integer)
        Dim g As Graphics = Me.CreateGraphics 
        Dim p As New System.Drawing.Pen(col, width) 
        g.DrawLine(p, xstart, ystart, xend, yend) 
        p.Dispose() 
        g.Dispose() 


    End Sub
    Private Sub ClearOverlay()
        Using g As Graphics = Me.CreateGraphics
            g.Clear(Color.red)
        End Using
    End Sub

    Private Function GetPixelColor(ByVal x As Integer, ByVal y As Integer) As Integer
        Dim a As New Drawing.Bitmap(1, 1)
        Dim b As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(a)
        b.CopyFromScreen(New Drawing.Point(x, y), New Drawing.Point(0, 0), a.Size)
        Dim c As Drawing.Color = a.GetPixel(0, 0)
        b.Dispose

        Return c.ToArgb
    End Function
    Private Function GetPixelBrightness(ByVal x As Integer, ByVal y As Integer) As single
        Dim a As New Drawing.Bitmap(1, 1)
        Dim b As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(a)
        b.CopyFromScreen(New Drawing.Point(x, y), New Drawing.Point(0, 0), a.Size)
        Dim c As single     = a.GetPixel(0, 0).GetBrightness

        b.Dispose
        Return c
    End Function     
    Private Function GetColorR(Byval col As Integer) As Byte
        Return (col And &HFF0000) / &H10000
    End Function
    Private Function GetColorG(Byval col As integer) As Byte
        Return (col And &HFF00) / &H100
    End Function
    Private Function GetColorB(Byval col As integer) As Byte
        Return col And &HFF
    End Function

    Private Function parseEmber(ByVal txt As String) As Integer
        Dim ember = 0
        txt = Microsoft.VisualBasic.Right(txt, txt.Length - 5)
        ember = Val(txt)
        Return ember
    End Function
    Private Function parseChat(ByVal txt As String) As String()
        txt = Microsoft.VisualBasic.Right(txt, txt.Length - InStr(2, txt, Chr(13))).ToLower
        If Asc(txt(0)) = 10 Then txt = Microsoft.VisualBasic.Right(txt, txt.Length - 1)

        If txt.Contains(ChrW(10)) Then
            txt = txt.Split(ChrW(10))(txt.Split(ChrW(10)).Count - 1)
        End If

        Dim username As String
        Dim cmd As string
        username = txt.Split(":")(0).Trim(" ")
        cmd = txt.Split(":")(1).Trim(" ")

        Return {username, cmd}
    End Function
    Private Sub outputChat(ByVal txt As String)
        Dim Elems As HtmlElementCollection
        Dim elem As HtmlElement
        Try
            Elems = wb.Document.GetElementsByTagName("textarea")
            elem = Elems(0)
            elem.InnerText = elem.InnerText & " " & txt
        Catch ex As Exception

        End Try

        If Not refTimerPost.Enabled Then 
            refTimerPost.Interval = 1000
            refTimerPost.Enabled = True
        End If

    End Sub

    Private Sub ProcessCMD(entry() As String)
        Dim tmpuser = entry(0)
        Dim tmpcmd = entry(1)

        Dim x
        Dim y

        Select Case tmpcmd
            Case "showcmdtut"
                showcmdtut = Not showcmdtut

            Case "showqueue"
                showqueue = True
            Case "noshowqueue"
                showqueue = False

            Case "showtime"
                showtime = true
            Case "noshowtime"
                showtime = false
            Case "showdate"
                showdate = true
            Case "noshowdate"
                showdate = false

            Case "reportloads"
                reportloads = True
            Case "noreportloads"
                reportloads = False
                loadscreen = false

            Case "reportlocks"
                reportlocks = true
            Case "noreportlocks"
                reportlocks = False
                reportedlockstate = false
                lastlockstate = {False, False, False, false, 
        False, false, False, false, False, false, False, false}

        End Select

        If tmpcmd = "checkbosshp" Then
            outputChat("Estimated Boss HP: " & checkbossHP & "%")
        End If


        If tmpcmd = "checkhp" Then
            checkHP
        End If

        If tmpcmd = "reconnect1" Then
            Me.WindowState = System.Windows.Forms.FormWindowState.Minimized
        End if
        If tmpcmd = "reconnect2" Then
            x = 1467
            y = 491

            Cursor.Position = New Point(x, y)
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, IntPtr.Zero)
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, IntPtr.Zero)
        End If
        If tmpcmd = "reconnect3" Then
            
            x = 1255
            y = 660

            Cursor.Position = New Point(x, y)
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, IntPtr.Zero)
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, IntPtr.Zero)
        End If

        If tmpcmd = "reconnect4" Then
            x = 1430
            y = 34

            Cursor.Position = New Point(x, y)
            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, IntPtr.Zero)
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, IntPtr.Zero)

            ctrlPtr = RInt32(rpCtrlWrap + &H208a21) + rpCtrlWrap + &H208a25 + &H400
        End If
        If tmpcmd = "reconnect5" Then
            Me.WindowState = System.Windows.Forms.FormWindowState.Normal
            Cursor.Position = New Point(0,0)
        End If

    End Sub


    Private Sub drawStuff()
        

        If ctrlPtr Then
            Dim buttons
            buttons = rint32(ctrlPtr + &Hc)

            Dim x, y As Integer

            x = 600
            y = 400


            Dim user As string
            dim cmd As string
            Dim queuecnt As string
            Dim draw As Boolean = true

            user = RAscStr(ctrlPtr - &H100)
            cmd = RAscStr(ctrlPtr - &HF0)
            queuecnt = RInt32(ctrlPtr - &H40)

            Dim tm As String
            tm = TimeOfDay.ToShortTimeString

            

            If reportlocks Then
                screencap
                Dim lockstate As Boolean = checklockon

                If Not lockstate = reportedlockstate Then
                    Dim consistentlock As Boolean = True
                    For i = 0 To lastlockstate.Count - 2
                        If consistentlock And not (lastlockstate(i) = lastlockstate(i + 1)) Then
                            consistentlock = false
                        End If
                    Next

                    If consistentlock Then
                        outputChat("Lock state: " & lockstate)
                        reportedlockstate = lockstate
                    End If
                End If

                For i = lastlockstate.Count-2 To 1 Step -1
                    lastlockstate(i) = lastlockstate(i-1)
                Next
                lastlockstate(0) = lockstate
            End If

            Dim col As integer
            If reportloads Then
                col = GetPixelColor(1730, 743)
                loadscreen = (CInt(GetColorR(col)) + CInt(GetColorG(col)) + CInt(GetColorB(col))) < &H20

                If Not loadscreen = prevloadscreen Then
                    prevloadscreen = loadscreen
                    If loadscreen Then
                        outputChat("Loading screen active.")
                        loadstart = Now
                    Else
                        Dim elapsed As TimeSpan
                        elapsed = Now - loadstart
                        outputChat("Loading complete. Duration = " & elapsed.TotalSeconds.ToString("0.0 seconds."))
                    End If
                End If
            End If


            If Not tm = prevtime then
                prevtime = tm
            End If


            If draw then
                ClearOverlay

                if Not user = "" Then user = user & "(" & queuecnt & ")"
                DrawTextWithOutline(cmd, New Point(1270 - (cmd.length * 13), 568))
                DrawTextWithOutline(user, New Point(1270 - (user.length * 13), 592),Brushes.Chartreuse)


                If showtime Then

                    DrawTextWithOutline(tm, New Point(1270 - (tm.length*13), 616))
                End If
                If showdate Then
                    Dim dt As String
                    dt = Now.ToString("yyyy/MM/dd")
                    DrawTextWithOutline(dt, New Point(1270 - (dt.length*13), 640))
                End If


                If showqueue Then
                    
                    For i = 0 To 9

                        Dim nextcmd As String = ""
                        nextcmd = RAscStr(ctrlPtr - &HE0 + &H10 * i)
                        DrawTextWithOutline(nextcmd, New Point(1270 - (nextcmd.Length * 13),544 - i * 24), Brushes.Gray)
                    Next
                End If


            End If

        End if

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        wb.Navigate("http://www.twitch.tv/wulf2k/chat")

        updTimer.Interval = 500
        updTimer.Enabled = True
        updTimer.Start()



        TransparencyKey = Color.Red
        Me.SetStyle(ControlStyles.SupportsTransparentBackColor, True)
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
     

        me.Location = New Point(30,30)


        ignorelist.Add("jtv")

        modlist.Add("wulf2k")
        modlist.Add("wulf2kbot")




    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        
        Dim tmphnd As Integer = 0
        Dim wndCount = 0
        Dim _allProcesses() As Process = Process.GetProcesses
        For Each pp As Process In _allProcesses
            If pp.MainWindowTitle.ToLower.Equals("ps4 remote play") Then
                tmphnd = pp.mainwindowHandle
                GetWindowRect(tmphnd, rct)

                wndcount += 1
                If rct.Top > 0 Then
                    rphnd = tmphnd
                    Label1.Text = pp.MainWindowTitle & " - " & wndCount

                    MoveWindow(rphnd, 630, 50, 1312, 760, true)

                    Me.Location = New point(rct.Left - 10, rct.Top - 30)
                    Me.Size = New Size(rct.Right - rct.Left + 5,rct.Bottom - rct.Top + 40)

                End If
            End If
        Next

        If not (rphnd = 0) Then
            'rpCtrlWrap + &H1D0980
            
            ScanForProcess("PS4 Remote Play", True)
            findDllAddresses()

            ctrlPtr = RInt32(rpCtrlWrap + &H208a21) + rpCtrlWrap + &H208a25 + &H400

            Label1.Text = Hex(ctrlPtr)

            

        End If
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Dim x = MousePosition.X
        Dim y = MousePosition.Y
        Dim col = GetPixelColor(x, y)

        Label1.Text = x & ", " & y & "  -  " & hex(GetColorR(col)) & "." & hex(GetColorG(col)) & "." & hex(GetColorB(col)) & " - " & GetPixelBrightness(x,y)
    End Sub






    Public Function RInt8(ByVal addr As IntPtr) As SByte
        Dim _rtnBytes(0) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 1, vbNull)
        Return _rtnBytes(0)
    End Function
    Public Function RInt16(ByVal addr As IntPtr) As Int16
        Dim _rtnBytes(1) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 2, vbNull)
        Return BitConverter.ToInt16(_rtnBytes, 0)
    End Function
    Public Function RInt32(ByVal addr As IntPtr) As Int32
        Dim _rtnBytes(3) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 4, vbNull)

        Return BitConverter.ToInt32(_rtnBytes, 0)
    End Function
    Private Function RAscStr(ByVal addr As UInteger) As String
        Dim Str As String = ""
        Dim cont As Boolean = True
        Dim loc As Integer = 0

        Dim bytes(&H10) As Byte

        ReadProcessMemory(_targetProcessHandle, addr, bytes, &H10, vbNull)

        While (cont And loc < &H10)
            If bytes(loc) > 0 Then

                Str = Str + Convert.ToChar(bytes(loc))

                loc += 1
            Else
                cont = False
            End If
        End While

        Return Str
    End Function

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        screencap
        checklockon

        pbCap.Visible = Not pbCap.Visible


    End Sub
    Private function checkBossHP As integer
        Dim width = 850
        Dim height = 1
        Dim x = 872
        Dim y = 702

        Dim a As New Drawing.Bitmap(width, height)
        Dim b As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(a)
        b.CopyFromScreen(New Drawing.Point(x, y), New Drawing.Point(0, 0), a.Size)
        b.Dispose

        Dim maxpixx = x
        Dim col As integer
        For i = 0 To width-1
            col  = a.GetPixel(i, 0).ToArgb

            If GetColorR(col) > &H50 And getcolorb(col) < &H60 Then
                maxpixx = x + i
            Else
                if maxpixx > x Then Exit For
            End If
        Next

        Return Math.Floor(((maxpixx - x) / 830) * 100)
    End function

    Private sub checkHP
        'Dim starttime As DateTime = now

        Dim width = 700
        Dim height = 1
        Dim x = 720
        Dim y = 126

        Dim a As New Drawing.Bitmap(width, height)
        Dim b As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(a)
        b.CopyFromScreen(New Drawing.Point(x, y), New Drawing.Point(0, 0), a.Size)
        b.Dispose

        Dim maxpixx = x
        Dim col As integer
        For i = 0 To width-1
            col  = a.GetPixel(i, 0).ToArgb

            If GetColorR(col) > &H50 And getcolorg(col) < &H40 Then
                maxpixx = x + i
            Else
                if maxpixx > x Then Exit For
            End If
        Next

        'Console.WriteLine((Now - starttime).TotalMilliseconds)

        outputChat("Estimated HP: " & Math.Floor((maxpixx - x) * 4.5))
        Label1.Text = maxpixx
    End sub
    Private function checklockon As Boolean

        Dim a As New Drawing.Bitmap(50, 50)

        Dim pixbright As Single = 0
        Dim totpix As Integer = 0
       

        For x = 4 To 20
            For y = 0 To 10
                a = imgcut(x * 50, y * 50, 50, 50)
                totpix = 0

                

                For i = 0 To 49
                    For j = 0 To 49
                        pixbright = a.GetPixel(i,j).GetBrightness
                        If pixbright > 0.99 and pixbright < 1 Then
                            totpix += 1
                        End If
                    Next
                Next


                If totpix > 10 And totpix < 20 Then
                    Label1.Text = totpix
                    Return true
                End If
            Next
        Next
        Return false
    End function

    Private sub screencap
        'Dim starttime As DateTime = now

        Dim x = 645
        Dim y = 83

        'Dim a As New Drawing.Bitmap(width, height)
        Dim b As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(gamecap)
        b.CopyFromScreen(New Drawing.Point(x, y), New Drawing.Point(0, 0), gamecap.Size)

        b.Dispose

    End sub
    Private Function imgcut(byval x As Integer, byval y As Integer, byval width As Integer, byval height As Integer) As Bitmap

        Dim to_bm As New Bitmap(width, height)
        Dim gr As Graphics = Graphics.FromImage(to_bm)

        ' Get source and destination rectangles.
        Dim fr_rect As New Rectangle(x, y, width, height)
        Dim to_rect As New Rectangle(0, 0, width, height)

        ' Draw from the source to the destination.
        gr.DrawImage(gamecap, to_rect, fr_rect, GraphicsUnit.Pixel)
        gr.DrawRectangle(Pens.Red, to_rect)

        Return to_bm

    End Function

End Class
