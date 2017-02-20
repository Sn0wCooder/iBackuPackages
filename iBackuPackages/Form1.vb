﻿Imports System.Management
Imports System.Net
Imports System.IO.Compression
Imports System.IO

Public Class Form1

    '----------INIZIO DEFINIZIONI----------

    Dim da As String = My.Computer.FileSystem.SpecialDirectories.CurrentUserApplicationData & "\"
    Dim WithEvents client As New WebClient
    Dim CurrentVersionInteger As Integer = 1
    Dim CurrentVersion As String = "1.0"
    Dim IfTheAppIsInBeta As Boolean = True
    Dim msgBeta As String = ""

    '----------FINE DEFINIZIONI----------

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If ControlBox = False Then
            e.Cancel = True
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        '----------terminando se è attiva la connessione SSH tramite USB----------

        End_SSH_Over_USB()

        '----------cartella temporanea----------

        CreateTempFolder()
        'Process.Start(da)
        Download_iproxy()

        '----------c'è o non c'è la DLL di Renci----------

        CheckForRenciDLL()

        '----------aggiornamento----------

        Updater()

        '----------parti grafiche del programma----------

        Me.Text = "iBackuPackages v." & CurrentVersion
        LinkLabel2.Location = New Point(Label4.Size.Width + 5, LinkLabel2.Location.Y)

    End Sub

    Public Sub CheckForRenciDLL()
        If Not System.IO.File.Exists(Application.StartupPath & "\Renci.SshNet.dll") Then
            My.Computer.FileSystem.WriteAllBytes(Application.StartupPath & "\Renci.SshNet.dll", My.Resources.Renci_SshNet, True)
        End If
    End Sub

    Public Sub Download_iproxy()
        If Not IO.Directory.Exists(da & "libimobiledevice") Then

            MsgBox("Downloading iproxy (only for the first time). It will take a while!", MsgBoxStyle.Information, "Warning")

            'download

            client.DownloadFile(New Uri("https://github.com/Sn0wCooder/libimobiledevice-compiled-windows/archive/master.zip"), da & "libimobiledevice.zip")

            'wait for the download

            'client

            'unzip
            'Process.Start(da)
            'IO.Directory.CreateDirectory(da & "libimobiledevice")
            ZipFile.ExtractToDirectory(da & "libimobiledevice.zip", da)
            IO.File.Delete(da & "libimobiledevice.zip")
            My.Computer.FileSystem.RenameDirectory(da & "libimobiledevice-compiled-windows-master", "libimobiledevice")
        End If


    End Sub

    Public Sub CreateTempFolder()
        If Not My.Computer.FileSystem.DirectoryExists(da) Then
            My.Computer.FileSystem.CreateDirectory(da)
        End If
    End Sub

    'Public Sub CleanUpResources()
    ' End_SSH_Over_USB()
    'If My.Computer.FileSystem.DirectoryExists(da) Then
    'My.Computer.FileSystem.DeleteDirectory(da, FileIO.DeleteDirectoryOption.DeleteAllContents)
    'End If
    'End Sub

    Public Sub ResetAll()
        Button1.Enabled = True
        Button2.Enabled = True
        ControlBox = True
        pb.Value = 0
        StatusLabel.Text = "Status: idle"
        End_SSH_Over_USB()
    End Sub

    Public Shared Function CheckForInternetConnection() As Boolean
        Try
            Using client = New WebClient()
                Using stream = client.OpenRead("http://www.google.com")
                    Return True
                End Using
            End Using
        Catch
            Return False
        End Try
    End Function

    Public Function GetTempFolder() As String
        Dim tempdir As String = Path.Combine(Path.GetTempPath, Path.GetRandomFileName)
        Do While Directory.Exists(tempdir) Or File.Exists(tempdir)
            tempdir = Path.Combine(Path.GetTempPath, Path.GetRandomFileName)
        Loop
        Return tempdir
    End Function

    Public Function RemoveBlankLines(file As String)
        Dim fold As String = GetTempFolder() & "\"
        My.Computer.FileSystem.CreateDirectory(fold)

        Dim sr As New IO.StreamReader(file)
        Dim line1 As String = sr.ReadLine
        Dim sw As New System.IO.StreamWriter(fold & "noblank.txt")
        While Not line1 Is Nothing
            If Not line1 = "" Then
                sw.WriteLine(line1)
            End If
            line1 = sr.ReadLine
        End While
        sw.Close()
        sr.Close()
        IO.File.Delete(file)
        IO.File.Copy(fold & "noblank.txt", file)

        ' Dim watcher As New FileSystemWatcher
        ' watcher.Filter = "*.txt"
        ' watcher.created+=new
        ' watcher.Path = Path.GetDirectoryName(file)
        ' watcher.EnableRaisingEvents = True
        ' watcher.WaitForChanged(WatcherChangeTypes.All)

        ' My.Computer.FileSystem.DeleteDirectory(fold, FileIO.DeleteDirectoryOption.DeleteAllContents, FileIO.RecycleOption.DeletePermanently)
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try
            Button1.Enabled = False
            Button2.Enabled = False
            ControlBox = False

            '----------controllo se il dispositivo è connesso al computer o no----------

            If IsUserlandConnected() = False Then
                MsgBox("Error: please connect a device before continue.", MsgBoxStyle.Critical, "Error")
                ResetAll()
                'Exit Sub
            End If

            '----------SaveFileDialog per il salvataggio del backup----------

            If Not SaveFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
                ResetAll()
                Exit Sub
            End If

            '----------provando a connettersi in SSH tramite USB----------

            pb.Value = 20
            StatusLabel.Text = "Status: 20% - trying to start a SSH connection over USB..."
            'CopyFilesitunnel_mux()
            End_SSH_Over_USB()
            SSH_Over_USB("22", "22")
            Dim connInfo As New Renci.SshNet.PasswordConnectionInfo("127.0.0.1", "root", "alpine")
            Dim sshClient As New Renci.SshNet.SshClient(connInfo)
            Dim sftpClient As New Renci.SshNet.SftpClient(connInfo)
            Dim cmd As Renci.SshNet.SshCommand
            Try
                If sshClient.IsConnected = False Then
                    sshClient.Connect()
                End If
                If sshClient.IsConnected = True Then
                    sshClient.Disconnect()
                End If
            Catch ex As Exception
                MsgBox("There was an error during the procedure. Make sure the device is connected to the computer and that it have installed " _
                     & "OpenSSH and APT 0.6 Transitional from Cydia, and that the default password is " + """" + "alpine" + """" + "than retry. If the problem persists, restart your device or contact me." _
                     , MsgBoxStyle.Critical, "Warning")
                ResetAll()
                'MsgBox(ex.Message)
                Exit Sub
            End Try

            '----------connessione SSH e SFTP----------

            If sshClient.IsConnected = False Then
                sshClient.Connect()
            End If
            If sftpClient.IsConnected = False Then
                sftpClient.Connect()
            End If

            '----------creazione directory temporanea----------

            Dim tempdir = GetTempFolder() & "\"
            My.Computer.FileSystem.CreateDirectory(tempdir)
            Process.Start(tempdir)

            '----------backup dei tweak installati----------

            pb.Value = 50
            StatusLabel.Text = "Status: 50% - getting all tweaks installed on the device..."
            cmd = sshClient.RunCommand("dpkg --get-selections")
            Dim Tweaks As String = cmd.Result
            Tweaks = Tweaks.Replace("install", Nothing)
            My.Computer.FileSystem.WriteAllText(tempdir & "tweaks.txt", Tweaks, True)

            '----------backup delle sorgenti----------

            pb.Value = 70
            StatusLabel.Text = "Status: 70% - backup sources..."
            cmd = sshClient.RunCommand("ls /etc/apt/sources.list.d")
            'MsgBox(cmd.Result)
            Dim repo As String = cmd.Result
            repo = repo.Replace("saurik.list", Nothing)

            Dim RepoFile As System.IO.StreamWriter
            RepoFile = My.Computer.FileSystem.OpenTextFileWriter(tempdir & "repolist.txt", True)
            RepoFile.WriteLine(repo)
            RepoFile.Close()

            RemoveBlankLines(tempdir & "repolist.txt")

            My.Computer.FileSystem.CreateDirectory(tempdir & "repos")

            For Each repos As String In File.ReadLines(tempdir & "repolist.txt")
                sftpClient.DownloadFile("/etc/apt/sources.list.d/" & repos, File.OpenWrite(tempdir & "repos\" & repos))
            Next

            File.Delete(tempdir & "repolist.txt")

            '----------final part----------

            '-----disconnessione SSH tramite USB-----

            End_SSH_Over_USB()

            '-----audio finale-----

            My.Computer.Audio.Play(My.Resources.sound_completed, AudioPlayMode.Background)

            '-----disconnettendo da SSH e SFTP

            If sshClient.IsConnected = True Then
                sshClient.Disconnect()
            End If
            If sftpClient.IsConnected = True Then
                sftpClient.Disconnect()
            End If

            'zip all

            ZipFile.CreateFromDirectory(tempdir, SaveFileDialog1.FileName)

            '-----parte finalissima: PrograssBar, Label e Timer-----

            pb.Value = 100
            StatusLabel.Text = "Status: 100% - done!"
            ControlBox = True
            AfterTimer.Start()
        Catch ex As Exception

            '----------ERROREEEEEE----------

            MsgBox("There was an error during the procedure. Please contact me. The error is: " & ex.Message, MsgBoxStyle.Critical, "Error")
            ResetAll()

        End Try
    End Sub

    Public Shared Sub iproxy(args As String)
        Dim iproxy As New Process()
        Try
            iproxy.StartInfo.UseShellExecute = False
            iproxy.StartInfo.FileName = Form1.da + "libimobiledevice\iproxy.exe"
            iproxy.StartInfo.Arguments = args
            iproxy.StartInfo.CreateNoWindow = True
            iproxy.Start()
        Catch ex As Exception
        End Try
    End Sub

    Public Shared Sub SSH_Over_USB(iport As String, lport As String)
        iproxy(iport + " " + lport)
    End Sub

    Public Shared Sub End_SSH_Over_USB()
        Kill({"iproxy"})
    End Sub

    Public Shared Sub Kill(ProcessesList As String())
        For Each ProcessName In ProcessesList
            Dim SubProcesses() As Process = Process.GetProcessesByName(ProcessName)
            For Each SubProcess As Process In SubProcesses
                If IsProcessRunning(SubProcess.ProcessName) = True Then
                    SubProcess.Kill()
                End If
            Next
        Next
    End Sub

    Public Shared Function IsProcessRunning(name As String) As Boolean
        For Each clsProcess As Process In Process.GetProcesses()
            If clsProcess.ProcessName.StartsWith(name) Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Shared Function IsUserlandConnected()
        Dim forever As Boolean = True
        Dim USBName As String = String.Empty
        Dim USBSearcher As New ManagementObjectSearcher( _
                      "root\CIMV2", _
                      "SELECT * FROM Win32_PnPEntity WHERE Description = 'Apple Mobile Device USB Driver'")
        For Each queryObj As ManagementObject In USBSearcher.Get()
            USBName += (queryObj("Description"))
        Next
        If USBName = "Apple Mobile Device USB Driver" Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Shared Function IsDFUConnected()
        Dim forever As Boolean = True
        Dim text1 As String = ""
        text1 = " "
        Dim searcher As New ManagementObjectSearcher( _
                  "root\CIMV2", _
                  "SELECT * FROM Win32_PnPEntity WHERE Description = 'Apple Recovery (DFU) USB Driver'")
        For Each queryObj As ManagementObject In searcher.Get()

            text1 += (queryObj("Description"))
        Next
        If text1.Contains("DFU") Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Shared Function IsRecoveryConnected()
        Dim text1 As String = ""
        text1 = " "
        Dim searcher As New ManagementObjectSearcher( _
                  "root\CIMV2", _
                  "SELECT * FROM Win32_PnPEntity WHERE Description = 'Apple Recovery (iBoot) USB Driver'")
        For Each queryObj As ManagementObject In searcher.Get()

            text1 += (queryObj("Description"))
        Next
        If text1.Contains("iBoot") Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles AfterTimer.Tick
        AfterTimer.Stop()
        ResetAll()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Try
            Button1.Enabled = False
            Button2.Enabled = False
            ControlBox = False
            If IsUserlandConnected() = False Then
                MsgBox("Error: please connect a device before continue.", MsgBoxStyle.Critical, "Error")
                ResetAll()
                Exit Sub
            End If
            If Not OpenFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
                ResetAll()
                Exit Sub
            End If

            '----------provando a connttersi in SSH----------

            pb.Value = 20
            StatusLabel.Text = "Status: 20% - trying to start a SSH connection over USB..."

            '----------copiando i file di itunnel_mux----------

            'CopyFilesitunnel_mux()

            '----------terminando la connessione SSH via USB----------

            End_SSH_Over_USB()

            '----------iniziando una connessione SSH via USB----------

            SSH_Over_USB("22", "22")

            '----------variabili delle info della connessione----------

            Dim connInfo As New Renci.SshNet.PasswordConnectionInfo("127.0.0.1", "root", "alpine")
            Dim sshClient As New Renci.SshNet.SshClient(connInfo)
            Dim sftpClient As New Renci.SshNet.SftpClient(connInfo)
            Dim cmd As Renci.SshNet.SshCommand
            Try
                If sshClient.IsConnected = False Then
                    sshClient.Connect()
                End If
                If sshClient.IsConnected = True Then
                    sshClient.Disconnect()
                End If
            Catch ex As Exception

                '----------ERROREEEEEEEEE----------

                MsgBox("There was an error during the procedure. Make sure the device is connected to the computer and that it have installed " _
                     & "OpenSSH and APT 0.6 Transitional from Cydia, and that the default password is " + """" + "alpine" + """" + "than retry. If the problem persists, restart your device or contact me." _
                     , MsgBoxStyle.Critical, "Warning")
                ResetAll()
                Exit Sub

            End Try

            '----------Connettendo SSH e SFTP----------

            If sshClient.IsConnected = False Then
                sshClient.Connect()
            End If

            If sftpClient.IsConnected = False Then
                sftpClient.Connect()
            End If

            '----------iniziando a refreshare le sorgenti----------

            pb.Value = 50
            StatusLabel.Text = "Status: 50% - updating sources..."

            '-----creando le cartelle se mancano-----

            If Not sftpClient.Exists("/var/lib/apt/lists/partial") Then
                sftpClient.CreateDirectory("/var/lib/apt/lists/partial")
            End If
            pb.Value = 15
            StatusLabel.Text = "Status: 15% - respringing device..."
            cmd = sshClient.RunCommand("killall SpringBoard")
            pb.Value = 20
            StatusLabel.Text = "Status: 20% - updating sources..."
            If Not sftpClient.Exists("/var/lib/apt") Then
                sftpClient.CreateDirectory("/var/lib/apt")
            End If
            If Not sftpClient.Exists("/var/lib/apt/lists") Then
                sftpClient.CreateDirectory("/var/lib/apt/lists")
            End If
            If Not sftpClient.Exists("/var/lib/apt/lists/partial") Then
                sftpClient.CreateDirectory("/var/lib/apt/lists/partial")
            End If

            '-----refresh sources-----

            cmd = sshClient.RunCommand("apt-get update")

            '-----iniziando a installare tutti i tweak-----

            pb.Value = 70
            For Each tweak In System.IO.File.ReadLines(OpenFileDialog1.FileName)
                StatusLabel.Text = "Status: 70% - installing tweak " & tweak & "..."
                cmd = sshClient.RunCommand("apt-get install -y " & tweak)
            Next

            '----------uicache----------

            pb.Value = 90
            StatusLabel.Text = "Status: 90% - clearing UICache..."
            cmd = sshClient.RunCommand("uicache")

            '----------riavviando il dispositivo---------

            pb.Value = 99
            StatusLabel.Text = "Status: 99% - rebooting device..."
            Try
                cmd = sshClient.RunCommand("reboot")
            Catch ex As Exception
            End Try

            '----------terminando itunnel_mux.exe----------

            End_SSH_Over_USB()

            'audio

            My.Computer.Audio.Play(My.Resources.sound_completed, AudioPlayMode.Background)

            '----------disconnettendo dall'sshClient e sftpClient----------

            If sshClient.IsConnected = True Then
                sshClient.Disconnect()
            End If
            If sftpClient.IsConnected = True Then
                sftpClient.Disconnect()
            End If

            '----------parte finale----------

            pb.Value = 100
            StatusLabel.Text = "Status: 100% - done!"
            ControlBox = True
            AfterTimer.Start()
        Catch ex As Exception

            'OH NO! ERORRE! D:

            MsgBox("There was an error during the procedure. Please contact me. The error is: " & ex.Message, MsgBoxStyle.Critical, "Error")
            ResetAll()
        End Try
    End Sub

    Public Sub Updater()
        If CheckForInternetConnection() = True Then '----------se la connessione a internet è disponibile----------
            Dim versione As Integer = client.DownloadString("http://leoalfred.altervista.org/app/ibackupackages/versione.txt")
            If versione > CurrentVersionInteger Then '-----se è disponibile un aggiornamento-----
                Dim nuovaversione As String = client.DownloadString("http://leoalfred.altervista.org/app/ibackupackages/latestversion.txt")
                LinkLabel1.Show()
                Label1.Show()
                Label3.Hide()
                Label1.Text = "A new version of this application is avaiable: " & nuovaversione.ToString & ". Download it"
                LinkLabel1.Location = New Point(Label1.Size.Width + 9, LinkLabel1.Location.Y)
            Else '-----se nessun aggiornnamento è disponibile----------
                LinkLabel1.Hide()
                Label1.Show()
                Label3.Hide()
                Label1.Text = "Hi! This is the latest version of iBackuPackages."
            End If
        Else '----------se la connessione a internet non è disponibile----------
            LinkLabel1.Hide()
            Label1.Show()
            Label3.Hide()
            Label1.Text = "Unable to check for update. Your internet connection isn't avaiable."
        End If
    End Sub

    Private Sub LinkLabel2_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel2.LinkClicked
        Process.Start("http://leoalfred.altervista.org/twitter.html")
    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        Process.Start("http://leoalfred.altervista.org/app/ibackupackages")
    End Sub
End Class
