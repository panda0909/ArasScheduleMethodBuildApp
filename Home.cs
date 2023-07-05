using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ScintillaNET;
using ArasScheduleMethodBuildApp.Utils;
using System.Drawing;
using System.Configuration;
using ArasScheduleMethodBuildApp.Code;

namespace ArasScheduleMethodBuildApp
{
    public partial class Home : Form
    {
        public string _appDir = "";
        public string _appCodeDir = "";
        public string _appInnoBinDir = "";
		public string _is_backrun = "";
		public string _is_auto_login = "";
		public string _arg = "";
		public ArasLib arasLib = null;

		ScintillaNET.Scintilla TextArea;

        public Home()
        {
            InitializeComponent();
			_is_backrun = ConfigurationManager.AppSettings["is_backrun"];
			_is_auto_login = ConfigurationManager.AppSettings["is_auto_login"];

		}

        private void Home_Load(object sender, EventArgs e)
        {
            this.Text = "自製Method 排程工具 V" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString();
            
            _appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _appCodeDir = _appDir + "/Code/";
            _appInnoBinDir = _appDir + "/InnoBin/";

			_arg = File.ReadAllText(_appDir + "/arg.txt");
			txtArg.Text = _arg;
			// CREATE CONTROL
			TextArea = new ScintillaNET.Scintilla();
			panelCode.Controls.Add(TextArea);

			// BASIC CONFIG
			TextArea.Dock = System.Windows.Forms.DockStyle.Fill;
			TextArea.TextChanged += (this.OnTextChanged);

			// INITIAL VIEW CONFIG
			TextArea.WrapMode = WrapMode.None;
			TextArea.IndentationGuides = IndentView.LookBoth;

			ResetView();

			if (_is_backrun == "1")
            {
				RunWork(_arg);
				Environment.Exit(0);
			}

            if (_is_auto_login == "1")
            {
				LoginAras();
			}

			
			

			#region "編輯器顯示"
			

			// STYLING
			InitColors();
			InitSyntaxColoring();

			// NUMBER MARGIN
			InitNumberMargin();

			// BOOKMARK MARGIN
			InitBookmarkMargin();

			// CODE FOLDING MARGIN
			InitCodeFolding();

			// DRAG DROP
			InitDragDropFile();

			// INIT HOTKEYS
			InitHotkeys();
            #endregion
        }
		private void Home_FormClosed(object sender, FormClosedEventArgs e)
		{
			Environment.Exit(0);
		}
		private void cboCodelist_SelectedIndexChanged(object sender, EventArgs e)
        {
            string current_file = cboCodelist.SelectedItem.ToString();
            ResetCodeText(current_file);
        }

        private void ResetCodeText(string filename)
        {
            TextArea.Text = ReadCodeText(filename);
        }

        private void btnComplier_Click(object sender, EventArgs e)
        {
            string log = "";
            SaveCode();
            RunComplier(TextArea.Text,ref log);
            MessageBox.Show(log);
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            string arg = txtArg.Text;
            RunWork(arg);
			txtLog.Text = Logger.ReadTodayLog();
		}

		private void btnSaveArg_Click(object sender, EventArgs e)
		{
			File.WriteAllText(_appDir + "/arg.txt", txtArg.Text);
		}

		private void chbIsBackRun_CheckedChanged(object sender, EventArgs e)
		{
            if (chbIsBackRun.Checked)
            {
				SaveAppConfig("is_backrun", "1");
            }
            else
            {
				SaveAppConfig("is_backrun", "0");
			}

		}

		private void btnRefreshLog_Click(object sender, EventArgs e)
		{
			txtLog.Text = Logger.ReadTodayLog();
		}
		private void btnResetLog_Click(object sender, EventArgs e)
		{
			Logger.ResetTodayLog();
			txtLog.Text = Logger.ReadTodayLog();
		}

		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
		{
            if (tabControl1.SelectedTab.Name == "tabPageLog")
            {
				txtLog.Text = Logger.ReadTodayLog();
			}
		}
		private void btnSaveLogin_Click(object sender, EventArgs e)
		{
			LoginAras();
		}
		private void btnDownloadMethod_Click(object sender, EventArgs e)
		{
			string search = txtSearchMethod.Text;
			if(this.arasLib == null)
            {
				MessageBox.Show("請先登入Aras");
				return;
            }
			SaveAppConfig("search_method", search);
			DialogResult result = MessageBox.Show("您確定要執行此操作嗎？會更新所有的測試Program", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

			if (result == DialogResult.Yes)
			{
				// 確定按鈕被點擊
				this.arasLib.DownloadMethod(search, _appCodeDir, "all");
			}
			else if (result == DialogResult.No)
			{
				// 取消按鈕被點擊
			}
			
		}
		private void btnDownloadMethodOnly_Click(object sender, EventArgs e)
		{
			string search = txtSearchMethod.Text;
			if (this.arasLib == null)
			{
				MessageBox.Show("請先登入Aras");
				return;
			}
			SaveAppConfig("search_method", search);
			this.arasLib.DownloadMethod(search, _appCodeDir, "method_only");
		}

		private void btnRunMethodStep_Click(object sender, EventArgs e)
		{
			string txtMethod = txtMethodStep.Text;
			string[] method_steps = txtMethod.Split('\n');
			SaveAppConfig("method_steps", string.Join(",", method_steps));
			foreach(string method in method_steps)
            {
				Type type = Type.GetType("InnoScheduleMethodBuildApp.Code.Temp_"+this.arasLib.domain+"_"+this.arasLib.database+"_"+method);
				object instance = Activator.CreateInstance(type);
				IProgram program = (IProgram)instance;
                try
                {
					program.run();
					MessageBox.Show("結束");
				}
				catch(Exception ex)
                {
					Logger.Debug("Run Method Steps Error", ex.Message);

				}
				
			}
		}
		private void btnConvertProperty_Click(object sender, EventArgs e)
		{
			string aml = txtItemAML.Text;

			string result = arasLib.ConvertAMLtoSetPropertyCode(aml);
			Logger.Debug("ConvertProperty", result);
		}
		private void chbAutoLogin_CheckedChanged(object sender, EventArgs e)
		{
			if (chbAutoLogin.Checked)
			{
				SaveAppConfig("is_auto_login", "1");
			}
			else
			{
				SaveAppConfig("is_auto_login", "0");
			}
		}
		private void Home_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.F5)
            {
				btnRunMethodStep_Click(this, new EventArgs());
			}
		}
		#region "----- 自訂函式 -----"
		public void LoginAras()
        {
			string plm_url = txtPLMUrl.Text;
			string db = txtDB.Text;
			string ad = txtAD.Text;
			string pwd = txtPWD.Text;

            if (plm_url == "" || db == "" || ad == "" || pwd == "")
            {
				MessageBox.Show("登入失敗");
				return;
            }
			SaveAppConfig("plm_url", plm_url);
			SaveAppConfig("plm_db", db);
			SaveAppConfig("plm_ad", ad);
			SaveAppConfig("plm_pwd", pwd);

			this.arasLib = new ArasLib(plm_url, db, ad, pwd);
			if (this.arasLib.CheckLogin())
			{
				MessageBox.Show("登入成功");
            }
            else
            {
				MessageBox.Show("登入失敗");
			}
		}

		/// <summary>
		/// 執行主程式
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public string RunWork(string arg="")
        {
			string result = "";

			try
            {
				Logger.Debug("RunWork", "開始加載"+ _appDir + "\\Work.dll");
				// 加載 Work.dll 程序集
				Assembly assembly = Assembly.Load(File.ReadAllBytes(_appDir + "\\Work.dll"));

				// 獲取 MyClass 類別的類型
				Type type = assembly.GetType("InnoScheduleMethodBuildApp.Code.Work");

				// 創建 MyClass 類別的實例
				object instance = Activator.CreateInstance(type);

				// 獲取 MyClass.MyMethod 方法的信息
				MethodInfo methodInfo = type.GetMethod("Method");

				Logger.Debug("RunWork", "調用方法，並取得結果");
				// 調用方法，並取得結果
				string parameter = arg;
				result = (string)methodInfo.Invoke(instance, new object[] { parameter });
				assembly = null;
				Logger.Debug("RunWork", "完成並結束\r\n"+result);
			}
			catch(Exception ex)
            {
				Logger.Debug("RunWork", ex.ToString());
            }
            return result;
        }
		

		/// <summary>
		/// 編譯程式
		/// </summary>
		/// <param name="code"></param>
		/// <param name="log"></param>
		/// <returns></returns>
		private bool RunComplier(string code, ref string log)
		{
			// 使用 C# 編譯器編譯代碼
			CSharpCodeProvider provider = new CSharpCodeProvider();
			CompilerParameters parameters = new CompilerParameters();
			//parameters.GenerateExecutable = true;
			parameters = AddInnoBins(parameters);
			parameters.OutputAssembly = _appDir + "/work.dll"; // 指定輸出 DLL 的名稱
			CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
			// 如果編譯成功，則執行代碼
			if (results.Errors.HasErrors)
			{
				//MessageBox.Show("編譯錯誤：" + results.Errors[0].ErrorText);
				log = "編譯錯誤：" + results.Errors[0].ErrorText;
				return false;
			}
			else
			{
				//MessageBox.Show("編譯完成");
				log = "編譯完成";
				return true;
			}
		}
		
		/// <summary>
		/// 讀取程式碼檔案
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		private string ReadCodeText(string filename)
        {
            string content = "";
            if (File.Exists(_appCodeDir + filename))
            {
                content = File.ReadAllText(_appCodeDir + filename);
                
            }
            return content;
        }
        
		/// <summary>
		/// 保存程式碼檔案
		/// </summary>
		private void SaveCode()
        {
            string filepath = _appCodeDir + cboCodelist.SelectedItem;
            string content = TextArea.Text;
            File.WriteAllText(filepath, content);
        }
        
		/// <summary>
        /// 重新整理Code清單
        /// </summary>
        private void ResetCodeFileList()
        {
            string[] files = Directory.GetFiles(_appCodeDir);
            foreach (var f in files)
            {
                cboCodelist.Items.Add(Path.GetFileName(f));
            }
            if (cboCodelist.Items.Count > 0)
            {
                cboCodelist.SelectedIndex = 0;
				//FileName.Text = cboCodelist.SelectedItem.ToString();
			}
			

		}
		
		/// <summary>
		/// 讀取資料夾的Bin元件
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
        private CompilerParameters AddInnoBins(CompilerParameters parameters)
        {
            List<string> items = GetInnoBinFiles();
            foreach(var item in items)
            {
                if(Path.GetExtension(item)==".dll")
                    parameters.ReferencedAssemblies.Add(item);
            }
            return parameters;
        } 
        
		/// <summary>
        /// 讀取InnoBin的參考元件
        /// </summary>
        /// <returns></returns>
        private List<string> GetInnoBinFiles()
        {
            List<string> bins = new List<string>();
            bins = Directory.GetFiles(_appInnoBinDir).ToList();
            return bins;
        }

		/// <summary>
		/// 刷新介面
		/// </summary>
		private void ResetView()
        {
			//顯示程式碼下拉清單
			ResetCodeFileList();
			ResetAppConfig();
		}
		
		/// <summary>
		/// 刷新AppSetting
		/// </summary>
		private void ResetAppConfig()
        {
			_is_backrun = ConfigurationManager.AppSettings["is_backrun"];
			chbIsBackRun.Checked = _is_backrun == "1" ? true : false;
			chbAutoLogin.Checked = _is_auto_login == "1" ? true : false;

			txtPLMUrl.Text =	ConfigurationManager.AppSettings["plm_url"];
			txtDB.Text =		ConfigurationManager.AppSettings["plm_db" ];
			txtAD.Text =		ConfigurationManager.AppSettings["plm_ad" ];
			txtPWD.Text =		ConfigurationManager.AppSettings["plm_pwd"];
			txtSearchMethod.Text = ConfigurationManager.AppSettings["search_method"];
			
			string method_steps = ConfigurationManager.AppSettings["method_steps"];
			method_steps = method_steps.Replace(",", "\n");
			txtMethodStep.Text = method_steps;
		}
		
		/// <summary>
		/// 修改 app.config 中的設定
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		private void SaveAppConfig(string key,string value)
        {
			// 修改 app.config 中的設定
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			config.AppSettings.Settings[key].Value = value;
			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("appSettings");
		}
		private void btnConnSave_Click(object sender, EventArgs e)
		{

		}
		#endregion


		#region"編輯器顯示"
		//** 編輯區 **//
		private void InitColors()
        {

            TextArea.SetSelectionBackColor(true, IntToColor(0x114D9C));
			TextArea.CaretLineBackColor = Color.White;
			TextArea.CaretForeColor = Color.White;

		}

        private void InitHotkeys()
        {

            // register the hotkeys with the form
            HotKeyManager.AddHotKey(this, OpenSearch, Keys.F, true);
            HotKeyManager.AddHotKey(this, OpenFindDialog, Keys.F, true, false, true);
            HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.R, true);
            HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.H, true);
            HotKeyManager.AddHotKey(this, Uppercase, Keys.U, true);
            HotKeyManager.AddHotKey(this, Lowercase, Keys.L, true);
            HotKeyManager.AddHotKey(this, ZoomIn, Keys.Oemplus, true);
            HotKeyManager.AddHotKey(this, ZoomOut, Keys.OemMinus, true);
            HotKeyManager.AddHotKey(this, ZoomDefault, Keys.D0, true);
            HotKeyManager.AddHotKey(this, CloseSearch, Keys.Escape);

            // remove conflicting hotkeys from scintilla
            TextArea.ClearCmdKey(Keys.Control | Keys.F);
            TextArea.ClearCmdKey(Keys.Control | Keys.R);
            TextArea.ClearCmdKey(Keys.Control | Keys.H);
            TextArea.ClearCmdKey(Keys.Control | Keys.L);
            TextArea.ClearCmdKey(Keys.Control | Keys.U);

        }

        private void InitSyntaxColoring()
        {

            // Configure the default style
            TextArea.StyleResetDefault();
            TextArea.Styles[Style.Default].Font = "Consolas";
            TextArea.Styles[Style.Default].Size = 10;
            TextArea.Styles[Style.Default].BackColor = IntToColor(0x212121);
            TextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
            TextArea.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            TextArea.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
            TextArea.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0xBD758B);
            TextArea.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
            TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
            TextArea.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
            TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
            TextArea.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
            TextArea.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
            TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
            TextArea.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
            TextArea.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
            TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
            TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
            TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);

            TextArea.Lexer = Lexer.Cpp;

            TextArea.SetKeywords(0, "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield");
            TextArea.SetKeywords(1, "void Null ArgumentError arguments Array Boolean Class Date DefinitionError Error EvalError Function int Math Namespace Number Object RangeError ReferenceError RegExp SecurityError String SyntaxError TypeError uint XML XMLList Boolean Byte Char DateTime Decimal Double Int16 Int32 Int64 IntPtr SByte Single UInt16 UInt32 UInt64 UIntPtr Void Path File System Windows Forms ScintillaNET");

        }

        private void OnTextChanged(object sender, EventArgs e)
        {
			// 定義關鍵字列表
			string[] keywords = new string[] { "int", "string", "bool", "float", "double" };

			// 獲取當前輸入的文本
			string inputText = TextArea.Text;

			// 查找最後一個空格字符的位置
			int lastSpaceIndex = inputText.LastIndexOf(' ');

			// 如果最後一個空格字符存在，表示正在輸入新的變數名稱
			if (lastSpaceIndex >= 0)
			{
				// 獲取變數名稱
				string variableName = inputText.Substring(lastSpaceIndex + 1);

				// 查找與變數名稱相匹配的關鍵字
				var matchedKeywords = from keyword in keywords
									  where keyword.StartsWith(variableName)
									  select keyword;

				// 如果有匹配的關鍵字，顯示 IntelliSense 提示
				if (matchedKeywords.Count() > 0)
				{
					string hintText = string.Join(", ", matchedKeywords);
					toolTip1.SetToolTip(TextArea, hintText);
					
				}
				else
				{
					toolTip1.RemoveAll();
				}
			}
		}

		#region Numbers, Bookmarks, Code Folding

		/// <summary>
		/// the background color of the text area
		/// </summary>
		private const int BACK_COLOR = 0x2A211C;

		/// <summary>
		/// default text color of the text area
		/// </summary>
		private const int FORE_COLOR = 0xB7B7B7;

		/// <summary>
		/// change this to whatever margin you want the line numbers to show in
		/// </summary>
		private const int NUMBER_MARGIN = 1;

		/// <summary>
		/// change this to whatever margin you want the bookmarks/breakpoints to show in
		/// </summary>
		private const int BOOKMARK_MARGIN = 2;
		private const int BOOKMARK_MARKER = 2;

		/// <summary>
		/// change this to whatever margin you want the code folding tree (+/-) to show in
		/// </summary>
		private const int FOLDING_MARGIN = 3;

		/// <summary>
		/// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
		/// </summary>
		private const bool CODEFOLDING_CIRCULAR = true;

		private void InitNumberMargin()
		{

			TextArea.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
			TextArea.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
			TextArea.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
			TextArea.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);

			var nums = TextArea.Margins[NUMBER_MARGIN];
			nums.Width = 30;
			nums.Type = MarginType.Number;
			nums.Sensitive = true;
			nums.Mask = 0;

			TextArea.MarginClick += TextArea_MarginClick;
		}

		private void InitBookmarkMargin()
		{

			//TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

			var margin = TextArea.Margins[BOOKMARK_MARGIN];
			margin.Width = 20;
			margin.Sensitive = true;
			margin.Type = MarginType.Symbol;
			margin.Mask = (1 << BOOKMARK_MARKER);
			//margin.Cursor = MarginCursor.Arrow;

			var marker = TextArea.Markers[BOOKMARK_MARKER];
			marker.Symbol = MarkerSymbol.Circle;
			marker.SetBackColor(IntToColor(0xFF003B));
			marker.SetForeColor(IntToColor(0x000000));
			marker.SetAlpha(100);

		}

		private void InitCodeFolding()
		{

			TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));
			TextArea.SetFoldMarginHighlightColor(true, IntToColor(BACK_COLOR));

			// Enable code folding
			TextArea.SetProperty("fold", "1");
			TextArea.SetProperty("fold.compact", "1");

			// Configure a margin to display folding symbols
			TextArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
			TextArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
			TextArea.Margins[FOLDING_MARGIN].Sensitive = true;
			TextArea.Margins[FOLDING_MARGIN].Width = 20;

			// Set colors for all folding markers
			for (int i = 25; i <= 31; i++)
			{
				TextArea.Markers[i].SetForeColor(IntToColor(BACK_COLOR)); // styles for [+] and [-]
				TextArea.Markers[i].SetBackColor(IntToColor(FORE_COLOR)); // styles for [+] and [-]
			}

			// Configure folding markers with respective symbols
			TextArea.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
			TextArea.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
			TextArea.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
			TextArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
			TextArea.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
			TextArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
			TextArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

			// Enable automatic folding
			TextArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

		}

		private void TextArea_MarginClick(object sender, MarginClickEventArgs e)
		{
			if (e.Margin == BOOKMARK_MARGIN)
			{
				// Do we have a marker for this line?
				const uint mask = (1 << BOOKMARK_MARKER);
				var line = TextArea.Lines[TextArea.LineFromPosition(e.Position)];
				if ((line.MarkerGet() & mask) > 0)
				{
					// Remove existing bookmark
					line.MarkerDelete(BOOKMARK_MARKER);
				}
				else
				{
					// Add bookmark
					line.MarkerAdd(BOOKMARK_MARKER);
				}
			}
		}

		#endregion

		#region Drag & Drop File

		public void InitDragDropFile()
		{

			TextArea.AllowDrop = true;
			TextArea.DragEnter += delegate (object sender, DragEventArgs e) {
				if (e.Data.GetDataPresent(DataFormats.FileDrop))
					e.Effect = DragDropEffects.Copy;
				else
					e.Effect = DragDropEffects.None;
			};
			TextArea.DragDrop += delegate (object sender, DragEventArgs e) {

				// get file drop
				if (e.Data.GetDataPresent(DataFormats.FileDrop))
				{

					Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
					if (a != null)
					{

						string path = a.GetValue(0).ToString();

						LoadDataFromFile(path);

					}
				}
			};

		}

		private void LoadDataFromFile(string path)
		{
			if (File.Exists(path))
			{
				//FileName.Text = Path.GetFileName(path);
				TextArea.Text = File.ReadAllText(path);
			}
		}

		#endregion

		#region Main Menu Commands

		private void findToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenSearch();
		}

		private void findDialogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFindDialog();
		}

		private void findAndReplaceToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenReplaceDialog();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TextArea.Cut();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TextArea.Copy();
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TextArea.Paste();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TextArea.SelectAll();
		}

		private void selectLineToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Line line = TextArea.Lines[TextArea.CurrentLine];
			TextArea.SetSelection(line.Position + line.Length, line.Position);
		}

		private void clearSelectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TextArea.SetEmptySelection(0);
		}

		private void indentSelectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Indent();
		}

		private void outdentSelectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Outdent();
		}

		private void uppercaseSelectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Uppercase();
		}

		private void lowercaseSelectionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Lowercase();
		}
		private void wordWrapToolStripMenuItem1_Click(object sender, EventArgs e)
		{

			// toggle word wrap
			wordWrapItem.Checked = !wordWrapItem.Checked;
			TextArea.WrapMode = wordWrapItem.Checked ? WrapMode.Word : WrapMode.None;
		}

		private void indentGuidesToolStripMenuItem_Click(object sender, EventArgs e)
		{

			// toggle indent guides
			indentGuidesItem.Checked = !indentGuidesItem.Checked;
			TextArea.IndentationGuides = indentGuidesItem.Checked ? IndentView.LookBoth : IndentView.None;
		}

		private void hiddenCharactersToolStripMenuItem_Click(object sender, EventArgs e)
		{

			// toggle view whitespace
			hiddenCharactersItem.Checked = !hiddenCharactersItem.Checked;
			TextArea.ViewWhitespace = hiddenCharactersItem.Checked ? WhitespaceMode.VisibleAlways : WhitespaceMode.Invisible;
		}
		private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ZoomIn();
		}

		private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ZoomOut();
		}

		private void zoom100ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ZoomDefault();
		}

		private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TextArea.FoldAll(FoldAction.Contract);
		}

		private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TextArea.FoldAll(FoldAction.Expand);
		}


		#endregion

		#region Uppercase / Lowercase

		private void Lowercase()
		{

			// save the selection
			int start = TextArea.SelectionStart;
			int end = TextArea.SelectionEnd;

			// modify the selected text
			TextArea.ReplaceSelection(TextArea.GetTextRange(start, end - start).ToLower());

			// preserve the original selection
			TextArea.SetSelection(start, end);
		}

		private void Uppercase()
		{

			// save the selection
			int start = TextArea.SelectionStart;
			int end = TextArea.SelectionEnd;

			// modify the selected text
			TextArea.ReplaceSelection(TextArea.GetTextRange(start, end - start).ToUpper());

			// preserve the original selection
			TextArea.SetSelection(start, end);
		}

		#endregion

		#region Indent / Outdent

		private void Indent()
		{
			// we use this hack to send "Shift+Tab" to scintilla, since there is no known API to indent,
			// although the indentation function exists. Pressing TAB with the editor focused confirms this.
			GenerateKeystrokes("{TAB}");
		}

		private void Outdent()
		{
			// we use this hack to send "Shift+Tab" to scintilla, since there is no known API to outdent,
			// although the indentation function exists. Pressing Shift+Tab with the editor focused confirms this.
			GenerateKeystrokes("+{TAB}");
		}

		private void GenerateKeystrokes(string keys)
		{
			HotKeyManager.Enable = false;
			TextArea.Focus();
			SendKeys.Send(keys);
			HotKeyManager.Enable = true;
		}

		#endregion

		#region Zoom

		private void ZoomIn()
		{
			TextArea.ZoomIn();
		}

		private void ZoomOut()
		{
			TextArea.ZoomOut();
		}

		private void ZoomDefault()
		{
			TextArea.Zoom = 0;
		}


		#endregion

		#region Quick Search Bar

		bool SearchIsOpen = false;

		private void OpenSearch()
		{

			SearchManager.SearchBox = TxtSearch;
			SearchManager.TextArea = TextArea;

			if (!SearchIsOpen)
			{
				SearchIsOpen = true;
				InvokeIfNeeded(delegate () {
					PanelSearch.Visible = true;
					TxtSearch.Text = SearchManager.LastSearch;
					TxtSearch.Focus();
					TxtSearch.SelectAll();
				});
			}
			else
			{
				InvokeIfNeeded(delegate () {
					TxtSearch.Focus();
					TxtSearch.SelectAll();
				});
			}
		}
		private void CloseSearch()
		{
			if (SearchIsOpen)
			{
				SearchIsOpen = false;
				InvokeIfNeeded(delegate () {
					PanelSearch.Visible = false;
					//CurBrowser.GetBrowser().StopFinding(true);
				});
			}
		}

		private void BtnClearSearch_Click(object sender, EventArgs e)
		{
			CloseSearch();
		}

		private void BtnPrevSearch_Click(object sender, EventArgs e)
		{
			SearchManager.Find(false, false);
		}
		private void BtnNextSearch_Click(object sender, EventArgs e)
		{
			SearchManager.Find(true, false);
		}
		private void TxtSearch_TextChanged(object sender, EventArgs e)
		{
			SearchManager.Find(true, true);
		}

		private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
		{
			if (HotKeyManager.IsHotkey(e, Keys.Enter))
			{
				SearchManager.Find(true, false);
			}
			if (HotKeyManager.IsHotkey(e, Keys.Enter, true) || HotKeyManager.IsHotkey(e, Keys.Enter, false, true))
			{
				SearchManager.Find(false, false);
			}
		}

        #endregion

        #region Find & Replace Dialog

        private void OpenFindDialog()
		{

		}
		private void OpenReplaceDialog()
		{


		}

		#endregion

		#region Utils

		public static Color IntToColor(int rgb)
		{
			return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
		}

		public void InvokeIfNeeded(Action action)
		{
			if (this.InvokeRequired)
			{
				this.BeginInvoke(action);
			}
			else
			{
				action.Invoke();
			}
		}














        #endregion

        #endregion

        
    }
}
