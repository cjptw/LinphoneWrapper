using System;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms;
namespace WindowsFormsLinphoneV
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 注册ID
        /// </summary>
        public string RegisterId { get; set; } = Guid.NewGuid().ToString();
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //LinphoneSdk.InitSdk();
            //注册状态变更动作
            Action<object> actionRegistrationStateChanged = (result)=>{
                dynamic data = (dynamic)result;
                this.Invoke(new MethodInvoker(() =>
                {
                    richTextBoxLinPhoneState.AppendText(data.RegistrationState.ToString() + "：" + data.Message + "\n");
                }));
            }; 
            LinphoneSdk.ActionOnRegistrationStateChanged(actionRegistrationStateChanged);
            //呼叫状态变更动作
            Action<object> actionOnCallStateChanged = (result) => {
                dynamic data = (dynamic)result;
                this.Invoke(new MethodInvoker(() =>
                {
                    richTextBoxLinPhoneState.AppendText(data.CallState.ToString() + "：" + data.Message + "\n");
                }));
            }; ;
            LinphoneSdk.ActionOnCallStateChanged(actionOnCallStateChanged);
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            string[] hostInfos = HostInfos;
            LinphoneSdk.Register(RegisterId, hostInfos[0], hostInfos[1], hostInfos[2]);
        }

        /// <summary>
        /// 挂断
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            LinphoneSdk.TerminateAllCalls();

        }
      
        private void button4_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 视频呼叫
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            string[] info = HostInfos;
            LinphoneSdk.CallPhone(tbPhone.Text, info[0], info[2], true);
        }
       /// <summary>
       /// 配置信息
       /// </summary>
        public string[] HostInfos
        {
            get
            {
                string phone = tbPhone.Text;
                string hostInfo = textBoxLoginInfo.Text;
                if (string.IsNullOrEmpty(hostInfo))
                {
                    MessageBox.Show("未配置注册信息，请填写：CSserverIP,userName,passWord,sipPort");
                    return null;
                }
                if (string.IsNullOrEmpty(phone))
                {
                    MessageBox.Show("未配置拨打电话号码");
                    return null;
                }
                string[] infos = hostInfo.Split(',');
                return infos;
            }
        }
        /// <summary>
        /// 获取呼叫状态
        /// </summary>
        public System.Timers.Timer timerCallState;
        /// <summary>
        /// 拨打电话
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCallPhone_Click(object sender, EventArgs e)
        {
            string[] info = HostInfos;
            LinphoneSdk.CallPhone(tbPhone.Text, info[0], info[2],false);
        }
        /// <summary>
        /// 注销
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUnregistration_Click(object sender, EventArgs e)
        {
            LinphoneSdk.Unregistration(RegisterId);
        }
    }
}
