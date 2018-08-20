using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Youtube
{
    // https://github.com/cefsharp/CefSharp/blob/master/CefSharp.OffScreen.Example/Program.cs#L127

    public partial class Form1 : Form
    {
        private const string START_PAGE_URL = "http://www.youtube.com/";
        private const string YOUTUBE_LOGIN_URL = "https://accounts.google.com/signin/v2/sl/pwd?continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Fnext%3D%252F%26hl%3Dko%26feature%3Dsign_in_button%26app%3Ddesktop%26action_handle_signin%3Dtrue&hl=ko&passive=true&service=youtube&uilel=3&flowName=GlifWebSignIn&flowEntry=ServiceLogin";
                                                        
        ChromiumWebBrowser browser;
        List<AuthVO> authVOs = null;
                
        Dictionary<string, object> programConfig = new Dictionary<string, object>();
        Dictionary<string, object> loveConfig = new Dictionary<string, object>();
        
        public Form1()
        {
            InitializeComponent();
        }

        private async Task ChangeProxServerAsync(string ip)
        {
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var rc = this.browser.GetBrowser().GetHost().RequestContext;
                var v = new Dictionary<string, object>();
                v["mode"] = "fixed_servers";
                v["server"] = ip;
                string error;
                bool success = rc.SetPreference("proxy", v, out error);                                
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {         
            // 공통 설정파일 로드
            AuthFileInitialize("./config/auth.txt");            

            // 브라우저 로드
            BrowserInitalize();
        }

      
        private void BrowserInitalize()
        {
            CefSettings cefSettings = new CefSettings();
            cefSettings.Locale = "ko-KR";
            cefSettings.AcceptLanguageList = "ko-KR";
            Cef.Initialize(cefSettings);
            browser = new ChromiumWebBrowser(START_PAGE_URL);
            panel1.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;            
            MainAsync();
        }



        private async void MainAsync()
        {
            foreach (AuthVO authVO in authVOs)
            {
                //uninstallButtonColumn.Name = "uninstall_column" + index;
                object[] row = { authVO.username, authVO.ip, authVO.memo};
                //dataGridView1.Columns.Insert(2, uninstallButtonColumn);
                dataGridView1.Rows.Add(row);
            }            
        }

        private async Task LoginYoutubeAsync(string username, string password)
        {
            browser.Focus();

            await EvaluateScriptAsync("document.querySelector('#text').click()");
            await WaitForPageLoadingAsync();
            await EvaluateScriptAsync("document.querySelector('#identifierId').focus()");
            await EvaluateScriptAsync(String.Format("document.querySelector('#identifierId').value = '{0}'", username.Trim()));
            await EvaluateScriptAsync("document.querySelector('#identifierNext').click()");
            await WaitForCheckScript("document.querySelector('input[type=password]') !== null", 100);
            await EvaluateScriptAsync("document.querySelector('input[type=password]').focus()");
            await WaitForCheckScript("document.querySelector('#password.u3bW4e') !== null", 100);            
            await EvaluateScriptAsync(String.Format("document.querySelector('input[type=password]').value = '{0}'", password.Trim()));            
        }
        
        private async Task TestModeProcessAsync()
        {
            await LoadPageAsync(browser, START_PAGE_URL);
            
            await EvaluateScriptAsync("$('#text').click()");
        }
     
       
        private async Task RandomWaitAsync(int minValue, int maxValue)
        {
            Random r = new Random();
            int randomValue = r.Next(minValue, maxValue);
            await Task.Delay( randomValue);
        }

        // 쿠키 삭제 메소드
        private async Task DeleteCookieAsync()
        {
            Cef.GetGlobalCookieManager().DeleteCookies("", "");
        }       

        private async Task ScrollElementBy(string id ,int height, int interval, int count)
        {
            int p = 0;
            for (int i = 0; i < count; i++)
            {
                p += height;
                await EvaluateScriptAsync("document.getElementById('"+ id +"').scrollTop =" + p);
                await Task.Delay(interval);
            }
        }

        private async Task WaitForCheckScript(string script, int delay)
        {
            while (true)
            {
                JavascriptResponse x = await EvaluateScriptAsync( script);

                Console.WriteLine((Boolean)getResult(x));
                
                if ((Boolean)getResult(x))
                {
                    break;
                }
                else
                {
                    await Task.Delay(delay);
                }
            }

        }

        private async Task MoveHome() {
            await EvaluateScriptAsync("document.querySelector('#logo-icon-container').click();");
            await WaitForPageLoadingAsync();
        }
        
        private async Task SendKeyToBrowserAsync(int keyCode)
        {
            KeyEvent k = new KeyEvent();
            k.WindowsKeyCode = keyCode;
            k.FocusOnEditableField = true;
            k.IsSystemKey = false;
            k.Type = KeyEventType.Char;
            browser.GetBrowser().GetHost().SendKeyEvent(k);

            await Task.Delay(100);
        }

        private object getResult(JavascriptResponse x)
        {
            return x.Success ? (x.Result ?? "null") : x.Message;
        }

        private async Task LogOutYoutubeAsync()
        {
            await EvaluateScriptAsync("document.querySelector('#avatar-btn').click()");
            await EvaluateScriptAsync("document.querySelector('a[href=\"/logout\"]').click()");
            await WaitForPageLoadingAsync();
        }

        private async Task WaitForPageLoadingAsync()
        {
            while (((IWebBrowser)browser).IsLoading)
            {                                
                await Task.Delay(10);
            }
        }

        private async Task<JavascriptResponse> EvaluateScriptAsync( string script)
        {
           Console.WriteLine(script);
           JavascriptResponse x = await browser.EvaluateScriptAsync(script);            
           await Task.Delay(100);
           return x;
        }


        public Task LoadPageAsync(IWebBrowser browser, string address = null)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler<LoadingStateChangedEventArgs> handler = null;
            handler = (sender, args) =>
            {
                if (!args.IsLoading)
                {
                    browser.LoadingStateChanged -= handler;
                    tcs.TrySetResult(true);
                }
            };

            browser.LoadingStateChanged += handler;            

            if (!string.IsNullOrEmpty(address))
            {
                browser.Load(address);                
            }
            
            return tcs.Task;
        }

        private async Task LoadJqueryAsync()
        {
            await EvaluateScriptAsync("var element1 = document.createElement('script');element1.src = '//ajax.googleapis.com/ajax/libs/jquery/2.1.1/jquery.min.js';element1.type='text/javascript';document.getElementsByTagName('head')[0].appendChild(element1);");
        }

        private void ExitMenuItemClick(object sender, EventArgs e)
        {
            browser.Dispose();
            Cef.Shutdown();
            Close();
        }

        private void AuthFileInitialize(string authFilePath)
        {
            string authLine, commentLine, urlLine;
            int counter = 0;

            StreamReader authFile = new StreamReader(authFilePath, Encoding.Default, true);
                        
            List<AuthVO> configVOs = new List<AuthVO>();

            while ((authLine = authFile.ReadLine()) != null && !authLine.Equals(""))                
            {
                AuthVO configVO = new AuthVO();
                string[] s = authLine.Split('/');
                                                
                configVO.username = s[0];
                configVO.password = s[1];
                configVO.ip = s[2];
                configVO.memo = s[3];
                configVOs.Add(configVO);

                counter++;
            }

            authFile.Close();                        

            this.authVOs = configVOs;
        }

        private async void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            label5.BackColor = Color.DarkOrange;
            label5.Text = "Ready";

            AuthVO authVO = authVOs[dataGridView1.CurrentCell.RowIndex];

            // 색갈
            DataGridViewRowCollection rows = dataGridView1.Rows;
            for (int i = 0; i < rows.Count; i++)
            {
                dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Empty;
            }
            dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Beige;

            // 아이디 표시
            label3.Text = authVO.username;

            // 아이피 표시
            label4.Text = authVO.ip;

            // 쿠키삭제
            await DeleteCookieAsync();

            // 아이피 변경
            await ChangeProxServerAsync(authVO.ip);

            // 네이버로 이동
            await LoadPageAsync(browser, YOUTUBE_LOGIN_URL);

            await LoginYoutubeAsync(authVO.username, authVO.password);           
            
            label5.BackColor = Color.FromArgb(0, 216, 255);
            label5.Text = "Complate";

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await LoadPageAsync(browser, textBox1.Text);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click_1(object sender, EventArgs e)
        {

        }
    }
}
