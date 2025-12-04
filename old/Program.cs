using System;
using System.IO;
using System.Linq;
using System.Text;
using Terminal.Gui;

class Program
{
    // UI コンポーネントと状態（クラスの静的フィールドとして管理）
    static ListView leftItems;
    static ListView rightItems;
    static Label pathLabel;
    static Label statusLabel;
    static TextField inputField;

    static string leftPath;
    static string rightPath;
    static string[] leftEntries = Array.Empty<string>();
    static string[] rightEntries = Array.Empty<string>();
    static string[] leftNames = Array.Empty<string>();
    static string[] rightNames = Array.Empty<string>();

    static bool showDetails = false;

    enum UiMode
    {
        Normal,
        MakeDirInput,
        DeleteConfirm
    }

    static UiMode uiMode = UiMode.Normal;
    static string pendingDeletePath = "";
    static bool makeDirLeftPane = true;
    static string lastCreatedDirPath = "";

    static void Main()
    {
        // 日本語などマルチバイト文字を正しく扱うための設定
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Application.Init();

        var top = Application.Top;

        // ウィンドウ（画面全体）
        var win = new Window("TWF - Double Pane Filer")
        {
            X = 0,
            Y = 1, // メニューバー用に 1 行空けることもできる
            Width = Dim.Fill(),
            // 下に 1 行分余白を残して、罫線の位置を 1 行上にする
            Height = Dim.Fill(1)
        };
        top.Add(win);

        // 左右ペインの ListView
        // - ウィンドウ内いっぱいまで使い、枠線の 1 行上までファイル一覧を表示
        // - 各ペインの一番右を 1 桁空けて「スクロールバー的エリア」にする（実際のバーは後で実装）
        leftItems = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50) - 1,
            Height = Dim.Fill()
        };
        rightItems = new ListView
        {
            X = Pos.Percent(50),
            Y = 0,
            Width = Dim.Fill(1) - 1,
            Height = Dim.Fill()
        };

        win.Add(leftItems, rightItems);

        // 最下行のメッセージ/入力エリア（画面の最下行）
        // Window とは別レイヤー（top）に配置して、枠線の外に出す
        var bottomY = Pos.AnchorEnd(1);
        statusLabel = new Label("")
        {
            X = 0,
            Y = bottomY,
            Width = Dim.Fill(),
            Height = 1
        };
        inputField = new TextField("")
        {
            X = 0,
            Y = bottomY,
            Width = Dim.Fill(),
            Visible = false
        };
        top.Add(statusLabel, inputField);

        // 初期ディレクトリ（とりあえずハードコード、後で CONFIG.TXT から読む）
        leftPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        rightPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // ペインごとの現在のエントリ一覧（フルパス）
        leftEntries = GetEntries(leftPath);
        rightEntries = GetEntries(rightPath);

        leftNames = leftEntries.Select(FormatEntry).ToArray();
        rightNames = rightEntries.Select(FormatEntry).ToArray();

        leftItems.SetSource(leftNames);
        rightItems.SetSource(rightNames);

        // フォーカス制御（Tab で左右切替）
        leftItems.CanFocus = true;
        rightItems.CanFocus = true;
        leftItems.SetFocus();

        // 上部に現在のパスを表示するラベル
        pathLabel = new Label(0, 0, $"L: {leftPath}    R: {rightPath}");
        top.Add(pathLabel);

        // キーハンドラ登録
        top.KeyDown += Top_KeyDown;

        Application.Run();
        Application.Shutdown();
    }

    // ディレクトリ読み込み関数（フルパス一覧）
    static string[] GetEntries(string path)
    {
        try
        {
            var dirs = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path);
            return dirs.Concat(files).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    // 1 行分の表示文字列を作る
    static string FormatEntry(string path)
    {
        var name = Path.GetFileName(path);
        if (!showDetails)
        {
            return name;
        }

        try
        {
            var isDir = Directory.Exists(path);
            var info = isDir ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);
            var sizeStr = isDir ? "<DIR>".PadLeft(10) :
                (info is FileInfo fi ? fi.Length.ToString().PadLeft(10) : "".PadLeft(10));
            var timeStr = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            return $"{name,-40} {sizeStr} {timeStr}";
        }
        catch
        {
            return name;
        }
    }

    static void UpdatePathLabel()
    {
        pathLabel.Text = $"L: {leftPath}    R: {rightPath}";
    }

    static void SetStatus(string message)
    {
        statusLabel.Text = message ?? "";
    }

    // 両ペインの内容を再読み込みして再描画する
    // ・カーソル位置（SelectedItem）
    // ・スクロール位置（TopItem）
    // を可能な限り維持する
    static void RefreshPanes()
    {
        // 左ペイン
        int leftSelected = leftItems.SelectedItem;
        int leftTop = leftItems.TopItem;
        leftEntries = GetEntries(leftPath);
        leftNames = leftEntries.Select(FormatEntry).ToArray();
        leftItems.SetSource(leftNames);

        if (leftNames.Length == 0)
            leftItems.SelectedItem = -1;
        else if (leftSelected >= 0 && leftSelected < leftNames.Length)
            leftItems.SelectedItem = leftSelected;
        else
            leftItems.SelectedItem = leftNames.Length - 1;

        if (leftNames.Length == 0)
            leftItems.TopItem = 0;
        else if (leftTop >= 0 && leftTop < leftNames.Length)
            leftItems.TopItem = leftTop;
        else
            leftItems.TopItem = Math.Max(0, leftNames.Length - 1);

        // 右ペイン
        int rightSelected = rightItems.SelectedItem;
        int rightTop = rightItems.TopItem;
        rightEntries = GetEntries(rightPath);
        rightNames = rightEntries.Select(FormatEntry).ToArray();
        rightItems.SetSource(rightNames);

        if (rightNames.Length == 0)
            rightItems.SelectedItem = -1;
        else if (rightSelected >= 0 && rightSelected < rightNames.Length)
            rightItems.SelectedItem = rightSelected;
        else
            rightItems.SelectedItem = rightNames.Length - 1;

        if (rightNames.Length == 0)
            rightItems.TopItem = 0;
        else if (rightTop >= 0 && rightTop < rightNames.Length)
            rightItems.TopItem = rightTop;
        else
            rightItems.TopItem = Math.Max(0, rightNames.Length - 1);

        UpdatePathLabel();
    }

    // 作成したディレクトリにカーソルを移動する
    static void FocusCreatedDirectory()
    {
        if (string.IsNullOrEmpty(lastCreatedDirPath))
            return;

        // 左ペイン側にあるか？
        int idx = Array.FindIndex(leftEntries, p =>
            string.Equals(p, lastCreatedDirPath, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            leftItems.SelectedItem = idx;
            leftItems.TopItem = Math.Max(0, idx - 1);
            leftItems.SetFocus();
            lastCreatedDirPath = "";
            return;
        }

        // 右ペイン側をチェック
        idx = Array.FindIndex(rightEntries, p =>
            string.Equals(p, lastCreatedDirPath, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            rightItems.SelectedItem = idx;
            rightItems.TopItem = Math.Max(0, idx - 1);
            rightItems.SetFocus();
            lastCreatedDirPath = "";
        }
    }

    static void OpenLeft()
    {
        if (leftEntries.Length == 0) return;
        if (leftItems.SelectedItem < 0 || leftItems.SelectedItem >= leftEntries.Length) return;

        var target = leftEntries[leftItems.SelectedItem];
        if (Directory.Exists(target))
        {
            leftPath = target;
            leftEntries = GetEntries(leftPath);
            leftNames = leftEntries.Select(FormatEntry).ToArray();
            leftItems.SetSource(leftNames);
            leftItems.SelectedItem = 0;
            UpdatePathLabel();
        }
        else
        {
            // ファイルの場合の動作は後で実装
        }
    }

    static void OpenRight()
    {
        if (rightEntries.Length == 0) return;
        if (rightItems.SelectedItem < 0 || rightItems.SelectedItem >= rightEntries.Length) return;

        var target = rightEntries[rightItems.SelectedItem];
        if (Directory.Exists(target))
        {
            rightPath = target;
            rightEntries = GetEntries(rightPath);
            rightNames = rightEntries.Select(FormatEntry).ToArray();
            rightItems.SetSource(rightNames);
            rightItems.SelectedItem = 0;
            UpdatePathLabel();
        }
        else
        {
            // ファイルの場合の動作は後で実装
        }
    }

    static void GoParentLeft()
    {
        try
        {
            var parent = Directory.GetParent(leftPath);
            if (parent == null) return;

            leftPath = parent.FullName;
            leftEntries = GetEntries(leftPath);
            leftNames = leftEntries.Select(FormatEntry).ToArray();
            leftItems.SetSource(leftNames);
            leftItems.SelectedItem = 0;
            UpdatePathLabel();
        }
        catch
        {
        }
    }

    static void GoParentRight()
    {
        try
        {
            var parent = Directory.GetParent(rightPath);
            if (parent == null) return;

            rightPath = parent.FullName;
            rightEntries = GetEntries(rightPath);
            rightNames = rightEntries.Select(FormatEntry).ToArray();
            rightItems.SetSource(rightNames);
            rightItems.SelectedItem = 0;
            UpdatePathLabel();
        }
        catch
        {
        }
    }

    static void Top_KeyDown(View.KeyEventEventArgs args)
    {
        var key = args.KeyEvent.Key;

        // 特殊モード中のキー処理
        if (uiMode == UiMode.MakeDirInput)
        {
            if (key == Key.Enter)
            {
                var name = inputField.Text.ToString() ?? "";
                name = name.Trim();
                inputField.Visible = false;

                if (!string.IsNullOrEmpty(name))
                {
                    var basePath = makeDirLeftPane ? leftPath : rightPath;
                    var full = Path.Combine(basePath, name);
                    try
                    {
                        Directory.CreateDirectory(full);
                        SetStatus($"Created: {name}");
                        lastCreatedDirPath = full;
                        RefreshPanes();

                        // 作成したディレクトリにカーソルを合わせる
                        FocusCreatedDirectory();
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"MakeDir error: {ex.Message}");
                    }
                }
                else
                {
                    SetStatus("MakeDir canceled");
                }

                uiMode = UiMode.Normal;
                args.Handled = true;
                return;
            }

            if (key == Key.Esc)
            {
                inputField.Visible = false;
                uiMode = UiMode.Normal;
                SetStatus("MakeDir canceled");
                args.Handled = true;
                return;
            }

            return;
        }

        if (uiMode == UiMode.DeleteConfirm)
        {
            if (key == Key.y || key == Key.Y)
            {
                try
                {
                    if (Directory.Exists(pendingDeletePath))
                        Directory.Delete(pendingDeletePath, recursive: true);
                    else if (File.Exists(pendingDeletePath))
                        File.Delete(pendingDeletePath);
                    SetStatus($"Deleted: {Path.GetFileName(pendingDeletePath)}");
                    RefreshPanes();
                }
                catch (Exception ex)
                {
                    SetStatus($"Delete error: {ex.Message}");
                }

                uiMode = UiMode.Normal;
                args.Handled = true;
                return;
            }

            if (key == Key.n || key == Key.N || key == Key.Esc)
            {
                SetStatus("Delete canceled");
                uiMode = UiMode.Normal;
                args.Handled = true;
                return;
            }

            args.Handled = true;
            return;
        }

        // Tab: フォーカス切替
        if (key == Key.Tab)
        {
            if (leftItems.HasFocus)
                rightItems.SetFocus();
            else
                leftItems.SetFocus();

            args.Handled = true;
            return;
        }

        // Enter: ディレクトリに入る
        if (key == Key.Enter)
        {
            if (leftItems.HasFocus)
                OpenLeft();
            else if (rightItems.HasFocus)
                OpenRight();

            args.Handled = true;
            return;
        }

        // Backspace: 親ディレクトリへ
        if (key == Key.Backspace)
        {
            if (leftItems.HasFocus)
                GoParentLeft();
            else if (rightItems.HasFocus)
                GoParentRight();

            args.Handled = true;
            return;
        }

        // F3: 詳細表示 ON/OFF
        if (key == Key.F3)
        {
            showDetails = !showDetails;
            RefreshPanes();
            args.Handled = true;
            return;
        }

        // F5: コピー（単一ファイル）
        if (key == Key.F5)
        {
            bool leftActive = leftItems.HasFocus;
            var srcEntries = leftActive ? leftEntries : rightEntries;
            var srcView = leftActive ? leftItems : rightItems;
            var dstPath = leftActive ? rightPath : leftPath;

            if (srcEntries.Length == 0 || srcView.SelectedItem < 0 || srcView.SelectedItem >= srcEntries.Length)
            {
                args.Handled = true;
                return;
            }

            var srcFile = srcEntries[srcView.SelectedItem];
            if (!File.Exists(srcFile))
            {
                args.Handled = true;
                return;
            }

            var fileName = Path.GetFileName(srcFile);
            var dstFile = Path.Combine(dstPath, fileName);

            try
            {
                File.Copy(srcFile, dstFile, overwrite: false);
            }
            catch (IOException ex)
            {
                MessageBox.ErrorQuery("Copy Error", ex.Message, "OK");
            }

            RefreshPanes();
            args.Handled = true;
            return;
        }

        // F6: 移動
        if (key == Key.F6)
        {
            bool leftActive = leftItems.HasFocus;
            var srcEntries = leftActive ? leftEntries : rightEntries;
            var srcView = leftActive ? leftItems : rightItems;
            var dstPath = leftActive ? rightPath : leftPath;

            if (srcEntries.Length == 0 || srcView.SelectedItem < 0 || srcView.SelectedItem >= srcEntries.Length)
            {
                args.Handled = true;
                return;
            }

            var srcPath = srcEntries[srcView.SelectedItem];
            var fileName = Path.GetFileName(srcPath);
            var dstFull = Path.Combine(dstPath, fileName);

            try
            {
                if (Directory.Exists(srcPath))
                    Directory.Move(srcPath, dstFull);
                else
                    File.Move(srcPath, dstFull);
            }
            catch (IOException ex)
            {
                MessageBox.ErrorQuery("Move Error", ex.Message, "OK");
            }

            RefreshPanes();
            args.Handled = true;
            return;
        }

        // F7: ディレクトリ作成
        if (key == Key.F7)
        {
            uiMode = UiMode.MakeDirInput;
            makeDirLeftPane = leftItems.HasFocus || !rightItems.HasFocus;
            inputField.Text = "";
            inputField.Visible = true;
            inputField.SetFocus();
            SetStatus("New folder name (Enter: OK, Esc: cancel):");

            args.Handled = true;
            return;
        }

        // F8: 削除
        if (key == Key.F8)
        {
            bool leftActive = leftItems.HasFocus;
            var srcEntries = leftActive ? leftEntries : rightEntries;
            var srcView = leftActive ? leftItems : rightItems;

            if (srcEntries.Length == 0 || srcView.SelectedItem < 0 || srcView.SelectedItem >= srcEntries.Length)
            {
                args.Handled = true;
                return;
            }

            pendingDeletePath = srcEntries[srcView.SelectedItem];
            var fileName = Path.GetFileName(pendingDeletePath);
            uiMode = UiMode.DeleteConfirm;
            SetStatus($"Delete \"{fileName}\" ? (y/N, Esc: cancel)");

            args.Handled = true;
            return;
        }

        // q / Q: 終了
        if (key == Key.Q || key == Key.q)
        {
            Application.RequestStop();
            args.Handled = true;
            return;
        }

        // Ctrl+L: 再読み込み
        if (key == (Key.L | Key.CtrlMask))
        {
            RefreshPanes();
            args.Handled = true;
            return;
        }
    }
}
