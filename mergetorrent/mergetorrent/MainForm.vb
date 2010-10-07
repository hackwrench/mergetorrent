﻿Public Class MainForm

    Private Const SHA1_HASHBYTES As Int64 = 20

    Private Class SourceItem
        Inherits ListViewItem

        Enum SourceItemType
            Torrent
            Directory
            File
        End Enum

        Private path_ As String
        Private Type_ As SourceItemType
        Private Processed_ As Double = -1
        Private Completion_ As Double = -1
        Private Recovered_ As Double = -1
        Private Status_ As String = ""

        Property Path As String
            Get
                Return path_
            End Get
            Set(ByVal value As String)
                path_ = value
            End Set
        End Property

        ReadOnly Property Type As SourceItemType
            Get
                Return Type_
            End Get
        End Property

        Property Processed As Double
            Get
                Return Processed_
            End Get
            Set(ByVal value As Double)
                Processed_ = value
                Me.SubItems(0).Text = Processed_.ToString("P02")
            End Set
        End Property

        Property Completion As Double
            Get
                Return Completion_
            End Get
            Set(ByVal value As Double)
                Completion_ = value
                Me.SubItems(1).Text = Completion_.ToString("P02")
            End Set
        End Property

        Property Recovered As Double
            Get
                Return Recovered_
            End Get
            Set(ByVal value As Double)
                Recovered_ = value
                Me.SubItems(2).Text = Recovered_.ToString("P02")
            End Set
        End Property

        Property Status As String
            Get
                Return Status_
            End Get
            Set(ByVal value As String)
                Status_ = value
                Me.SubItems(3).Text = Status_
            End Set
        End Property

        Sub New(ByVal Path As String, ByVal Type As SourceItemType)
            If Type = SourceItemType.Torrent Then
                Me.ToolTipText = Path
                Me.Text = My.Computer.FileSystem.GetName(Path)
                Me.SubItems.Add("") 'processed
                Me.SubItems.Add("") 'completion
                Me.SubItems.Add("") 'recovered
                Me.SubItems.Add("") 'status
            Else
                Me.Text = Path
            End If
            Me.Path = Path
            Me.Type_ = Type
        End Sub
    End Class

    Private Sub btnAddTorrents_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddTorrents.Click
        Dim ofd As New OpenFileDialog

        ofd.AddExtension = True
        ofd.AutoUpgradeEnabled = True
        ofd.CheckFileExists = True
        ofd.CheckPathExists = True
        ofd.DefaultExt = ".torrent"
        ofd.DereferenceLinks = True
        ofd.Filter = "Torrents (*.torrent)|*.torrent|All files (*.*)|*.*"
        If System.IO.Directory.Exists(My.Computer.FileSystem.CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent")) Then
            ofd.InitialDirectory = My.Computer.FileSystem.CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent")
        End If
        ofd.Multiselect = True
        ofd.Title = "Find Torrent(s)"
        If ofd.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
            For Each filename As String In ofd.FileNames
                Dim si As New SourceItem(filename, SourceItem.SourceItemType.Torrent)
                si.Group = lvSources.Groups("lvgTorrents")
                lvSources.Items.Add(si)
            Next
        End If
        lvSources_ItemCountChanged(Me, Nothing)
    End Sub

    Private Sub btnAddFiles_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddFiles.Click
        Dim ofd As New OpenFileDialog

        ofd.AddExtension = True
        ofd.AutoUpgradeEnabled = True
        ofd.CheckFileExists = True
        ofd.CheckPathExists = True
        ofd.DereferenceLinks = True
        ofd.Filter = "All files (*.*)|*.*"
        ofd.Multiselect = True
        ofd.Title = "Find Source File(s)"
        If ofd.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
            For Each filename As String In ofd.FileNames
                Dim si As New SourceItem(filename, SourceItem.SourceItemType.File)
                si.Group = lvSources.Groups("lvgFilesAndDirectories")
                lvSources.Items.Add(si)
            Next
        End If
        lvSources_ItemCountChanged(Me, Nothing)
    End Sub

    Private Sub btnAddDirectory_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddDirectory.Click
        Dim fbd As New FolderBrowserDialog
        fbd.ShowNewFolderButton = False
        fbd.Description = "Find Source Directory (all subdirectories will be included)"
        If fbd.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
            Dim si As New SourceItem(fbd.SelectedPath, SourceItem.SourceItemType.Directory)
            si.Group = lvSources.Groups("lvgFilesAndDirectories")
            lvSources.Items.Add(si)
        End If
        lvSources_ItemCountChanged(Me, Nothing)
    End Sub

    Private Sub lvSources_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lvSources.SelectedIndexChanged
        btnClear.Enabled = lvSources.SelectedIndices.Count > 0
    End Sub

    Private Sub lvSources_ItemCountChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        btnClearAll.Enabled = lvSources.Items.Count > 0
        btnStart.Enabled = False
        For Each li As SourceItem In lvSources.Items
            If li.Type = SourceItem.SourceItemType.Torrent Then
                btnStart.Enabled = True
            End If
        Next
    End Sub

    Private Sub btnClear_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClear.Click
        If lvSources.SelectedIndices.Count > 0 Then
            For i As Integer = lvSources.Items.Count - 1 To 0 Step -1
                If lvSources.Items(i).Selected Then
                    lvSources.Items.RemoveAt(i)
                End If
            Next
        End If
        lvSources_ItemCountChanged(Me, Nothing)
    End Sub

    Private Sub btnClearAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClearAll.Click
        If lvSources.Items.Count > 0 Then
            lvSources.Items.Clear()
        End If
        lvSources_ItemCountChanged(Me, Nothing)
    End Sub

    Private Sub lnkMergeTorrent_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles lnkMergeTorrent.LinkClicked
        Process.Start("http://code.google.com/p/mergetorrent/")
    End Sub

    Private Function GetResumeDat() As Dictionary(Of String, Object)
        Dim resume_dat_fs As System.IO.FileStream
        If System.IO.File.Exists(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent"), "resume.dat")) Then
            resume_dat_fs = System.IO.File.OpenRead(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent"), "resume.dat"))
        Else
            Dim ofd As New OpenFileDialog

            ofd.AddExtension = True
            ofd.AutoUpgradeEnabled = True
            ofd.CheckFileExists = True
            ofd.CheckPathExists = True
            ofd.DefaultExt = ".dat"
            ofd.DereferenceLinks = True
            ofd.Filter = "resume.dat|resume.dat|All files (*.*)|*.*"
            ofd.Title = "Open resume.dat"
            If System.IO.Directory.Exists(My.Computer.FileSystem.CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent")) Then
                ofd.InitialDirectory = My.Computer.FileSystem.CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent")
            End If
            ofd.Multiselect = False
            If ofd.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
                resume_dat_fs = System.IO.File.OpenRead(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent"), "resume.dat"))
            Else
                Throw New ApplicationException("Can't find resume.dat")
            End If
        End If
        Dim resume_dat As Dictionary(Of String, Object) = DirectCast(Bencode.Decode(resume_dat_fs), Dictionary(Of String, Object))
        resume_dat_fs.Close()
        Return resume_dat
    End Function

    Private Function TorrentFilenameToMultiPath(ByVal torrent_filename As String) As List(Of MultiFileStream.FileInfo)
        TorrentFilenameToMultiPath = New List(Of MultiFileStream.FileInfo)
        Dim resume_dat As Dictionary(Of String, Object) = GetResumeDat()

        Dim torrent_fs As System.IO.FileStream = System.IO.File.OpenRead(torrent_filename)
        Dim torrent As Dictionary(Of String, Object) = DirectCast(Bencode.Decode(torrent_fs), Dictionary(Of String, Object))
        torrent_fs.Close()
        Dim current_torrent As Dictionary(Of String, Object) = DirectCast(resume_dat(My.Computer.FileSystem.GetName(torrent_filename)), Dictionary(Of String, Object))

        Dim info As Dictionary(Of String, Object)
        info = DirectCast(torrent("info"), Dictionary(Of String, Object))
        If resume_dat.ContainsKey(My.Computer.FileSystem.GetName(torrent_filename)) Then
            If info.ContainsKey("files") Then
                For file_index As Integer = 0 To DirectCast(DirectCast(torrent("info"), Dictionary(Of String, Object))("files"), List(Of Object)).Count - 1
                    Dim f As Dictionary(Of String, Object) = DirectCast(DirectCast(DirectCast(torrent("info"), Dictionary(Of String, Object))("files"), List(Of Object))(file_index), Dictionary(Of String, Object))
                    Dim source_filename As String = System.Text.Encoding.UTF8.GetString(DirectCast(current_torrent("path"), Byte()))
                    For Each path_element As Byte() In DirectCast(f("path"), List(Of Object))
                        source_filename = My.Computer.FileSystem.CombinePath(source_filename, System.Text.Encoding.UTF8.GetString(path_element))
                    Next
                    If current_torrent.ContainsKey("targets") Then 'override
                        For Each current_target As List(Of Object) In DirectCast(current_torrent("targets"), List(Of Object))
                            If DirectCast(current_target(0), Long) = file_index Then
                                source_filename = System.Text.Encoding.UTF8.GetString(DirectCast(current_target(1), Byte()))
                                Exit For
                            End If
                        Next
                    End If

                    Dim fi As New MultiFileStream.FileInfo(source_filename, DirectCast(f("length"), Long))
                    TorrentFilenameToMultiPath.Add(fi)
                Next
            ElseIf info.ContainsKey("length") Then
                If current_torrent.ContainsKey("path") Then
                    Dim source_filename As String = System.Text.Encoding.UTF8.GetString(DirectCast(current_torrent("path"), Byte()))
                    Dim fi As New MultiFileStream.FileInfo(source_filename, DirectCast(info("length"), Long))
                    TorrentFilenameToMultiPath.Add(fi)
                End If
            End If
        End If
    End Function

    ''' <summary>
    ''' Find all files that are a certain length
    ''' </summary>
    ''' <param name="target_length"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function FindAllByLength(ByVal target_length As Long) As List(Of String)
        FindAllByLength = New List(Of String)

        For Each possible_source As SourceItem In lvSources.Items
            Select Case possible_source.Type
                Case SourceItem.SourceItemType.Torrent
                    Dim br As New System.IO.BinaryReader(System.IO.File.OpenRead(possible_source.Path))
                    Dim possible_source_torrent As Dictionary(Of String, Object) = Bencode.DecodeDictionary(br)
                    br.Close()
                    Dim possible_source_info As Dictionary(Of String, Object)
                    possible_source_info = DirectCast(possible_source_torrent("info"), Dictionary(Of String, Object))
                    Dim m As List(Of MultiFileStream.FileInfo) = TorrentFilenameToMultiPath(possible_source.Path)
                    'now we have files to look at
                    For Each fi As MultiFileStream.FileInfo In m
                        For Each s As String In fi.Path
                            If Not FindAllByLength.Contains(s) AndAlso _
                               My.Computer.FileSystem.FileExists(s) AndAlso _
                               My.Computer.FileSystem.GetFileInfo(s).Length = target_length Then
                                FindAllByLength.Add(s)
                            End If
                        Next
                    Next
                Case SourceItem.SourceItemType.File
                    If Not FindAllByLength.Contains(possible_source.Path) AndAlso _
                       My.Computer.FileSystem.FileExists(possible_source.Path) AndAlso _
                       My.Computer.FileSystem.GetFileInfo(possible_source.Path).Length = target_length Then
                        FindAllByLength.Add(possible_source.Path)
                    End If
                Case SourceItem.SourceItemType.Directory
                    'we don't use GetFiles recursive feature because there might be some directories that we can't read.
                    Dim directory_stack As New Queue(Of System.IO.DirectoryInfo)
                    directory_stack.Enqueue(My.Computer.FileSystem.GetDirectoryInfo(possible_source.Path))
                    Do While directory_stack.Count > 0
                        Try
                            For Each f As System.IO.FileInfo In directory_stack.Peek.GetFiles
                                If Not FindAllByLength.Contains(f.FullName) AndAlso _
                                   f.Length = target_length Then
                                    FindAllByLength.Add(f.FullName)
                                End If
                            Next
                        Catch ex As UnauthorizedAccessException
                            'do nothing, we'll just skip this directory
                        End Try
                        Try
                            For Each d As System.IO.DirectoryInfo In directory_stack.Peek.GetDirectories
                                directory_stack.Enqueue(d)
                            Next
                        Catch ex As UnauthorizedAccessException
                            'do nothing, we'll just skip this directory
                        End Try
                        directory_stack.Dequeue() 'don't need it anymore
                    Loop
            End Select
        Next
    End Function

    Private Sub btnStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStart.Click
        btnStart.Enabled = False
        btnStart.Text = "Running..."
        Dim current_listitem_index As Integer = 0
        Do While current_listitem_index < lvSources.Items.Count
            Dim current_listitem As SourceItem = DirectCast(lvSources.Items(current_listitem_index), SourceItem)
            If current_listitem.Type = SourceItem.SourceItemType.Torrent Then
                Dim out_stream As MultiFileStream
                Dim in_stream As MultiFileStream
                current_listitem.Status = "Finding destination files..."
                lvSources.Items(current_listitem_index) = current_listitem
                My.Application.DoEvents()
                Dim files As List(Of MultiFileStream.FileInfo) = TorrentFilenameToMultiPath(current_listitem.Path)
                out_stream = New MultiFileStream(files, IO.FileMode.OpenOrCreate, IO.FileAccess.ReadWrite, IO.FileShare.ReadWrite)

                current_listitem.Status = "Finding source files..."
                lvSources.Items(current_listitem_index) = current_listitem
                My.Application.DoEvents()
                Dim lfi As New List(Of MultiFileStream.FileInfo)
                For Each fi As MultiFileStream.FileInfo In files
                    Dim new_paths As List(Of String) = FindAllByLength(fi.Length)
                    If new_paths.Contains(fi.Path(0)) AndAlso new_paths.IndexOf(fi.Path(0)) <> 0 Then 'make it first if it's there
                        new_paths.Remove(fi.Path(0))
                        new_paths.Insert(0, fi.Path(0))
                    End If

                    Dim new_fi As New MultiFileStream.FileInfo(new_paths, fi.Length)
                    lfi.Add(new_fi)

                    current_listitem.Status &= "."
                    lvSources.Items(current_listitem_index) = current_listitem
                    My.Application.DoEvents()
                Next
                in_stream = New MultiFileStream(lfi, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)

                current_listitem.Status = "" 'we'll update very soon anyway
                'now we have all the files that might work.  Start checking and merging.

                Dim torrent As Dictionary(Of String, Object)
                Dim br As New System.IO.BinaryReader(System.IO.File.OpenRead(current_listitem.Path))
                torrent = Bencode.DecodeDictionary(br)
                br.Close()
                Dim info As Dictionary(Of String, Object)
                info = DirectCast(torrent("info"), Dictionary(Of String, Object))

                Dim piece_len As Integer = CType(info("piece length"), Integer)
                Dim buffer(0 To (piece_len - 1)) As Byte
                Dim hash_result() As Byte
                Dim pieces() As Byte = DirectCast(info("pieces"), Byte())
                Dim pieces_position As Integer = 0
                Dim complete_bytes As Long = 0
                Dim recovered_bytes As Long = 0
                Dim doevents_period As TimeSpan = New TimeSpan(0, 0, 0, 0, 500) 'every 1/2 second
                Dim last_doevents As Date = Date.MinValue

                Do While pieces_position < pieces.Length
                    If last_doevents + doevents_period <= Now Then
                        current_listitem.Completion = CDbl(complete_bytes) / CDbl(out_stream.Length)
                        current_listitem.Processed = CDbl(out_stream.Position) / CDbl(out_stream.Length)
                        current_listitem.Recovered = CDbl(recovered_bytes) / CDbl(out_stream.Length)
                        lvSources.Items(current_listitem_index) = current_listitem
                        My.Application.DoEvents()
                        last_doevents = Now
                    End If
                    Dim read_len As Integer

                    read_len = CInt(Math.Min(piece_len, in_stream.Length - in_stream.Position))
                    'try the out_stream first
                    out_stream.Read(buffer, 0, read_len)
                    hash_result = CheckHash.Hash(buffer, read_len)
                    Dim i As Integer = 0
                    Do While i < 20 AndAlso pieces(pieces_position + i) = hash_result(i)
                        i += 1
                    Loop
                    If i = 20 Then
                        'match!  No need to read from the in_stream
                        in_stream.Position += read_len
                        complete_bytes += read_len
                    Else
                        out_stream.Position -= read_len 'back up
                        Dim useful_permutation As List(Of Integer) = in_stream.GetPermutation()
                        Do
                            in_stream.Read(buffer, 0, read_len)
                            hash_result = CheckHash.Hash(buffer, read_len)
                            i = 0
                            Do While i < 20 AndAlso pieces(pieces_position + i) = hash_result(i)
                                i += 1
                            Loop
                            If i = 20 Then
                                'match!
                                complete_bytes += read_len
                                recovered_bytes += read_len
                                out_stream.Write(buffer, 0, read_len)
                                Exit Do
                            Else
                                'no match, try the next permutation
                                in_stream.NextPermutation(in_stream.Position - read_len, read_len)
                                If MultiFileStream.ComparePermutation(in_stream.GetPermutation, useful_permutation) Then
                                    'this piece can't be completed, let's move on
                                    out_stream.Position += read_len
                                    Exit Do
                                Else
                                    in_stream.Position -= read_len 'try again with the new permutation
                                End If
                            End If
                        Loop
                    End If
                    pieces_position += 20
                Loop
                current_listitem.Completion = CDbl(complete_bytes) / CDbl(out_stream.Length)
                current_listitem.Processed = CDbl(out_stream.Position) / CDbl(out_stream.Length)
                current_listitem.Recovered = CDbl(recovered_bytes) / CDbl(out_stream.Length)
                lvSources.Items(current_listitem_index) = current_listitem
                My.Application.DoEvents()
                last_doevents = Now
            End If
            current_listitem_index = current_listitem_index + 1
        Loop
        btnStart.Enabled = True
        btnStart.Text = "Start!"
    End Sub
End Class