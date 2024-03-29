﻿Imports System
Imports System.Threading
Imports System.IO
Imports System.IO.MemoryMappedFiles
Imports System.Runtime.InteropServices


Public Class frmPS4TwitchHelper

    Private Declare Function GetWindowRect Lib "user32" Alias "GetWindowRect" (ByVal hwnd As Integer, ByRef lpRect As RECT) As Integer

    <DllImport("user32.dll", EntryPoint:="GetWindowLong")>
    Public Shared Function GetWindowLong(
        ByVal hWnd As IntPtr,
        ByVal nIndex As GWL
            ) As Integer
    End Function
    <DllImport("user32.dll", EntryPoint:="SetWindowLong")>
    Public Shared Function SetWindowLong(
        ByVal hWnd As IntPtr,
        ByVal nIndex As GWL,
        ByVal dwNewLong As WS_EX
            ) As Integer
    End Function

    Public Enum WindowLongFlags As Integer
        GWL_EXSTYLE = -20
        GWLP_HINSTANCE = -6
        GWLP_HWNDPARENT = -8
        GWL_ID = -12
        GWL_STYLE = -16
        GWL_USERDATA = -21
        GWL_WNDPROC = -4
        DWLP_USER = &H8
        DWLP_MSGRESULT = &H0
        DWLP_DLGPROC = &H4
    End Enum
    Public Enum GWL As Integer
        ExStyle = -20
    End Enum
    Public Enum WS_EX As Integer
        Transparent = &H20
        Layered = &H80000
    End Enum
    Public Enum LWA As Integer
        ColorKey = &H1
        Alpha = &H2
    End Enum

    Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure


    Dim mmf As MemoryMappedFile
    Dim mmfa As MemoryMappedViewAccessor


    Private _whiteFont As New Font("Courier New", 16)
    Private _blackFont As New Font("Courier New", 16)

    Private WithEvents updTimer As New System.Windows.Forms.Timer()


    Private Declare Function OpenProcess Lib "kernel32.dll" (ByVal dwDesiredAcess As UInt32, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Int32) As IntPtr
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByVal lpNumberOfBytesWritten As Integer) As Boolean
    Private Declare Function CloseHandle Lib "kernel32.dll" (ByVal hObject As IntPtr) As Boolean
    Private Declare Function VirtualAllocEx Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As IntPtr, ByVal flAllocationType As Integer, ByVal flProtect As Integer) As IntPtr
    Private Declare Function VirtualProtectEx Lib "kernel32.dll" (hProcess As IntPtr, lpAddress As IntPtr, ByVal lpSize As IntPtr, ByVal dwNewProtect As UInt32, ByRef dwOldProtect As UInt32) As Boolean
    Private Declare Function VirtualFreeEx Lib "kernel32.dll" (hProcess As IntPtr, lpAddress As IntPtr, ByVal dwSize As Integer, ByVal dwFreeType As Integer) As Boolean
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As Integer, ByVal lpThreadAttributes As Integer, ByVal dwStackSize As Integer, ByVal lpStartAddress As Integer, ByVal lpParameter As Integer, ByVal dwCreationFlags As Integer, ByRef lpThreadId As Integer) As Integer




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



    Public Const RemotePlayHookLoc = &H1BEB90



    Private _targetProcess As Process = Nothing 'to keep track of it. not used yet.
    Private _targetProcessHandle As IntPtr = IntPtr.Zero 'Used for ReadProcessMemory

    Dim rct As New Rect
    Dim rphnd as Integer


    Dim showqueue As Boolean = true
    Dim showtime As Boolean = True
    Dim showdate As Boolean = True




    Private rpBase As Int32 = 0
    Private rpCtrlWrap As Int32 = 0
    Private wow64 As Int32 = 0

    Private ctrlPtr As Int32




    Private wb As New WebBrowser

    Private Sub updTimer_Tick() Handles updTimer.Tick
        Try
            Me.Invalidate()
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try
    End Sub

    Public Function ScanForProcess(ByVal windowCaption As String, Optional automatic As Boolean = False) As Boolean
        Dim _allProcesses() As Process = Process.GetProcesses
        For Each pp As Process In _allProcesses
            If pp.MainWindowTitle.ToLower.equals(windowCaption.ToLower) Then
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
            Return False
        Else
            'if we get here, all connected and ready to use ReadProcessMemory()
            Return True
        End If

    End Function
    Public Sub DetachFromProcess()
        If Not (_targetProcessHandle = IntPtr.Zero) Then
            _targetProcess = Nothing
            Try
                CloseHandle(_targetProcessHandle)
                _targetProcessHandle = IntPtr.Zero

            Catch ex As Exception

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

                Case "wow64.dll"
                    wow64 = dll.BaseAddress
            End Select
        Next
    End Sub


    Private Sub DrawTextWithOutline(g As Graphics, ByVal text As String, ByVal pt As Point, clr As brush)
        g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y) 'left
        g.DrawString(text, _blackFont, Brushes.Black, pt.X, pt.Y + 1) 'top
        g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y) 'right
        g.DrawString(text, _blackFont, Brushes.Black, pt.X, pt.Y + 1) 'bottom
        g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y - 1) 'top left
        g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y + 1) 'bottom left
        g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y - 1) 'top right
        g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y + 1) 'bottom right
        g.DrawString(text, _whiteFont, clr, pt)
    End Sub
    Private Sub DrawTextWithOutline(g As Graphics, ByVal text As String, ByVal pt As Point)
        g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y) 'left
        g.DrawString(text, _blackFont, Brushes.Black, pt.X, pt.Y + 1) 'top
        g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y) 'right
        g.DrawString(text, _blackFont, Brushes.Black, pt.X, pt.Y + 1) 'bottom
        g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y - 1) 'top left
        g.DrawString(text, _blackFont, Brushes.Black, pt.X - 1, pt.Y + 1) 'bottom left
        g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y - 1) 'top right
        g.DrawString(text, _blackFont, Brushes.Black, pt.X + 1, pt.Y + 1) 'bottom right
        g.DrawString(text, _whiteFont, Brushes.White, pt)
    End Sub
    Private Sub DrawLine(g As Graphics, Byval col As Color, Byval xstart As integer, byval ystart As integer, byval xend As integer, byval yend As integer, byval width As Integer)
        Dim p As New System.Drawing.Pen(col, width) 
        g.DrawLine(p, xstart, ystart, xend, yend) 
        p.Dispose() 
    End Sub
    Private Sub ClearOverlay(g As Graphics)
        g.Clear(Color.red)
    End Sub




    Private Sub drawStuff(sender As Object, e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint
        Dim g As Graphics = e.Graphics


        'ClearOverlay()
        'DrawTextWithOutline("Testing", MousePosition)
        ' wb.Location = New Point(MousePosition.X + 100, MousePosition.Y + 100)




        '        If ctrlPtr Then
        If True Then
            'Dim buttons
            'buttons = RInt32(ctrlPtr + &HC)

            Dim x, y As Integer

            x = 600
            y = 400


            Dim user As String
            Dim cmd As String
            Dim queuecnt As String
            Dim draw As Boolean = True

            Dim b(&H20) As Byte
            Dim index As Int16


            'user = RAscStr(ctrlPtr - &H100)
            mmfa.ReadArray(&H300, b, 0, b.Length)
            user = System.Text.Encoding.ASCII.GetString(b)
            index = user.IndexOf(vbNullChar)
            If index >= 0 Then user = user.Remove(index)

            mmfa.ReadArray(&H310, b, 0, b.Length)
            cmd = System.Text.Encoding.ASCII.GetString(b)
            index = cmd.IndexOf(vbNullChar)
            If index >= 0 Then cmd = cmd.Remove(index)


            queuecnt = mmfa.ReadInt32(&H3C0)

            Dim tm As String
            tm = TimeOfDay.ToShortTimeString



            If draw Then
                ClearOverlay(g)


                If Not user = "" Then user = user & "(" & queuecnt & ")"

                DrawTextWithOutline(g, cmd, New Point(1320 - (cmd.Length * 13), 630))
                DrawTextWithOutline(g, user, New Point(1320 - (user.Length * 13), 670), Brushes.Chartreuse)



                If showtime Then


                    DrawTextWithOutline(g, tm, New Point(1320 - (tm.Length * 13), 720))
                End If
                If showdate Then
                    Dim dt As String
                    dt = Now.ToString("yyyy/MM/dd")
                    DrawTextWithOutline(g, dt, New Point(1320 - (dt.Length * 13), 700))
                End If


                If showqueue Then

                    For i = 0 To 9

                        Dim nextcmd As String = ""
                        mmfa.ReadArray(&H320 + &H10 * i, b, 0, b.Length)
                        Try
                            nextcmd = System.Text.Encoding.ASCII.GetString(b).Split(Chr(0))(0)

                            DrawTextWithOutline(g, nextcmd, New Point(1320 - (nextcmd.Length * 13), 600 - i * 24), Brushes.Gray)
                        Catch ex As Exception

                        End Try

                    Next
                End If


            End If

        End If

    End Sub



    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        updTimer.Interval = 16.667
        updTimer.Enabled = True
        updTimer.Start()

        TransparencyKey = Color.Red
        Me.SetStyle(ControlStyles.SupportsTransparentBackColor, True)
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint, True)

        Me.ControlBox = False
        Me.Text = ""

        Me.WindowState = FormWindowState.Maximized
        Me.TopMost = True



        Dim InitialStyle As Integer
        Dim NewStyle As Integer
        InitialStyle = GetWindowLong(Me.Handle, GWL.ExStyle)
        NewStyle = InitialStyle Or WS_EX.Layered Or WS_EX.Transparent
        SetWindowLong(Me.Handle, GWL.ExStyle, NewStyle)

        connect()

        'Me.Controls.Add(wb)
        'wb.DocumentText = "<html><body>Test</body></html>"
        'wb.Width = 100
        'wb.Height = 100




    End Sub

    Private Sub connect()

        'Dim tmphnd As Integer = 0
        'Dim wndCount = 0
        'Dim _allProcesses() As Process = Process.GetProcesses
        'For Each pp As Process In _allProcesses
        'If pp.MainWindowTitle.ToLower.Equals("ps4 remote play") Then
        'tmphnd = pp.MainWindowHandle
        'GetWindowRect(tmphnd, rct)

        'Console.WriteLine(tmphnd)

        'rphnd = tmphnd

        'End If
        'Next
        'Console.WriteLine(rphnd)
        'If Not (rphnd = 0) Then


        'ScanForProcess("PS4 Remote Play", True)
        'findDllAddresses()

        'ctrlPtr = RInt32(rpCtrlWrap + RemotePlayHookLoc + 1) + rpCtrlWrap + RemotePlayHookLoc + 5 + &H400
        'Console.WriteLine(ctrlPtr)


        'End If


        mmf = MemoryMappedFile.CreateOrOpen("TwitchControl", &H1000)
        mmfa = mmf.CreateViewAccessor()
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
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


End Class
