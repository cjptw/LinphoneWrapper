using Linphone;
using LinPhoneSDK;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
namespace LinPhoneSDK
//注册
{
    public static class Class1
{
        static Core core; static Factory factory; static Call call;
        static System.Timers.Timer t;
        [DllExport("Set_Login_Info", CallingConvention = CallingConvention.Cdecl)]
        public static void Set_Login_Info(
    [MarshalAs(UnmanagedType.LPStr)] string CSserverIP,
    [MarshalAs(UnmanagedType.LPStr)] string localIP,
    [MarshalAs(UnmanagedType.LPStr)] string userName,
    [MarshalAs(UnmanagedType.LPStr)] string passWord,
    [MarshalAs(UnmanagedType.LPStr)] string sipPort
    )
        {

            Reg(factory, core, CSserverIP + ":" + sipPort, userName, passWord);
        }


        /// <summary>
        /// CSharp_Call-StartP2PCall// 呼叫
        /// </summary>
        /// <param name="Sip"></param>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        [DllExport("SartP2PCall", CallingConvention = CallingConvention.Cdecl)]
        public static void SartP2PCall(
            [MarshalAs(UnmanagedType.LPStr)] string Sip,
     [MarshalAs(UnmanagedType.LPStr)] string IP,
    [MarshalAs(UnmanagedType.LPStr)] string Port
            )
        {
            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = false;
            call = core.InviteAddressWithParams(core.InterpretUrl($"sip:{Sip}@{IP}:{Port}"), callParams);
        }
        //组呼
        [DllExport("SartGrpCall", CallingConvention = CallingConvention.Cdecl)]
        public static void SartGrpCall(
            [MarshalAs(UnmanagedType.LPStr)] string Sip1,
     [MarshalAs(UnmanagedType.LPStr)] string IP1,
    [MarshalAs(UnmanagedType.LPStr)] string Port1
            )
        {
            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = false;
            call = core.InviteAddressWithParams(core.InterpretUrl($"sip:{Sip1}@{IP1}:{Port1}"), callParams);
        }
        //紧急组呼SartEmeGrpCall
        [DllExport("SartEmeGrpCall", CallingConvention = CallingConvention.Cdecl)]
        public static void SartEmeGrpCall(
            [MarshalAs(UnmanagedType.LPStr)] string Sip2,
     [MarshalAs(UnmanagedType.LPStr)] string IP2,
    [MarshalAs(UnmanagedType.LPStr)] string Port2
            )
        {
            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = false;
            call = core.InviteAddressWithParams(core.InterpretUrl($"sip:{Sip2}@{IP2}:{Port2}"), callParams);
        }
        //外部呼叫Call_T
        [DllExport("Call_T", CallingConvention = CallingConvention.Cdecl)]
        public static void SCall_T(
            [MarshalAs(UnmanagedType.LPStr)] string Sip3,
     [MarshalAs(UnmanagedType.LPStr)] string IP3,
    [MarshalAs(UnmanagedType.LPStr)] string Port3
            )
        {
            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = false;
            call = core.InviteAddressWithParams(core.InterpretUrl($"sip:{Sip3}@{IP3}:{Port3}"), callParams);
        }

        //  强插
        [DllExport("P2pBrkin", CallingConvention = CallingConvention.Cdecl)]
        public static void P2pBrkin(
            [MarshalAs(UnmanagedType.LPStr)] string Sip4,
     [MarshalAs(UnmanagedType.LPStr)] string IP4,
    [MarshalAs(UnmanagedType.LPStr)] string Port4
            )
        {
            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = false;
            call = core.InviteAddressWithParams(core.InterpretUrl($"sip:'*91'+{Sip4}@{IP4}:{Port4}"), callParams);
        }
        //  监听
    //    [DllExport("GrpBreakOff", CallingConvention = CallingConvention.Cdecl)]
    //    public static void GrpBreakOff(
    //        [MarshalAs(UnmanagedType.LPStr)] string Sip6,
    // [MarshalAs(UnmanagedType.LPStr)] string IP6,
    //[MarshalAs(UnmanagedType.LPStr)] string Port6
    //        )
    //    {
    //        call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
    //        CallParams callParams = core.CreateCallParams(null);
    //        callParams.VideoEnabled = false;
    //        call = core.InviteAddressWithParams(core.InterpretUrl($"sip:'*91'+{Sip6}@{IP6}:{Port6}"), callParams);
    //    }


        /// <summary>
        /// CSharp_VideoCall-StP2PVideoCall  视频呼叫
        /// </summary>
        /// <param name="Sip"></param>
        /// <param name="IP"></param>
        /// <param name="Port"></param>
        /// <param name="Headle"></param>
        [DllExport("StP2PVideoCall", CallingConvention = CallingConvention.Cdecl)]
        public static void StP2PVideoCall(
    [MarshalAs(UnmanagedType.LPStr)] string Sip,
[MarshalAs(UnmanagedType.LPStr)] string IP,
[MarshalAs(UnmanagedType.LPStr)] string Port,
[MarshalAs(UnmanagedType.SysUInt)] IntPtr Headle
    )
        {
            core.NativeVideoWindowId = Headle;
            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
            CallParams callParams = core.CreateCallParams(null);
            callParams.VideoEnabled = true;
            call = core.InviteAddressWithParams(core.InterpretUrl($"sip:{Sip}@{IP}:{Port}"), callParams);




        }
        /// <summary>
        /// CSharp_TerminateAllCalls-SartP2PHangup 挂断
        /// </summary>
        [DllExport("SartP2PHangup", CallingConvention = CallingConvention.Cdecl)]
        public static void StartP2PHangup()
        {
            core.TerminateAllCalls();
        }
        //组呼挂断
        [DllExport("GrpHangup", CallingConvention = CallingConvention.Cdecl)]
        public static void GrpHangup(
            [MarshalAs(UnmanagedType.LPStr)] string PTT_HANGUP,
            [MarshalAs(UnmanagedType.LPStr)] string GrpId
            )
        {
            core.TerminateAllCalls();
        }
        //点呼挂断
        [DllExport("SartP3PReject", CallingConvention = CallingConvention.Cdecl)]
        public static void SartP3PReject(
            [MarshalAs(UnmanagedType.LPStr)] string OpCode,
            [MarshalAs(UnmanagedType.LPStr)] string mobileid
            )
        {
            core.TerminateAllCalls();
        }

        /// <summary>
        /// CSharp_Accept-StartP2PRecv 接听
        /// </summary>
        [DllExport("SartP2PRecv", CallingConvention = CallingConvention.Cdecl)]
        public static void SartP2PRecv()
        {
            call = core.CurrentCall;
            if (call != null)
            {
                call.Accept();
            }

        }

        [DllExport("SDK_START", CallingConvention = CallingConvention.Cdecl)]
        public static int SDK_START()
        {
            //日志开启
            // LoggingService LoggingService = LoggingService.Instance;
            // LoggingService.LogLevel = LogLevel.Debug;
            //LoggingService.Listener.OnLogMessageWritten = OnLog;

            factory = Factory.Instance;

            //C:\Users\lilin\AppData\Local\linphone 如果装过linphone win exe的请删除这里面的内容否则core 创建不成功
            //多线程core需要加线程锁
            core = factory.CreateCore(new CoreListener() { }, "linphonerc", "linphonerc-factory");

            core.AvpfMode = AVPFMode.Disabled;
            Core.EnableLogCollection(LogCollectionState.Disabled);
            core.Listener.OnRegistrationStateChanged = OnRegistration;
            core.Listener.OnCallStateChanged = OnCallState;
            core.Listener.OnMessageReceived = OnMessage;
            core.Transports.UdpPort = -1;

            string x = "";
            foreach (string s in core.VideoDevicesList)
            {
                Console.WriteLine("set=" + s);
                if (s.IndexOf("Camera") != -1)
                {// bctbx-error-bctbx_file_open: Error open Permission denied
                    x = s;
                }
            }
            if (x != "")
            {
                Console.WriteLine("set last=" + x);
                core.VideoDevice = x;//"Directshow capture: Integrated Camera"; //摄像头设置
            }
            Console.WriteLine(">>" + core.DefaultInputAudioDevice.DeviceName);
            Console.WriteLine(">>" + core.DefaultOutputAudioDevice.DeviceName);

            foreach (AudioDevice s in core.ExtendedAudioDevices)
            {
                Console.WriteLine(s.DeviceName + ">++>");
            }
            core.SetUserAgent("mylinphone", "1.0");
            Console.WriteLine(">>");
            // core.InputAudioDevice
            //core.OutputAudioDevice

            // core.NativeVideoWindowId = pictureBox1.Handle;//video window set

            foreach (PayloadType p in core.AudioPayloadTypes)
            {
                if (p.MimeType == "opus") { p.Enable(false); }//
                Console.Write(p.MimeType + "(" + p.Enabled() + ")" + "=");
            }
            Console.WriteLine();
            foreach (PayloadType p in core.VideoPayloadTypes)
            {
                if (p.MimeType == "VP8") { p.Enable(false); }//
                Console.Write(p.MimeType + "(" + p.Enabled() + ")" + "=");
            }
            Console.WriteLine();


            t = new System.Timers.Timer(20);   //实例化Timer类，设置间隔时间为10000毫秒；   
            t.Elapsed += new System.Timers.ElapsedEventHandler(theout); //到达时间的时候执行事件；   
            t.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
            t.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；   

            return 0; // 假设成功启动，返回0  
        }
        [DllExport("SDK_STOP", CallingConvention = CallingConvention.Cdecl)]
        public static int SDK_STOP()
        {
            // 在这里实现SDK停止的逻辑  
            // 返回停止状态或错误代码（通常是非零值表示错误）  

            // 假设成功停止，返回0  
            //Call.Stop();
            //call.Stop();
            core.Stop();

            return 0;
        }

        [DllExport("CSharp_proviAll", CallingConvention = CallingConvention.Cdecl)]
        public static string CSharp_proviAll(int jarg1, long jarg2)
        {
            return "";
        }
        // 导出CSharp_getPgrpList函数  
        [DllExport("CSharp_getPgrpList", CallingConvention = CallingConvention.Cdecl)]
        public static string CSharp_getPgrpList(long dcid)
        {
            // 在这里实现获取程序组列表的逻辑  
            // 根据传入的dcid参数获取相应的程序组列表，并转换为字符串返回  

            // 示例：仅返回硬编码的字符串，实际情况下你应该实现具体的逻辑  
            return "示例程序组列表";
        }

        static private void Reg(Factory factory, Core core, String domain, String username, String password)
        {

            foreach (AuthInfo a in core.AuthInfoList)
            {
                core.RemoveAuthInfo(a);
            }
            foreach (ProxyConfig a in core.ProxyConfigList)
            {
                core.RemoveProxyConfig(a);
            }

            AuthInfo authInfo = factory.CreateAuthInfo(username, username, password, null, null, domain);
            core.AddAuthInfo(authInfo);

            /*
            NatPolicy nat= core.CreateNatPolicy();
            nat.IceEnabled = true;
            nat.TurnEnabled = true;
            nat.StunServer = "118.190.151.162:3478";
            string turnusername = "lilin",turnpwd= "lilinaini";

            AuthInfo xauthInfo = core.FindAuthInfo(null, turnusername, null);
            if (xauthInfo != null)
            { 
                AuthInfo cloneAuthInfo = xauthInfo.Clone();
                core.RemoveAuthInfo(xauthInfo);   
                cloneAuthInfo.Username = turnusername;
                cloneAuthInfo.Userid = turnusername;
                cloneAuthInfo.Passwd=turnpwd;
                core.AddAuthInfo(cloneAuthInfo);
            }
            else
            {
                AuthInfo cloneAuthInfo = core.CreateAuthInfo(turnusername, turnusername, turnpwd, null, null, null);
                core.AddAuthInfo(cloneAuthInfo);
            }
            //
            nat.StunServerUsername = turnusername;
            core.NatPolicy = nat;   */

            var proxyConfig = core.CreateProxyConfig();
            String sipurl = "sip:" + username + "@" + domain;
            var identity = factory.CreateAddress(sipurl);
            // identity.Username = username;
            //identity.Domain = domain;
            //identity.Transport = TransportType.Udp;
            proxyConfig.Edit();
            proxyConfig.IdentityAddress = identity;
            proxyConfig.ServerAddr = sipurl;
            //  proxyConfig.Route = domain;

            proxyConfig.RegisterEnabled = true;
            proxyConfig.PublishEnabled = true;
            proxyConfig.AvpfMode = AVPFMode.Disabled;
            proxyConfig.Done();
            core.AddProxyConfig(proxyConfig);
            core.DefaultProxyConfig = proxyConfig;

            //core.RefreshRegisters();

        }
        static private void OnCallState(Core lc, Call call, CallState cstate, string message)
        {

            //Console.WriteLine("OnCallState=" + message);
        }

        static private void OnRegistration(Core lc, ProxyConfig cfg, RegistrationState cstate, string message)
        {
            //Console.WriteLine("OnRegistration" + message);
            ////label1.Text = "12";
        }
        static private void OnMessage(Core lc, ChatRoom room, ChatMessage message)
        {
            //   Console.WriteLine("收到消息>>" + message.TextContent); ;
        }
        static private void theout(object sender, ElapsedEventArgs e)
        {

            core.Iterate();

        }
        //----------------------------------------------------------------------------------

        /// <summary>
        /// 增加群组
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        [DllExport("DgnaGrp", CallingConvention = CallingConvention.Cdecl)]
        public static string DgnaGrp([MarshalAs(UnmanagedType.LPStr)] string number, [MarshalAs(UnmanagedType.LPStr)] string url)
        {
            using (var httpClient = new HttpClient())
            {
                string urls = url + "/admintor/api/pbx/page/create";
                var request = new HttpRequestMessage(HttpMethod.Post, urls);
                //  string data = "{\"api_str\": \"bgapi originate {'origination_caller_id_number=300,orgination_caller_id_name=api_call'}loopback/19190012028/国内+市话/xml 300 xml Local-Extensions\"}";
                var data = "{\"number\":\"+number+\",\"name\":\"A区广播\",\"flags\":\"mute\",\"call_timeout\": 30,\"record_format\": \"false\",\"rtmp_server\": \"\",\"enable_file_write_buffering\": false,\"record_stereo\": true,\"media_bug_answer_req\": true,\"record_min_sec\": 1,\"member_ids\":[1,2,3]}"; // 替换为你要发送的数据
                                                                                                                                                                                                                                                                                                                // 添加参数
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                string token = getToken(url);
                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 输出响应内容
                string responseBody = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
                return obj.ToString();
            }



        }
        /// <summary>
        /// uuid查询
        /// </summary>
        /// <returns></returns>

        [DllExport("query", CallingConvention = CallingConvention.Cdecl)]
        public static string query([MarshalAs(UnmanagedType.LPStr)] string page_number, [MarshalAs(UnmanagedType.LPStr)] string page_size, [MarshalAs(UnmanagedType.LPStr)] string cid_num, [MarshalAs(UnmanagedType.LPStr)] string url)
        {
            string urls = url + "/admintor/api/status/channel/query";

            using (var httpClient = new HttpClient())
            {
                // 设置请求的URI


                // 创建查询参数
                var query = ParseQueryString(string.Empty);
                query["page_number"] = page_number.ToString();
                query["page_size"] = page_size.ToString();
                query["cid_num"] = cid_num;

                // 添加查询参数到URI
                var fullUri = $"{urls}?{query}";

                // 创建HttpRequestMessage
                var request = new HttpRequestMessage(HttpMethod.Get, fullUri);

                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken(url));


                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 确保HTTP成功状态值
                response.EnsureSuccessStatusCode();

                // 读取响应内容（根据需要处理）
                var content = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(content);
                return obj.ToString();


            }

        }
        //强拆
        [DllExport("P2pBrkOff", CallingConvention = CallingConvention.Cdecl)]
        public static string P2pBrkOff([MarshalAs(UnmanagedType.LPStr)] string page_number, [MarshalAs(UnmanagedType.LPStr)] string page_size, [MarshalAs(UnmanagedType.LPStr)] string cid_num, [MarshalAs(UnmanagedType.LPStr)] string url)
        {
            string urls = url + "/admintor/api/status/channel/query";

            using (var httpClient = new HttpClient())
            {
                // 设置请求的URI


                // 创建查询参数
                var query = ParseQueryString(string.Empty);
                query["page_number"] = page_number.ToString();
                query["page_size"] = page_size.ToString();
                query["cid_num"] = cid_num;

                // 添加查询参数到URI
                var fullUri = $"{urls}?{query}";

                // 创建HttpRequestMessage
                var request = new HttpRequestMessage(HttpMethod.Get, fullUri);

                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken(url));


                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 确保HTTP成功状态值
                response.EnsureSuccessStatusCode();

                // 读取响应内容（根据需要处理）
                var content = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(content);
                return obj.ToString();
            }

        }
        //组呼强拆
        [DllExport("GrpBreakOff", CallingConvention = CallingConvention.Cdecl)]
        public static string GrpBreakOff([MarshalAs(UnmanagedType.LPStr)] string page_number, [MarshalAs(UnmanagedType.LPStr)] string page_size, [MarshalAs(UnmanagedType.LPStr)] string cid_num, [MarshalAs(UnmanagedType.LPStr)] string url)
        {
            string urls = url + "/admintor/api/status/channel/query";

            using (var httpClient = new HttpClient())
            {
                // 设置请求的URI

                // 创建查询参数
                var query = ParseQueryString(string.Empty);
                query["page_number"] = page_number.ToString();
                query["page_size"] = page_size.ToString();
                query["cid_num"] = cid_num;

                // 添加查询参数到URI
                var fullUri = $"{urls}?{query}";

                // 创建HttpRequestMessage
                var request = new HttpRequestMessage(HttpMethod.Get, fullUri);

                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken(url));


                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 确保HTTP成功状态值
                response.EnsureSuccessStatusCode();

                // 读取响应内容（根据需要处理）
                var content = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(content);
                return obj.ToString();
            }

        }



        /// <summary>
        /// 群组
        /// </summary>
        /// <returns></returns>
        [DllExport("fsapi", CallingConvention = CallingConvention.Cdecl)]
        public static string fsapi([MarshalAs(UnmanagedType.LPStr)] string api_str, [MarshalAs(UnmanagedType.LPStr)] string url)
        {
            using (var httpClient = new HttpClient())
            {
                string urls = url + "/admintor/api/utils/fsapi";
                var request = new HttpRequestMessage(HttpMethod.Post, urls);
                //  string data = "{\"api_str\": \"bgapi originate {'origination_caller_id_number=300,orgination_caller_id_name=api_call'}loopback/19190012028/国内+市话/xml 300 xml Local-Extensions\"}";
                //string data = "{\"api_str\":\"api_str\"}";
                string data = "{\"api_str\":\"" + api_str + "\"}";
                //string data = "{\"api_str\":\"sofia status profile internal reg\"}";
                // 添加参数
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                // string token = getToken();
                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken(url));

                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 输出响应内容
                string responseBody = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
                return obj.ToString();
            }

        }

        /// <summary>
        ///  群组删除
        /// </summary>
        /// <returns></returns>
        [DllExport("DgnaCacel", CallingConvention = CallingConvention.Cdecl)]
        public static string DgnaCacel([MarshalAs(UnmanagedType.LPArray)] int[] ids, [MarshalAs(UnmanagedType.LPStr)] string url)
        {
            using (var httpClient = new HttpClient())
            {
                // var str=ids.ToString();
                string id = string.Empty;
                if (ids.Count() > 0)
                {
                    id = "[" + String.Join(",", ids) + "]";


                }
                string urls = url + "/admintor/api/pbx/page/delete";
                var request = new HttpRequestMessage(HttpMethod.Post, urls);
                //  string data = "{\"api_str\": \"bgapi originate {'origination_caller_id_number=300,orgination_caller_id_name=api_call'}loopback/19190012028/国内+市话/xml 300 xml Local-Extensions\"}";
                string data = "{\"ids\":" + id + "}";
                // 添加参数
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                string token = getToken(url);
                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 输出响应内容
                string responseBody = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
                return obj.ToString();
            }

        }
        /// <summary>
        /// 分组查询
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        [DllExport("groupQuery", CallingConvention = CallingConvention.Cdecl)]
        public static string groupQuery([MarshalAs(UnmanagedType.LPStr)] string page_number, [MarshalAs(UnmanagedType.LPStr)] string page_size, [MarshalAs(UnmanagedType.LPStr)] string url)
        {
            string urls = url + "/admintor/api/pbx/page/query";

            using (var httpClient = new HttpClient())
            {
                // 设置请求的URI


                // 创建查询参数
                var query = ParseQueryString(string.Empty);
                query["page_number"] = page_number.ToString();
                query["page_size"] = page_size.ToString();


                // 添加查询参数到URI
                var fullUri = $"{urls}?{query}";

                // 创建HttpRequestMessage
                var request = new HttpRequestMessage(HttpMethod.Get, fullUri);

                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken(url));


                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 确保HTTP成功状态值
                response.EnsureSuccessStatusCode();

                // 读取响应内容（根据需要处理）
                var content = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(content);
                return obj.ToString();


            }

        }

        /// <summary>
        /// 修改广播
        /// </summary>
        /// <returns></returns>
        [DllExport("update", CallingConvention = CallingConvention.Cdecl)]
        public static string update([MarshalAs(UnmanagedType.LPStr)] string id, [MarshalAs(UnmanagedType.LPStr)] string number,
            [MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string flags, [MarshalAs(UnmanagedType.LPStr)] string call_timeout,
            [MarshalAs(UnmanagedType.LPStr)] string record_format, [MarshalAs(UnmanagedType.LPStr)] string rtmp_server,
            [MarshalAs(UnmanagedType.Bool)] bool enable_file_write_buffering,
             [MarshalAs(UnmanagedType.Bool)] bool record_stereo,
                   [MarshalAs(UnmanagedType.Bool)] bool media_bug_answer_req,
                       [MarshalAs(UnmanagedType.LPStr)] string record_min_sec,
                        [MarshalAs(UnmanagedType.LPArray)] int[] member_ids,
                         [MarshalAs(UnmanagedType.LPStr)] string url
            )
        {
            using (var httpClient = new HttpClient())
            {
                string urls = url + "/admintor/api/pbx/page/update";
                var request = new HttpRequestMessage(HttpMethod.Post, urls);
                string ids = string.Empty;
                if (member_ids.Count() > 0)
                {
                    ids = "[" + String.Join(",", member_ids) + "]";


                }
                // string media_bug_answer_reqs=  media_bug_answer_req.ToString().ToLower();
                string media_bug_answer_reqs = media_bug_answer_req.ToString().ToLower(); ;
                string enable_file_write_bufferings = enable_file_write_buffering.ToString().ToLower(); ;
                string record_stereos = record_stereo.ToString().ToLower(); ;

                // string data1 ="{\"id\":1,\"number\":\"500\",\"name\":\"A区广播\",\"flags\":\"mute\",\"call_timeout\":30,\"record_format\":\"false\",\"rtmp_server\":\"\",\"enable_file_write_buffering\":false,\"record_stereo\":true,\"media_bug_answer_req\":true,\"record_min_sec\":1,\"member_ids\":[1,2,3]}";
                string data = "{\"id\":" + id + ",\"number\":\"" + number + "\",\"name\":\"" + name + "\",\"flags\":\"" + flags + "\",\"call_timeout\":" + call_timeout + ",\"record_format\":\"" + record_format + "\",\"rtmp_server\":\"" + rtmp_server + "\",\"enable_file_write_buffering\":" + enable_file_write_bufferings + ",\"record_stereo\":" + record_stereos + ",\"media_bug_answer_req\":" + media_bug_answer_reqs + ",\"record_min_sec\":" + record_min_sec + ",\"member_ids\":" + ids + "}";
                //
                // string data = "{\"id\":1,\"number\":\"500\",\"name\":\"A区广播\",\"flags\":\"mute\",\"call_timeout\":30,\"record_format\":\"false\",\"rtmp_server\":\"\",\"enable_file_write_buffering\":false,\"record_stereo\":true,\"media_bug_answer_req\":true,\"record_min_sec\":1,\"member_ids\":[1,2,3]}";
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                string token = getToken(url);
                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 输出响应内容
                string responseBody = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
                return obj.ToString();
            }

        }

        /// <summary>
        /// 怎加分机
        /// </summary>
        /// <param name="number"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [DllExport("create", CallingConvention = CallingConvention.Cdecl)]
        public static string create([MarshalAs(UnmanagedType.LPStr)] string number, [MarshalAs(UnmanagedType.LPStr)] string password, [MarshalAs(UnmanagedType.LPStr)] string url)
        {
            using (var httpClient = new HttpClient())
            {
                string urls = url + "/admintor/api/pbx/phone/create";
                var request = new HttpRequestMessage(HttpMethod.Post, urls);
                // string data = "{\"number\":\"1000\",\"password\":\"1000\",\"name\":\"user1\",\"department_id\":1,\"context_id\":1,\"callgroup\":1,\"alias_numbers\":\"\",\"call_timeout\":60,\"sched_hangup\":7200,\"video_bandwidth\":2,\"jitterbuffer\":0,\"limit_max\":1,\"tx_vol\":0,\"rx_vol\":0,\"cid_type\":\"number\",\"absolute_codec_string\":[],\"media_processing\":\"default\",\"playback_filename\":\"aaa.wav\",\"ringback_filename\":\"bbb.wav\",\"record_format\":\"false\",\"enable_file_write_buffering\":false,\"record_stereo\":true,\"media_bug_answer_req\":true,\"record_min_sec\":1,\"rtmp_server\":\"\",\"minimum_session_expires\":120,\"sip_force_expires\":1800,\"sip_expires_max_deviation\":600,\"cidr\":\"\",\"allow_empty_password\":false,\"sip_allow_register\":true,\"zrtp_secure_media\":false,\"srtp_secure_media\":false,\"direct_tx_number\":\"\",\"noanswer_tx_number\":\"\",\"busy_tx_number\":\"\",\"offline_tx_number\":\"\",\"vm_enabled\":false,\"vm_password\":\"12345\",\"skip_greeting\":false,\"skip_instructions\":false,\"vm_email_all_messages\":true,\"vm_mailto\":\"jjjj@aa.cn\",\"vm_attach_file\":true,\"vm_keep_local_after_email\":true,\"cash\":999999999,\"autoanswer\":false,\"ringtone_sound\":\"ringin.wav\",\"redirect_method\":\"blank\",\"redirect_url\":\"http://192.168.0.100\",\"ice_server\":\"\",\"ice_username\":\"\",\"ice_password\":\"\",\"webuser_permissions\":[{\"id\":100,\"name\":\"Call\",\"label\":\"呼叫\",\"icon\":\"Phone\",\"path\":\"/webuser/api/call\",\"parent_id\":0},{\"id\":101,\"name\":\"CallTransfer\",\"label\":\"呼叫转移\",\"icon\":\"\",\"path\":\"/webuser/api/call/transfer\",\"parent_id\":100},{\"id\":200,\"name\":\"Dispatch\",\"label\":\"调度\",\"icon\":\"Management\",\"path\":\"/webuser/api/dispatch\",\"parent_id\":0},{\"id\":201,\"name\":\"DispatchVoice\",\"label\":\"语音呼叫\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/voice\",\"parent_id\":200},{\"id\":202,\"name\":\"DispatchVideo\",\"label\":\"视频呼叫\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/video\",\"parent_id\":200},{\"id\":204,\"name\":\"DispatchSpy\",\"label\":\"监听通话\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/spy\",\"parent_id\":200},{\"id\":207,\"name\":\"DispatchHup\",\"label\":\"强挂通话\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/hup\",\"parent_id\":200},{\"id\":300,\"name\":\"Conference\",\"label\":\"会议\",\"icon\":\"Monitor\",\"path\":\"/webuser/api/conference\",\"parent_id\":0},{\"id\":301,\"name\":\"ConferenceList\",\"label\":\"获取会议室列表\",\"icon\":\"\",\"path\":\"/webuser/api/conference/list\",\"parent_id\":300},{\"id\":302,\"name\":\"ConferenceDetail\",\"label\":\"获取会议室详情\",\"icon\":\"\",\"path\":\"/webuser/api/conference/detail\",\"parent_id\":300},{\"id\":303,\"name\":\"ConferenceQuery\",\"label\":\"查询会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/query\",\"parent_id\":300},{\"id\":304,\"name\":\"ConferenceCreate\",\"label\":\"添加会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/create\",\"parent_id\":300},{\"id\":305,\"name\":\"ConferenceUpdate\",\"label\":\"修改会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/update\",\"parent_id\":300},{\"id\":306,\"name\":\"ConferenceDelete\",\"label\":\"删除会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/delete\",\"parent_id\":300},{\"id\":308,\"name\":\"ConferenceStart\",\"label\":\"开始会议\",\"icon\":\"\",\"path\":\"/webuser/api/conference/start\",\"parent_id\":300},{\"id\":309,\"name\":\"ConferenceHup\",\"label\":\"结束会议\",\"icon\":\"\",\"path\":\"/webuser/api/conference/hup\",\"parent_id\":300},{\"id\":310,\"name\":\"ConferenceBgDial\",\"label\":\"邀请成员\",\"icon\":\"\",\"path\":\"/webuser/api/conference/bgdial\",\"parent_id\":300},{\"id\":311,\"name\":\"ConferenceMute\",\"label\":\"禁止发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/mute\",\"parent_id\":300},{\"id\":312,\"name\":\"ConferenceUnMute\",\"label\":\"允许发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unmute\",\"parent_id\":300},{\"id\":313,\"name\":\"ConferenceTmute\",\"label\":\"自动切换禁止及允许发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/tmute\",\"parent_id\":300},{\"id\":314,\"name\":\"ConferenceVmute\",\"label\":\"关闭视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vmute\",\"parent_id\":300},{\"id\":315,\"name\":\"ConferenceUnVmute\",\"label\":\"开启视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unvmute\",\"parent_id\":300},{\"id\":316,\"name\":\"ConferenceTvmute\",\"label\":\"自动切换关闭及开启视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/tvmute\",\"parent_id\":300},{\"id\":317,\"name\":\"ConferenceDeaf\",\"label\":\"开启禁音\",\"icon\":\"\",\"path\":\"/webuser/api/conference/deaf\",\"parent_id\":300},{\"id\":318,\"name\":\"ConferenceUnDeaf\",\"label\":\"关闭禁音\",\"icon\":\"\",\"path\":\"/webuser/api/conference/undeaf\",\"parent_id\":300},{\"id\":324,\"name\":\"ConferenceLock\",\"label\":\"锁定会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/lock\",\"parent_id\":300},{\"id\":325,\"name\":\"ConferenceUnLock\",\"label\":\"解锁会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unlock\",\"parent_id\":300},{\"id\":330,\"name\":\"ConferenceVidLayout\",\"label\":\"设置布局\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidlayout\",\"parent_id\":300},{\"id\":331,\"name\":\"ConferenceVidRes\",\"label\":\"设置分辨率\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidres\",\"parent_id\":300},{\"id\":332,\"name\":\"ConferenceVidFps\",\"label\":\"设置帧率\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidfps\",\"parent_id\":300},{\"id\":333,\"name\":\"ConferenceVidBandwidth\",\"label\":\"设置带宽\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidbandwidth\",\"parent_id\":300},{\"id\":334,\"name\":\"ConferenceKick\",\"label\":\"踢出成员\",\"icon\":\"\",\"path\":\"/webuser/api/conference/kick\",\"parent_id\":300},{\"id\":335,\"name\":\"ConferenceVidLayer\",\"label\":\"调整位置\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidlayer\",\"parent_id\":300},{\"id\":336,\"name\":\"ConferenceVidFloor\",\"label\":\"设置主屏\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidfloor\",\"parent_id\":300},{\"id\":337,\"name\":\"ConferenceCdrList\",\"label\":\"获取会议室记录列表\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/list\",\"parent_id\":300},{\"id\":338,\"name\":\"ConferenceCdrDetail\",\"label\":\"获取会议室记录详情\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/detail\",\"parent_id\":300},{\"id\":339,\"name\":\"ConferenceCdrQuery\",\"label\":\"查询会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/query\",\"parent_id\":300},{\"id\":340,\"name\":\"ConferenceCdrDelete\",\"label\":\"删除会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/delete\",\"parent_id\":300},{\"id\":341,\"name\":\"ConferenceCdrDelete\",\"label\":\"删除所有会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/delall\",\"parent_id\":300},{\"id\":342,\"name\":\"ConferenceRecording\",\"label\":\"下载会议录音录像\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/downloadrecording\",\"parent_id\":300},{\"id\":500,\"name\":\"Txl\",\"label\":\"通讯录\",\"icon\":\"Notebook\",\"path\":\"/webuser/api/txl\",\"parent_id\":0},{\"id\":501,\"name\":\"TxlList\",\"label\":\"获取通讯录列表\",\"icon\":\"\",\"path\":\"/webuser/api/txl/list\",\"parent_id\":500},{\"id\":502,\"name\":\"TxlCreate\",\"label\":\"添加通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/create\",\"parent_id\":500},{\"id\":503,\"name\":\"TxlDelete\",\"label\":\"删除通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/delete\",\"parent_id\":500},{\"id\":504,\"name\":\"TxlUpdate\",\"label\":\"修改通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/update\",\"parent_id\":500},{\"id\":505,\"name\":\"TxlQuery\",\"label\":\"查询通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/query\",\"parent_id\":500},{\"id\":506,\"name\":\"TxlDetail\",\"label\":\"查看通讯录详情\",\"icon\":\"\",\"path\":\"/webuser/api/txl/detail\",\"parent_id\":500},{\"id\":507,\"name\":\"TxlCall\",\"label\":\"呼叫通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/call\",\"parent_id\":500},{\"id\":600,\"name\":\"Cdr\",\"label\":\"CDR\",\"icon\":\"Tickets\",\"path\":\"/webuser/api/cdr\",\"parent_id\":0},{\"id\":601,\"name\":\"CdrList\",\"label\":\"获取分机记录列表\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/list\",\"parent_id\":600},{\"id\":602,\"name\":\"CdrDelete\",\"label\":\"删除分机通话记录\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/delete\",\"parent_id\":600},{\"id\":603,\"name\":\"CdrQuery\",\"label\":\"查询分机通话记录\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/query\",\"parent_id\":600},{\"id\":604,\"name\":\"CdrDownloadRecording\",\"label\":\"下载分机通话录音录像\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/downloadrecording\",\"parent_id\":600},{\"id\":700,\"name\":\"Setting\",\"label\":\"设置\",\"icon\":\"Setting\",\"path\":\"/webuser/api/setting\",\"parent_id\":0},{\"id\":701,\"name\":\"SettingUpdate\",\"label\":\"修改设置\",\"icon\":\"\",\"path\":\"/webuser/api/setting/update\",\"parent_id\":700}]}";

                string data = "{\"number\":" + number + ",\"password\":" + password + ",\"name\":\"user1\",\"department_id\":1,\"context_id\":1,\"callgroup\":1,\"alias_numbers\":\"\",\"call_timeout\":60,\"sched_hangup\":7200,\"video_bandwidth\":2,\"jitterbuffer\":0,\"limit_max\":1,\"tx_vol\":0,\"rx_vol\":0,\"cid_type\":\"number\",\"absolute_codec_string\":[],\"media_processing\":\"default\",\"playback_filename\":\"aaa.wav\",\"ringback_filename\":\"bbb.wav\",\"record_format\":\"false\",\"enable_file_write_buffering\":false,\"record_stereo\":true,\"media_bug_answer_req\":true,\"record_min_sec\":1,\"rtmp_server\":\"\",\"minimum_session_expires\":120,\"sip_force_expires\":1800,\"sip_expires_max_deviation\":600,\"cidr\":\"\",\"allow_empty_password\":false,\"sip_allow_register\":true,\"zrtp_secure_media\":false,\"srtp_secure_media\":false,\"direct_tx_number\":\"\",\"noanswer_tx_number\":\"\",\"busy_tx_number\":\"\",\"offline_tx_number\":\"\",\"vm_enabled\":false,\"vm_password\":\"12345\",\"skip_greeting\":false,\"skip_instructions\":false,\"vm_email_all_messages\":true,\"vm_mailto\":\"jjjj@aa.cn\",\"vm_attach_file\":true,\"vm_keep_local_after_email\":true,\"cash\":999999999,\"autoanswer\":false,\"ringtone_sound\":\"ringin.wav\",\"redirect_method\":\"blank\",\"redirect_url\":\"http://192.168.0.100\",\"ice_server\":\"\",\"ice_username\":\"\",\"ice_password\":\"\",\"webuser_permissions\":[{\"id\":100,\"name\":\"Call\",\"label\":\"呼叫\",\"icon\":\"Phone\",\"path\":\"/webuser/api/call\",\"parent_id\":0},{\"id\":101,\"name\":\"CallTransfer\",\"label\":\"呼叫转移\",\"icon\":\"\",\"path\":\"/webuser/api/call/transfer\",\"parent_id\":100},{\"id\":200,\"name\":\"Dispatch\",\"label\":\"调度\",\"icon\":\"Management\",\"path\":\"/webuser/api/dispatch\",\"parent_id\":0},{\"id\":201,\"name\":\"DispatchVoice\",\"label\":\"语音呼叫\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/voice\",\"parent_id\":200},{\"id\":202,\"name\":\"DispatchVideo\",\"label\":\"视频呼叫\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/video\",\"parent_id\":200},{\"id\":204,\"name\":\"DispatchSpy\",\"label\":\"监听通话\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/spy\",\"parent_id\":200},{\"id\":207,\"name\":\"DispatchHup\",\"label\":\"强挂通话\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/hup\",\"parent_id\":200},{\"id\":300,\"name\":\"Conference\",\"label\":\"会议\",\"icon\":\"Monitor\",\"path\":\"/webuser/api/conference\",\"parent_id\":0},{\"id\":301,\"name\":\"ConferenceList\",\"label\":\"获取会议室列表\",\"icon\":\"\",\"path\":\"/webuser/api/conference/list\",\"parent_id\":300},{\"id\":302,\"name\":\"ConferenceDetail\",\"label\":\"获取会议室详情\",\"icon\":\"\",\"path\":\"/webuser/api/conference/detail\",\"parent_id\":300},{\"id\":303,\"name\":\"ConferenceQuery\",\"label\":\"查询会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/query\",\"parent_id\":300},{\"id\":304,\"name\":\"ConferenceCreate\",\"label\":\"添加会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/create\",\"parent_id\":300},{\"id\":305,\"name\":\"ConferenceUpdate\",\"label\":\"修改会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/update\",\"parent_id\":300},{\"id\":306,\"name\":\"ConferenceDelete\",\"label\":\"删除会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/delete\",\"parent_id\":300},{\"id\":308,\"name\":\"ConferenceStart\",\"label\":\"开始会议\",\"icon\":\"\",\"path\":\"/webuser/api/conference/start\",\"parent_id\":300},{\"id\":309,\"name\":\"ConferenceHup\",\"label\":\"结束会议\",\"icon\":\"\",\"path\":\"/webuser/api/conference/hup\",\"parent_id\":300},{\"id\":310,\"name\":\"ConferenceBgDial\",\"label\":\"邀请成员\",\"icon\":\"\",\"path\":\"/webuser/api/conference/bgdial\",\"parent_id\":300},{\"id\":311,\"name\":\"ConferenceMute\",\"label\":\"禁止发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/mute\",\"parent_id\":300},{\"id\":312,\"name\":\"ConferenceUnMute\",\"label\":\"允许发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unmute\",\"parent_id\":300},{\"id\":313,\"name\":\"ConferenceTmute\",\"label\":\"自动切换禁止及允许发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/tmute\",\"parent_id\":300},{\"id\":314,\"name\":\"ConferenceVmute\",\"label\":\"关闭视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vmute\",\"parent_id\":300},{\"id\":315,\"name\":\"ConferenceUnVmute\",\"label\":\"开启视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unvmute\",\"parent_id\":300},{\"id\":316,\"name\":\"ConferenceTvmute\",\"label\":\"自动切换关闭及开启视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/tvmute\",\"parent_id\":300},{\"id\":317,\"name\":\"ConferenceDeaf\",\"label\":\"开启禁音\",\"icon\":\"\",\"path\":\"/webuser/api/conference/deaf\",\"parent_id\":300},{\"id\":318,\"name\":\"ConferenceUnDeaf\",\"label\":\"关闭禁音\",\"icon\":\"\",\"path\":\"/webuser/api/conference/undeaf\",\"parent_id\":300},{\"id\":324,\"name\":\"ConferenceLock\",\"label\":\"锁定会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/lock\",\"parent_id\":300},{\"id\":325,\"name\":\"ConferenceUnLock\",\"label\":\"解锁会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unlock\",\"parent_id\":300},{\"id\":330,\"name\":\"ConferenceVidLayout\",\"label\":\"设置布局\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidlayout\",\"parent_id\":300},{\"id\":331,\"name\":\"ConferenceVidRes\",\"label\":\"设置分辨率\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidres\",\"parent_id\":300},{\"id\":332,\"name\":\"ConferenceVidFps\",\"label\":\"设置帧率\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidfps\",\"parent_id\":300},{\"id\":333,\"name\":\"ConferenceVidBandwidth\",\"label\":\"设置带宽\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidbandwidth\",\"parent_id\":300},{\"id\":334,\"name\":\"ConferenceKick\",\"label\":\"踢出成员\",\"icon\":\"\",\"path\":\"/webuser/api/conference/kick\",\"parent_id\":300},{\"id\":335,\"name\":\"ConferenceVidLayer\",\"label\":\"调整位置\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidlayer\",\"parent_id\":300},{\"id\":336,\"name\":\"ConferenceVidFloor\",\"label\":\"设置主屏\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidfloor\",\"parent_id\":300},{\"id\":337,\"name\":\"ConferenceCdrList\",\"label\":\"获取会议室记录列表\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/list\",\"parent_id\":300},{\"id\":338,\"name\":\"ConferenceCdrDetail\",\"label\":\"获取会议室记录详情\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/detail\",\"parent_id\":300},{\"id\":339,\"name\":\"ConferenceCdrQuery\",\"label\":\"查询会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/query\",\"parent_id\":300},{\"id\":340,\"name\":\"ConferenceCdrDelete\",\"label\":\"删除会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/delete\",\"parent_id\":300},{\"id\":341,\"name\":\"ConferenceCdrDelete\",\"label\":\"删除所有会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/delall\",\"parent_id\":300},{\"id\":342,\"name\":\"ConferenceRecording\",\"label\":\"下载会议录音录像\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/downloadrecording\",\"parent_id\":300},{\"id\":500,\"name\":\"Txl\",\"label\":\"通讯录\",\"icon\":\"Notebook\",\"path\":\"/webuser/api/txl\",\"parent_id\":0},{\"id\":501,\"name\":\"TxlList\",\"label\":\"获取通讯录列表\",\"icon\":\"\",\"path\":\"/webuser/api/txl/list\",\"parent_id\":500},{\"id\":502,\"name\":\"TxlCreate\",\"label\":\"添加通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/create\",\"parent_id\":500},{\"id\":503,\"name\":\"TxlDelete\",\"label\":\"删除通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/delete\",\"parent_id\":500},{\"id\":504,\"name\":\"TxlUpdate\",\"label\":\"修改通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/update\",\"parent_id\":500},{\"id\":505,\"name\":\"TxlQuery\",\"label\":\"查询通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/query\",\"parent_id\":500},{\"id\":506,\"name\":\"TxlDetail\",\"label\":\"查看通讯录详情\",\"icon\":\"\",\"path\":\"/webuser/api/txl/detail\",\"parent_id\":500},{\"id\":507,\"name\":\"TxlCall\",\"label\":\"呼叫通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/call\",\"parent_id\":500},{\"id\":600,\"name\":\"Cdr\",\"label\":\"CDR\",\"icon\":\"Tickets\",\"path\":\"/webuser/api/cdr\",\"parent_id\":0},{\"id\":601,\"name\":\"CdrList\",\"label\":\"获取分机记录列表\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/list\",\"parent_id\":600},{\"id\":602,\"name\":\"CdrDelete\",\"label\":\"删除分机通话记录\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/delete\",\"parent_id\":600},{\"id\":603,\"name\":\"CdrQuery\",\"label\":\"查询分机通话记录\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/query\",\"parent_id\":600},{\"id\":604,\"name\":\"CdrDownloadRecording\",\"label\":\"下载分机通话录音录像\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/downloadrecording\",\"parent_id\":600},{\"id\":700,\"name\":\"Setting\",\"label\":\"设置\",\"icon\":\"Setting\",\"path\":\"/webuser/api/setting\",\"parent_id\":0},{\"id\":701,\"name\":\"SettingUpdate\",\"label\":\"修改设置\",\"icon\":\"\",\"path\":\"/webuser/api/setting/update\",\"parent_id\":700}]}";

                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                string token = getToken(url);
                // 添加Authorization头
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 发送请求
                var response = httpClient.SendAsync(request).Result;

                // 输出响应内容
                string responseBody = response.Content.ReadAsStringAsync().Result;
                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
                return obj.ToString();
            }

        }




        /// <summary>
        /// 获取token
        /// </summary>
        /// <returns></returns>
        public static string getToken(string url)
        {
            var urls = url + "/admintor/api/login"; // 替换为你的API接口URL
            var data = "{\"username\":\"admin\",\"password\":\"admin\"}"; // 替换为你要发送的数据
            string result = PostResponse(urls, data, null);
            JObject jo = (JObject)JsonConvert.DeserializeObject(result);
            string token = jo["data"]["token"].ToString();
            return token;

        }
        /// <summary>
        /// post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData">post数据</param>
        /// <returns></returns>
        public static string PostResponse(string url, string postData, string token)
        {

            string result = "";
            if (url.StartsWith("https"))
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            HttpContent httpContent = new StringContent(postData);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            ////if(token != null)
            ////{
            ////    httpContent.DefaultRequestHeaders.Authorization =
            ////  new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            ////    httpContent.Headers.Add("Authorization", "Bearer "+token);
            ////}
            httpContent.Headers.ContentType.CharSet = "utf-8";

            HttpClient httpClient = new HttpClient();
            //httpClient..setParameter(HttpMethodParams.HTTP_CONTENT_CHARSET, "utf-8");

            HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;

            //  statusCode = response.StatusCode.ToString();
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
                return result;
            }

            return result;
        }
        public static NameValueCollection ParseQueryString(string query)
        {
            var result = new NameValueCollection();
            if (string.IsNullOrEmpty(query))
            {
                return result;
            }

            var bytes = Encoding.UTF8.GetBytes(query);
            for (int i = 0; i < bytes.Length; i++)
            {
                var j = i;
                while (j < bytes.Length && bytes[j] != '=')
                {
                    j++;
                }

                if (j < bytes.Length)
                {
                    var key = Encoding.UTF8.GetString(bytes, i, j - i);
                    i = j + 1;
                    j = i;
                    while (j < bytes.Length && bytes[j] != '&')
                    {
                        j++;
                    }

                    var value = Encoding.UTF8.GetString(bytes, i, j - i);
                    result.Add(key, value);
                    i = j + 1;
                }
            }

            return result;
        }

        //------------------------------------------------------------------------
        //        static Core core; static Factory factory; static Call call;
        //        static System.Timers.Timer t;
        //        [DllExport("Set_Login_Info", CallingConvention = CallingConvention.Cdecl)]
        //        public static void Set_Login_Info(
        //    [MarshalAs(UnmanagedType.LPStr)] string CSserverIP,
        //    [MarshalAs(UnmanagedType.LPStr)] string localIP,
        //    [MarshalAs(UnmanagedType.LPStr)] string userName,
        //    [MarshalAs(UnmanagedType.LPStr)] string passWord,
        //    [MarshalAs(UnmanagedType.LPStr)] string sipPort
        //    )
        //        {

        //            Reg(factory, core, CSserverIP + ":" + sipPort, userName, passWord);
        //        }


        //        /// <summary>
        //        /// CSharp_Call-StartP2PCall// 呼叫
        //        /// </summary>
        //        /// <param name="Sip"></param>
        //        /// <param name="IP"></param>
        //        /// <param name="Port"></param>
        //        [DllExport("StartP2PCall", CallingConvention = CallingConvention.Cdecl)]
        //        public static void StartP2PCall(
        //            [MarshalAs(UnmanagedType.LPStr)] string Sip,
        //     [MarshalAs(UnmanagedType.LPStr)] string IP,
        //    [MarshalAs(UnmanagedType.LPStr)] string Port
        //            )
        //        {
        //            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
        //            CallParams callParams = core.CreateCallParams(null);
        //            callParams.VideoEnabled = false;
        //            call = core.InviteAddressWithParams(core.InterpretUrl($"sip:{Sip}@{IP}:{Port}"), callParams);
        //        }
        //        /// <summary>
        //        /// CSharp_VideoCall-StP2PVideoCall  视频呼叫
        //        /// </summary>
        //        /// <param name="Sip"></param>
        //        /// <param name="IP"></param>
        //        /// <param name="Port"></param>
        //        /// <param name="Headle"></param>
        //        [DllExport("StP2PVideoCall", CallingConvention = CallingConvention.Cdecl)]
        //        public static void StP2PVideoCall(
        //    [MarshalAs(UnmanagedType.LPStr)] string Sip,
        //[MarshalAs(UnmanagedType.LPStr)] string IP,
        //[MarshalAs(UnmanagedType.LPStr)] string Port,
        //[MarshalAs(UnmanagedType.SysUInt)] IntPtr Headle
        //    )
        //        {
        //            core.NativeVideoWindowId = Headle;
        //            call = core.CurrentCall; if (call != null) { core.TerminateCall(call); }
        //            CallParams callParams = core.CreateCallParams(null);
        //            callParams.VideoEnabled = true;
        //            call = core.InviteAddressWithParams(core.InterpretUrl($"sip:{Sip}@{IP}:{Port}"), callParams);




        //        }
        //        /// <summary>
        //        /// CSharp_TerminateAllCalls-StartP2PHangup 挂断
        //        /// </summary>
        //        [DllExport("StartP2PHangup", CallingConvention = CallingConvention.Cdecl)]
        //        public static void StartP2PHangup()
        //        {
        //            core.TerminateAllCalls();
        //        }
        //        /// <summary>
        //        /// CSharp_Accept-StartP2PRecv 接听
        //        /// </summary>
        //        [DllExport("StartP2PRecv", CallingConvention = CallingConvention.Cdecl)]
        //        public static void StartP2PRecv()
        //        {
        //            call = core.CurrentCall;
        //            if (call != null)
        //            {
        //                call.Accept();
        //            }

        //        }

        //        [DllExport("SDK_START", CallingConvention = CallingConvention.Cdecl)]
        //        public static int SDK_START()
        //        {
        //            //日志开启
        //            // LoggingService LoggingService = LoggingService.Instance;
        //            // LoggingService.LogLevel = LogLevel.Debug;
        //            //LoggingService.Listener.OnLogMessageWritten = OnLog;

        //            factory = Factory.Instance;

        //            //C:\Users\lilin\AppData\Local\linphone 如果装过linphone win exe的请删除这里面的内容否则core 创建不成功
        //            //多线程core需要加线程锁
        //            core = factory.CreateCore(new CoreListener() { }, "linphonerc", "linphonerc-factory");

        //            core.AvpfMode = AVPFMode.Disabled;
        //            Core.EnableLogCollection(LogCollectionState.Disabled);
        //            core.Listener.OnRegistrationStateChanged = OnRegistration;
        //            core.Listener.OnCallStateChanged = OnCallState;
        //            core.Listener.OnMessageReceived = OnMessage;
        //            core.Transports.UdpPort = -1;

        //            string x = "";
        //            foreach (string s in core.VideoDevicesList)
        //            {
        //                Console.WriteLine("set=" + s);
        //                if (s.IndexOf("Camera") != -1)
        //                {// bctbx-error-bctbx_file_open: Error open Permission denied
        //                    x = s;
        //                }
        //            }
        //            if (x != "")
        //            {
        //                Console.WriteLine("set last=" + x);
        //                core.VideoDevice = x;//"Directshow capture: Integrated Camera"; //摄像头设置
        //            }
        //            Console.WriteLine(">>" + core.DefaultInputAudioDevice.DeviceName);
        //            Console.WriteLine(">>" + core.DefaultOutputAudioDevice.DeviceName);

        //            foreach (AudioDevice s in core.ExtendedAudioDevices)
        //            {
        //                Console.WriteLine(s.DeviceName + ">++>");
        //            }
        //            core.SetUserAgent("mylinphone", "1.0");
        //            Console.WriteLine(">>");
        //            // core.InputAudioDevice
        //            //core.OutputAudioDevice

        //            // core.NativeVideoWindowId = pictureBox1.Handle;//video window set

        //            foreach (PayloadType p in core.AudioPayloadTypes)
        //            {
        //                if (p.MimeType == "opus") { p.Enable(false); }//
        //                Console.Write(p.MimeType + "(" + p.Enabled() + ")" + "=");
        //            }
        //            Console.WriteLine();
        //            foreach (PayloadType p in core.VideoPayloadTypes)
        //            {
        //                if (p.MimeType == "VP8") { p.Enable(false); }//
        //                Console.Write(p.MimeType + "(" + p.Enabled() + ")" + "=");
        //            }
        //            Console.WriteLine();


        //            t = new System.Timers.Timer(20);   //实例化Timer类，设置间隔时间为10000毫秒；   
        //            t.Elapsed += new System.Timers.ElapsedEventHandler(theout); //到达时间的时候执行事件；   
        //            t.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
        //            t.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；   

        //            return 0; // 假设成功启动，返回0  
        //        }
        //        [DllExport("SDK_STOP", CallingConvention = CallingConvention.Cdecl)]
        //        public static int SDK_STOP()
        //        {
        //            // 在这里实现SDK停止的逻辑  
        //            // 返回停止状态或错误代码（通常是非零值表示错误）  

        //            // 假设成功停止，返回0  
        //            return 0;
        //        }

        //        [DllExport("CSharp_proviAll", CallingConvention = CallingConvention.Cdecl)]
        //        public static string CSharp_proviAll(int jarg1, long jarg2)
        //        {
        //            return "";
        //        }
        //        // 导出CSharp_getPgrpList函数  
        //        [DllExport("CSharp_getPgrpList", CallingConvention = CallingConvention.Cdecl)]
        //        public static string CSharp_getPgrpList(long dcid)
        //        {
        //            // 在这里实现获取程序组列表的逻辑  
        //            // 根据传入的dcid参数获取相应的程序组列表，并转换为字符串返回  

        //            // 示例：仅返回硬编码的字符串，实际情况下你应该实现具体的逻辑  
        //            return "示例程序组列表";
        //        }

        //        static private void Reg(Factory factory, Core core, String domain, String username, String password)
        //        {

        //            foreach (AuthInfo a in core.AuthInfoList)
        //            {
        //                core.RemoveAuthInfo(a);
        //            }
        //            foreach (ProxyConfig a in core.ProxyConfigList)
        //            {
        //                core.RemoveProxyConfig(a);
        //            }

        //            AuthInfo authInfo = factory.CreateAuthInfo(username, username, password, null, null, domain);
        //            core.AddAuthInfo(authInfo);

        //            /*
        //            NatPolicy nat= core.CreateNatPolicy();
        //            nat.IceEnabled = true;
        //            nat.TurnEnabled = true;
        //            nat.StunServer = "118.190.151.162:3478";
        //            string turnusername = "lilin",turnpwd= "lilinaini";

        //            AuthInfo xauthInfo = core.FindAuthInfo(null, turnusername, null);
        //            if (xauthInfo != null)
        //            { 
        //                AuthInfo cloneAuthInfo = xauthInfo.Clone();
        //                core.RemoveAuthInfo(xauthInfo);   
        //                cloneAuthInfo.Username = turnusername;
        //                cloneAuthInfo.Userid = turnusername;
        //                cloneAuthInfo.Passwd=turnpwd;
        //                core.AddAuthInfo(cloneAuthInfo);
        //            }
        //            else
        //            {
        //                AuthInfo cloneAuthInfo = core.CreateAuthInfo(turnusername, turnusername, turnpwd, null, null, null);
        //                core.AddAuthInfo(cloneAuthInfo);
        //            }
        //            //
        //            nat.StunServerUsername = turnusername;
        //            core.NatPolicy = nat;   */

        //            var proxyConfig = core.CreateProxyConfig();
        //            String sipurl = "sip:" + username + "@" + domain;
        //            var identity = factory.CreateAddress(sipurl);
        //            // identity.Username = username;
        //            //identity.Domain = domain;
        //            //identity.Transport = TransportType.Udp;
        //            proxyConfig.Edit();
        //            proxyConfig.IdentityAddress = identity;
        //            proxyConfig.ServerAddr = sipurl;
        //            //  proxyConfig.Route = domain;

        //            proxyConfig.RegisterEnabled = true;
        //            proxyConfig.PublishEnabled = true;
        //            proxyConfig.AvpfMode = AVPFMode.Disabled;
        //            proxyConfig.Done();
        //            core.AddProxyConfig(proxyConfig);
        //            core.DefaultProxyConfig = proxyConfig;

        //            //core.RefreshRegisters();

        //        }
        //        static private void OnCallState(Core lc, Call call, CallState cstate, string message)
        //        {

        //            //Console.WriteLine("OnCallState=" + message);
        //        }

        //        static private void OnRegistration(Core lc, ProxyConfig cfg, RegistrationState cstate, string message)
        //        {
        //            //Console.WriteLine("OnRegistration" + message);
        //            ////label1.Text = "12";
        //        }
        //        static private void OnMessage(Core lc, ChatRoom room, ChatMessage message)
        //        {
        //            //   Console.WriteLine("收到消息>>" + message.TextContent); ;
        //        }
        //        static private void theout(object sender, ElapsedEventArgs e)
        //        {

        //            core.Iterate();

        //        }
        //        //----------------------------------------------------------------------------------

        //        /// <summary>
        //        /// 增加广播
        //        /// </summary>
        //        /// <param name="number"></param>
        //        /// <returns></returns>
        //        [DllExport("AddBroadcast", CallingConvention = CallingConvention.Cdecl)]
        //        public static object AddBroadcast(string number,string url)
        //        {
        //            using (var httpClient = new HttpClient())
        //            {
        //                string urls = url + "/admintor/api/pbx/page/create";
        //                //"http://121.196.245.215/admintor/api/pbx/page/create"
        //                var request = new HttpRequestMessage(HttpMethod.Post, urls);
        //                //  string data = "{\"api_str\": \"bgapi originate {'origination_caller_id_number=300,orgination_caller_id_name=api_call'}loopback/19190012028/国内+市话/xml 300 xml Local-Extensions\"}";
        //                var data = "{\"number\":\"+number+\",\"name\":\"A区广播\",\"flags\":\"mute\",\"call_timeout\": 30,\"record_format\": \"false\",\"rtmp_server\": \"\",\"enable_file_write_buffering\": false,\"record_stereo\": true,\"media_bug_answer_req\": true,\"record_min_sec\": 1,\"member_ids\":[1,2,3]}"; // 替换为你要发送的数据
        //                                                                                                                                                                                                                                                                                                                // 添加参数
        //                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
        //                string token = getToken();
        //                // 添加Authorization头
        //                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken());

        //                // 发送请求
        //                var response = httpClient.SendAsync(request).Result;

        //                // 输出响应内容
        //                string responseBody = response.Content.ReadAsStringAsync().Result;
        //                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
        //                return obj;
        //            }



        //        }
        //        /// <summary>
        //        /// uuid查询
        //        /// </summary>
        //        /// <param name="number"></param>
        //        /// <returns></returns>
        //        [DllExport("query", CallingConvention = CallingConvention.Cdecl)]
        //        public static object query(int page_number, int page_size, string cid_num)
        //        {
        //            string url = "http://121.196.245.215/admintor/api/status/channel/query";

        //            using (var httpClient = new HttpClient())
        //            {
        //                // 设置请求的URI


        //                // 创建查询参数
        //                var query = ParseQueryString(string.Empty);
        //                query["page_number"] = page_number.ToString();
        //                query["page_size"] = page_size.ToString();
        //                query["cid_num"] = cid_num;

        //                // 添加查询参数到URI
        //                var fullUri = $"{url}?{query}";

        //                // 创建HttpRequestMessage
        //                var request = new HttpRequestMessage(HttpMethod.Get, fullUri);

        //                // 添加Authorization头
        //                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken());


        //                // 发送请求
        //                var response = httpClient.SendAsync(request).Result;

        //                // 确保HTTP成功状态值
        //                response.EnsureSuccessStatusCode();

        //                // 读取响应内容（根据需要处理）
        //                var content = response.Content.ReadAsStringAsync().Result;
        //                JObject obj = (JObject)JsonConvert.DeserializeObject(content);
        //                return obj;


        //            }

        //        }
        //        /// <summary>
        //        /// 群组
        //        /// </summary>
        //        /// <returns></returns>
        //        [DllExport("fsapi", CallingConvention = CallingConvention.Cdecl)]
        //        public static object fsapi(string api_str)
        //        {
        //            using (var httpClient = new HttpClient())
        //            {
        //                var request = new HttpRequestMessage(HttpMethod.Post, "http://121.196.245.215/admintor/api/utils/fsapi");
        //                //  string data = "{\"api_str\": \"bgapi originate {'origination_caller_id_number=300,orgination_caller_id_name=api_call'}loopback/19190012028/国内+市话/xml 300 xml Local-Extensions\"}";
        //                string data = "{\"api_str\":\"+api_str+\"}";
        //                // 添加参数
        //                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
        //                string token = getToken();
        //                // 添加Authorization头
        //                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken());

        //                // 发送请求
        //                var response = httpClient.SendAsync(request).Result;

        //                // 输出响应内容
        //                string responseBody = response.Content.ReadAsStringAsync().Result;
        //                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
        //                return obj;
        //            }

        //        }

        //        /// <summary>
        //        ///  群组删除
        //        /// </summary>
        //        /// <returns></returns>
        //        [DllExport("delete", CallingConvention = CallingConvention.Cdecl)]
        //        public static object delete(int[] ids)
        //        {
        //            using (var httpClient = new HttpClient())
        //            {
        //                // var str=ids.ToString();
        //                string id = string.Empty;
        //                if (ids.Count() > 0)
        //                {
        //                    id = "[" + String.Join(",", ids) + "]";


        //                }
        //                var request = new HttpRequestMessage(HttpMethod.Post, "http://121.196.245.215/admintor/api/pbx/page/delete");
        //                //  string data = "{\"api_str\": \"bgapi originate {'origination_caller_id_number=300,orgination_caller_id_name=api_call'}loopback/19190012028/国内+市话/xml 300 xml Local-Extensions\"}";
        //                string data = "{\"ids\":" + id + "}";
        //                // 添加参数
        //                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
        //                string token = getToken();
        //                // 添加Authorization头
        //                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken());

        //                // 发送请求
        //                var response = httpClient.SendAsync(request).Result;

        //                // 输出响应内容
        //                string responseBody = response.Content.ReadAsStringAsync().Result;
        //                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
        //                return obj;
        //            }

        //        }
        //        /// <summary>
        //        /// 分组查询
        //        /// </summary>
        //        /// <param name="number"></param>
        //        /// <returns></returns>
        //        [DllExport("groupQuery", CallingConvention = CallingConvention.Cdecl)]
        //        public static object groupQuery(int page_number, int page_size)
        //        {
        //            string url = "http://121.196.245.215/admintor/api/pbx/page/query";

        //            using (var httpClient = new HttpClient())
        //            {
        //                // 设置请求的URI


        //                // 创建查询参数
        //                var query = ParseQueryString(string.Empty);
        //                query["page_number"] = page_number.ToString();
        //                query["page_size"] = page_size.ToString();


        //                // 添加查询参数到URI
        //                var fullUri = $"{url}?{query}";

        //                // 创建HttpRequestMessage
        //                var request = new HttpRequestMessage(HttpMethod.Get, fullUri);

        //                // 添加Authorization头
        //                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken());


        //                // 发送请求
        //                var response = httpClient.SendAsync(request).Result;

        //                // 确保HTTP成功状态值
        //                response.EnsureSuccessStatusCode();

        //                // 读取响应内容（根据需要处理）
        //                var content = response.Content.ReadAsStringAsync().Result;
        //                JObject obj = (JObject)JsonConvert.DeserializeObject(content);
        //                return obj;


        //            }

        //        }
        //        /// <summary>
        //        /// 群组
        //        /// </summary>
        //        /// <returns></returns>
        //        [DllExport("update", CallingConvention = CallingConvention.Cdecl)]
        //        public static object update(UpdateInfo updateInfo)
        //        {
        //            using (var httpClient = new HttpClient())
        //            {
        //                var request = new HttpRequestMessage(HttpMethod.Post, "http://121.196.245.215/admintor/api/pbx/page/update");
        //                string id = string.Empty;
        //                if (updateInfo.member_ids.Count() > 0)
        //                {
        //                    id = "[" + String.Join(",", updateInfo.member_ids) + "]";


        //                }
        //                string media_bug_answer_req = updateInfo.media_bug_answer_req.ToString().ToLower(); ;
        //                string enable_file_write_buffering = updateInfo.enable_file_write_buffering.ToString().ToLower(); ;
        //                string record_stereo = updateInfo.record_stereo.ToString().ToLower(); ;

        //                // string data1 ="{\"id\":1,\"number\":\"500\",\"name\":\"A区广播\",\"flags\":\"mute\",\"call_timeout\":30,\"record_format\":\"false\",\"rtmp_server\":\"\",\"enable_file_write_buffering\":false,\"record_stereo\":true,\"media_bug_answer_req\":true,\"record_min_sec\":1,\"member_ids\":[1,2,3]}";
        //                string data = "{\"id\":" + updateInfo.id + ",\"number\":\"" + updateInfo.number + "\",\"name\":\"" + updateInfo.name + "\",\"flags\":\"" + updateInfo.flags + "\",\"call_timeout\":" + updateInfo.call_timeout + ",\"record_format\":\"" + updateInfo.record_format + "\",\"rtmp_server\":\"" + updateInfo.rtmp_server + "\",\"enable_file_write_buffering\":" + enable_file_write_buffering + ",\"record_stereo\":" + record_stereo + ",\"media_bug_answer_req\":" + media_bug_answer_req + ",\"record_min_sec\":" + updateInfo.record_min_sec + ",\"member_ids\":" + id + "}";
        //                //
        //                // string data = "{\"id\":1,\"number\":\"500\",\"name\":\"A区广播\",\"flags\":\"mute\",\"call_timeout\":30,\"record_format\":\"false\",\"rtmp_server\":\"\",\"enable_file_write_buffering\":false,\"record_stereo\":true,\"media_bug_answer_req\":true,\"record_min_sec\":1,\"member_ids\":[1,2,3]}";
        //                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
        //                string token = getToken();
        //                // 添加Authorization头
        //                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken());

        //                // 发送请求
        //                var response = httpClient.SendAsync(request).Result;

        //                // 输出响应内容
        //                string responseBody = response.Content.ReadAsStringAsync().Result;
        //                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
        //                return obj;
        //            }

        //        }

        //        /// <summary>
        //        /// 怎加分机
        //        /// </summary>
        //        /// <param name="number"></param>
        //        /// <param name="password"></param>
        //        /// <returns></returns>
        //        [DllExport("create", CallingConvention = CallingConvention.Cdecl)]
        //        public static object create(string number, string password)
        //        {
        //            using (var httpClient = new HttpClient())
        //            {
        //                var request = new HttpRequestMessage(HttpMethod.Post, "http://121.196.245.215/admintor/api/pbx/phone/create");
        //                // string data = "{\"number\":\"1000\",\"password\":\"1000\",\"name\":\"user1\",\"department_id\":1,\"context_id\":1,\"callgroup\":1,\"alias_numbers\":\"\",\"call_timeout\":60,\"sched_hangup\":7200,\"video_bandwidth\":2,\"jitterbuffer\":0,\"limit_max\":1,\"tx_vol\":0,\"rx_vol\":0,\"cid_type\":\"number\",\"absolute_codec_string\":[],\"media_processing\":\"default\",\"playback_filename\":\"aaa.wav\",\"ringback_filename\":\"bbb.wav\",\"record_format\":\"false\",\"enable_file_write_buffering\":false,\"record_stereo\":true,\"media_bug_answer_req\":true,\"record_min_sec\":1,\"rtmp_server\":\"\",\"minimum_session_expires\":120,\"sip_force_expires\":1800,\"sip_expires_max_deviation\":600,\"cidr\":\"\",\"allow_empty_password\":false,\"sip_allow_register\":true,\"zrtp_secure_media\":false,\"srtp_secure_media\":false,\"direct_tx_number\":\"\",\"noanswer_tx_number\":\"\",\"busy_tx_number\":\"\",\"offline_tx_number\":\"\",\"vm_enabled\":false,\"vm_password\":\"12345\",\"skip_greeting\":false,\"skip_instructions\":false,\"vm_email_all_messages\":true,\"vm_mailto\":\"jjjj@aa.cn\",\"vm_attach_file\":true,\"vm_keep_local_after_email\":true,\"cash\":999999999,\"autoanswer\":false,\"ringtone_sound\":\"ringin.wav\",\"redirect_method\":\"blank\",\"redirect_url\":\"http://192.168.0.100\",\"ice_server\":\"\",\"ice_username\":\"\",\"ice_password\":\"\",\"webuser_permissions\":[{\"id\":100,\"name\":\"Call\",\"label\":\"呼叫\",\"icon\":\"Phone\",\"path\":\"/webuser/api/call\",\"parent_id\":0},{\"id\":101,\"name\":\"CallTransfer\",\"label\":\"呼叫转移\",\"icon\":\"\",\"path\":\"/webuser/api/call/transfer\",\"parent_id\":100},{\"id\":200,\"name\":\"Dispatch\",\"label\":\"调度\",\"icon\":\"Management\",\"path\":\"/webuser/api/dispatch\",\"parent_id\":0},{\"id\":201,\"name\":\"DispatchVoice\",\"label\":\"语音呼叫\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/voice\",\"parent_id\":200},{\"id\":202,\"name\":\"DispatchVideo\",\"label\":\"视频呼叫\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/video\",\"parent_id\":200},{\"id\":204,\"name\":\"DispatchSpy\",\"label\":\"监听通话\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/spy\",\"parent_id\":200},{\"id\":207,\"name\":\"DispatchHup\",\"label\":\"强挂通话\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/hup\",\"parent_id\":200},{\"id\":300,\"name\":\"Conference\",\"label\":\"会议\",\"icon\":\"Monitor\",\"path\":\"/webuser/api/conference\",\"parent_id\":0},{\"id\":301,\"name\":\"ConferenceList\",\"label\":\"获取会议室列表\",\"icon\":\"\",\"path\":\"/webuser/api/conference/list\",\"parent_id\":300},{\"id\":302,\"name\":\"ConferenceDetail\",\"label\":\"获取会议室详情\",\"icon\":\"\",\"path\":\"/webuser/api/conference/detail\",\"parent_id\":300},{\"id\":303,\"name\":\"ConferenceQuery\",\"label\":\"查询会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/query\",\"parent_id\":300},{\"id\":304,\"name\":\"ConferenceCreate\",\"label\":\"添加会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/create\",\"parent_id\":300},{\"id\":305,\"name\":\"ConferenceUpdate\",\"label\":\"修改会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/update\",\"parent_id\":300},{\"id\":306,\"name\":\"ConferenceDelete\",\"label\":\"删除会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/delete\",\"parent_id\":300},{\"id\":308,\"name\":\"ConferenceStart\",\"label\":\"开始会议\",\"icon\":\"\",\"path\":\"/webuser/api/conference/start\",\"parent_id\":300},{\"id\":309,\"name\":\"ConferenceHup\",\"label\":\"结束会议\",\"icon\":\"\",\"path\":\"/webuser/api/conference/hup\",\"parent_id\":300},{\"id\":310,\"name\":\"ConferenceBgDial\",\"label\":\"邀请成员\",\"icon\":\"\",\"path\":\"/webuser/api/conference/bgdial\",\"parent_id\":300},{\"id\":311,\"name\":\"ConferenceMute\",\"label\":\"禁止发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/mute\",\"parent_id\":300},{\"id\":312,\"name\":\"ConferenceUnMute\",\"label\":\"允许发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unmute\",\"parent_id\":300},{\"id\":313,\"name\":\"ConferenceTmute\",\"label\":\"自动切换禁止及允许发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/tmute\",\"parent_id\":300},{\"id\":314,\"name\":\"ConferenceVmute\",\"label\":\"关闭视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vmute\",\"parent_id\":300},{\"id\":315,\"name\":\"ConferenceUnVmute\",\"label\":\"开启视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unvmute\",\"parent_id\":300},{\"id\":316,\"name\":\"ConferenceTvmute\",\"label\":\"自动切换关闭及开启视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/tvmute\",\"parent_id\":300},{\"id\":317,\"name\":\"ConferenceDeaf\",\"label\":\"开启禁音\",\"icon\":\"\",\"path\":\"/webuser/api/conference/deaf\",\"parent_id\":300},{\"id\":318,\"name\":\"ConferenceUnDeaf\",\"label\":\"关闭禁音\",\"icon\":\"\",\"path\":\"/webuser/api/conference/undeaf\",\"parent_id\":300},{\"id\":324,\"name\":\"ConferenceLock\",\"label\":\"锁定会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/lock\",\"parent_id\":300},{\"id\":325,\"name\":\"ConferenceUnLock\",\"label\":\"解锁会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unlock\",\"parent_id\":300},{\"id\":330,\"name\":\"ConferenceVidLayout\",\"label\":\"设置布局\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidlayout\",\"parent_id\":300},{\"id\":331,\"name\":\"ConferenceVidRes\",\"label\":\"设置分辨率\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidres\",\"parent_id\":300},{\"id\":332,\"name\":\"ConferenceVidFps\",\"label\":\"设置帧率\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidfps\",\"parent_id\":300},{\"id\":333,\"name\":\"ConferenceVidBandwidth\",\"label\":\"设置带宽\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidbandwidth\",\"parent_id\":300},{\"id\":334,\"name\":\"ConferenceKick\",\"label\":\"踢出成员\",\"icon\":\"\",\"path\":\"/webuser/api/conference/kick\",\"parent_id\":300},{\"id\":335,\"name\":\"ConferenceVidLayer\",\"label\":\"调整位置\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidlayer\",\"parent_id\":300},{\"id\":336,\"name\":\"ConferenceVidFloor\",\"label\":\"设置主屏\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidfloor\",\"parent_id\":300},{\"id\":337,\"name\":\"ConferenceCdrList\",\"label\":\"获取会议室记录列表\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/list\",\"parent_id\":300},{\"id\":338,\"name\":\"ConferenceCdrDetail\",\"label\":\"获取会议室记录详情\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/detail\",\"parent_id\":300},{\"id\":339,\"name\":\"ConferenceCdrQuery\",\"label\":\"查询会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/query\",\"parent_id\":300},{\"id\":340,\"name\":\"ConferenceCdrDelete\",\"label\":\"删除会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/delete\",\"parent_id\":300},{\"id\":341,\"name\":\"ConferenceCdrDelete\",\"label\":\"删除所有会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/delall\",\"parent_id\":300},{\"id\":342,\"name\":\"ConferenceRecording\",\"label\":\"下载会议录音录像\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/downloadrecording\",\"parent_id\":300},{\"id\":500,\"name\":\"Txl\",\"label\":\"通讯录\",\"icon\":\"Notebook\",\"path\":\"/webuser/api/txl\",\"parent_id\":0},{\"id\":501,\"name\":\"TxlList\",\"label\":\"获取通讯录列表\",\"icon\":\"\",\"path\":\"/webuser/api/txl/list\",\"parent_id\":500},{\"id\":502,\"name\":\"TxlCreate\",\"label\":\"添加通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/create\",\"parent_id\":500},{\"id\":503,\"name\":\"TxlDelete\",\"label\":\"删除通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/delete\",\"parent_id\":500},{\"id\":504,\"name\":\"TxlUpdate\",\"label\":\"修改通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/update\",\"parent_id\":500},{\"id\":505,\"name\":\"TxlQuery\",\"label\":\"查询通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/query\",\"parent_id\":500},{\"id\":506,\"name\":\"TxlDetail\",\"label\":\"查看通讯录详情\",\"icon\":\"\",\"path\":\"/webuser/api/txl/detail\",\"parent_id\":500},{\"id\":507,\"name\":\"TxlCall\",\"label\":\"呼叫通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/call\",\"parent_id\":500},{\"id\":600,\"name\":\"Cdr\",\"label\":\"CDR\",\"icon\":\"Tickets\",\"path\":\"/webuser/api/cdr\",\"parent_id\":0},{\"id\":601,\"name\":\"CdrList\",\"label\":\"获取分机记录列表\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/list\",\"parent_id\":600},{\"id\":602,\"name\":\"CdrDelete\",\"label\":\"删除分机通话记录\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/delete\",\"parent_id\":600},{\"id\":603,\"name\":\"CdrQuery\",\"label\":\"查询分机通话记录\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/query\",\"parent_id\":600},{\"id\":604,\"name\":\"CdrDownloadRecording\",\"label\":\"下载分机通话录音录像\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/downloadrecording\",\"parent_id\":600},{\"id\":700,\"name\":\"Setting\",\"label\":\"设置\",\"icon\":\"Setting\",\"path\":\"/webuser/api/setting\",\"parent_id\":0},{\"id\":701,\"name\":\"SettingUpdate\",\"label\":\"修改设置\",\"icon\":\"\",\"path\":\"/webuser/api/setting/update\",\"parent_id\":700}]}";

        //                string data = "{\"number\":" + number + ",\"password\":" + password + ",\"name\":\"user1\",\"department_id\":1,\"context_id\":1,\"callgroup\":1,\"alias_numbers\":\"\",\"call_timeout\":60,\"sched_hangup\":7200,\"video_bandwidth\":2,\"jitterbuffer\":0,\"limit_max\":1,\"tx_vol\":0,\"rx_vol\":0,\"cid_type\":\"number\",\"absolute_codec_string\":[],\"media_processing\":\"default\",\"playback_filename\":\"aaa.wav\",\"ringback_filename\":\"bbb.wav\",\"record_format\":\"false\",\"enable_file_write_buffering\":false,\"record_stereo\":true,\"media_bug_answer_req\":true,\"record_min_sec\":1,\"rtmp_server\":\"\",\"minimum_session_expires\":120,\"sip_force_expires\":1800,\"sip_expires_max_deviation\":600,\"cidr\":\"\",\"allow_empty_password\":false,\"sip_allow_register\":true,\"zrtp_secure_media\":false,\"srtp_secure_media\":false,\"direct_tx_number\":\"\",\"noanswer_tx_number\":\"\",\"busy_tx_number\":\"\",\"offline_tx_number\":\"\",\"vm_enabled\":false,\"vm_password\":\"12345\",\"skip_greeting\":false,\"skip_instructions\":false,\"vm_email_all_messages\":true,\"vm_mailto\":\"jjjj@aa.cn\",\"vm_attach_file\":true,\"vm_keep_local_after_email\":true,\"cash\":999999999,\"autoanswer\":false,\"ringtone_sound\":\"ringin.wav\",\"redirect_method\":\"blank\",\"redirect_url\":\"http://192.168.0.100\",\"ice_server\":\"\",\"ice_username\":\"\",\"ice_password\":\"\",\"webuser_permissions\":[{\"id\":100,\"name\":\"Call\",\"label\":\"呼叫\",\"icon\":\"Phone\",\"path\":\"/webuser/api/call\",\"parent_id\":0},{\"id\":101,\"name\":\"CallTransfer\",\"label\":\"呼叫转移\",\"icon\":\"\",\"path\":\"/webuser/api/call/transfer\",\"parent_id\":100},{\"id\":200,\"name\":\"Dispatch\",\"label\":\"调度\",\"icon\":\"Management\",\"path\":\"/webuser/api/dispatch\",\"parent_id\":0},{\"id\":201,\"name\":\"DispatchVoice\",\"label\":\"语音呼叫\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/voice\",\"parent_id\":200},{\"id\":202,\"name\":\"DispatchVideo\",\"label\":\"视频呼叫\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/video\",\"parent_id\":200},{\"id\":204,\"name\":\"DispatchSpy\",\"label\":\"监听通话\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/spy\",\"parent_id\":200},{\"id\":207,\"name\":\"DispatchHup\",\"label\":\"强挂通话\",\"icon\":\"\",\"path\":\"/webuser/api/dispatch/hup\",\"parent_id\":200},{\"id\":300,\"name\":\"Conference\",\"label\":\"会议\",\"icon\":\"Monitor\",\"path\":\"/webuser/api/conference\",\"parent_id\":0},{\"id\":301,\"name\":\"ConferenceList\",\"label\":\"获取会议室列表\",\"icon\":\"\",\"path\":\"/webuser/api/conference/list\",\"parent_id\":300},{\"id\":302,\"name\":\"ConferenceDetail\",\"label\":\"获取会议室详情\",\"icon\":\"\",\"path\":\"/webuser/api/conference/detail\",\"parent_id\":300},{\"id\":303,\"name\":\"ConferenceQuery\",\"label\":\"查询会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/query\",\"parent_id\":300},{\"id\":304,\"name\":\"ConferenceCreate\",\"label\":\"添加会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/create\",\"parent_id\":300},{\"id\":305,\"name\":\"ConferenceUpdate\",\"label\":\"修改会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/update\",\"parent_id\":300},{\"id\":306,\"name\":\"ConferenceDelete\",\"label\":\"删除会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/delete\",\"parent_id\":300},{\"id\":308,\"name\":\"ConferenceStart\",\"label\":\"开始会议\",\"icon\":\"\",\"path\":\"/webuser/api/conference/start\",\"parent_id\":300},{\"id\":309,\"name\":\"ConferenceHup\",\"label\":\"结束会议\",\"icon\":\"\",\"path\":\"/webuser/api/conference/hup\",\"parent_id\":300},{\"id\":310,\"name\":\"ConferenceBgDial\",\"label\":\"邀请成员\",\"icon\":\"\",\"path\":\"/webuser/api/conference/bgdial\",\"parent_id\":300},{\"id\":311,\"name\":\"ConferenceMute\",\"label\":\"禁止发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/mute\",\"parent_id\":300},{\"id\":312,\"name\":\"ConferenceUnMute\",\"label\":\"允许发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unmute\",\"parent_id\":300},{\"id\":313,\"name\":\"ConferenceTmute\",\"label\":\"自动切换禁止及允许发言\",\"icon\":\"\",\"path\":\"/webuser/api/conference/tmute\",\"parent_id\":300},{\"id\":314,\"name\":\"ConferenceVmute\",\"label\":\"关闭视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vmute\",\"parent_id\":300},{\"id\":315,\"name\":\"ConferenceUnVmute\",\"label\":\"开启视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unvmute\",\"parent_id\":300},{\"id\":316,\"name\":\"ConferenceTvmute\",\"label\":\"自动切换关闭及开启视频\",\"icon\":\"\",\"path\":\"/webuser/api/conference/tvmute\",\"parent_id\":300},{\"id\":317,\"name\":\"ConferenceDeaf\",\"label\":\"开启禁音\",\"icon\":\"\",\"path\":\"/webuser/api/conference/deaf\",\"parent_id\":300},{\"id\":318,\"name\":\"ConferenceUnDeaf\",\"label\":\"关闭禁音\",\"icon\":\"\",\"path\":\"/webuser/api/conference/undeaf\",\"parent_id\":300},{\"id\":324,\"name\":\"ConferenceLock\",\"label\":\"锁定会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/lock\",\"parent_id\":300},{\"id\":325,\"name\":\"ConferenceUnLock\",\"label\":\"解锁会议室\",\"icon\":\"\",\"path\":\"/webuser/api/conference/unlock\",\"parent_id\":300},{\"id\":330,\"name\":\"ConferenceVidLayout\",\"label\":\"设置布局\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidlayout\",\"parent_id\":300},{\"id\":331,\"name\":\"ConferenceVidRes\",\"label\":\"设置分辨率\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidres\",\"parent_id\":300},{\"id\":332,\"name\":\"ConferenceVidFps\",\"label\":\"设置帧率\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidfps\",\"parent_id\":300},{\"id\":333,\"name\":\"ConferenceVidBandwidth\",\"label\":\"设置带宽\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidbandwidth\",\"parent_id\":300},{\"id\":334,\"name\":\"ConferenceKick\",\"label\":\"踢出成员\",\"icon\":\"\",\"path\":\"/webuser/api/conference/kick\",\"parent_id\":300},{\"id\":335,\"name\":\"ConferenceVidLayer\",\"label\":\"调整位置\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidlayer\",\"parent_id\":300},{\"id\":336,\"name\":\"ConferenceVidFloor\",\"label\":\"设置主屏\",\"icon\":\"\",\"path\":\"/webuser/api/conference/vidfloor\",\"parent_id\":300},{\"id\":337,\"name\":\"ConferenceCdrList\",\"label\":\"获取会议室记录列表\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/list\",\"parent_id\":300},{\"id\":338,\"name\":\"ConferenceCdrDetail\",\"label\":\"获取会议室记录详情\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/detail\",\"parent_id\":300},{\"id\":339,\"name\":\"ConferenceCdrQuery\",\"label\":\"查询会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/query\",\"parent_id\":300},{\"id\":340,\"name\":\"ConferenceCdrDelete\",\"label\":\"删除会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/delete\",\"parent_id\":300},{\"id\":341,\"name\":\"ConferenceCdrDelete\",\"label\":\"删除所有会议室记录\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/delall\",\"parent_id\":300},{\"id\":342,\"name\":\"ConferenceRecording\",\"label\":\"下载会议录音录像\",\"icon\":\"\",\"path\":\"/webuser/api/conferencecdr/downloadrecording\",\"parent_id\":300},{\"id\":500,\"name\":\"Txl\",\"label\":\"通讯录\",\"icon\":\"Notebook\",\"path\":\"/webuser/api/txl\",\"parent_id\":0},{\"id\":501,\"name\":\"TxlList\",\"label\":\"获取通讯录列表\",\"icon\":\"\",\"path\":\"/webuser/api/txl/list\",\"parent_id\":500},{\"id\":502,\"name\":\"TxlCreate\",\"label\":\"添加通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/create\",\"parent_id\":500},{\"id\":503,\"name\":\"TxlDelete\",\"label\":\"删除通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/delete\",\"parent_id\":500},{\"id\":504,\"name\":\"TxlUpdate\",\"label\":\"修改通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/update\",\"parent_id\":500},{\"id\":505,\"name\":\"TxlQuery\",\"label\":\"查询通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/query\",\"parent_id\":500},{\"id\":506,\"name\":\"TxlDetail\",\"label\":\"查看通讯录详情\",\"icon\":\"\",\"path\":\"/webuser/api/txl/detail\",\"parent_id\":500},{\"id\":507,\"name\":\"TxlCall\",\"label\":\"呼叫通讯录\",\"icon\":\"\",\"path\":\"/webuser/api/txl/call\",\"parent_id\":500},{\"id\":600,\"name\":\"Cdr\",\"label\":\"CDR\",\"icon\":\"Tickets\",\"path\":\"/webuser/api/cdr\",\"parent_id\":0},{\"id\":601,\"name\":\"CdrList\",\"label\":\"获取分机记录列表\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/list\",\"parent_id\":600},{\"id\":602,\"name\":\"CdrDelete\",\"label\":\"删除分机通话记录\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/delete\",\"parent_id\":600},{\"id\":603,\"name\":\"CdrQuery\",\"label\":\"查询分机通话记录\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/query\",\"parent_id\":600},{\"id\":604,\"name\":\"CdrDownloadRecording\",\"label\":\"下载分机通话录音录像\",\"icon\":\"\",\"path\":\"/webuser/api/cdr/downloadrecording\",\"parent_id\":600},{\"id\":700,\"name\":\"Setting\",\"label\":\"设置\",\"icon\":\"Setting\",\"path\":\"/webuser/api/setting\",\"parent_id\":0},{\"id\":701,\"name\":\"SettingUpdate\",\"label\":\"修改设置\",\"icon\":\"\",\"path\":\"/webuser/api/setting/update\",\"parent_id\":700}]}";

        //                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
        //                string token = getToken();
        //                // 添加Authorization头
        //                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", getToken());

        //                // 发送请求
        //                var response = httpClient.SendAsync(request).Result;

        //                // 输出响应内容
        //                string responseBody = response.Content.ReadAsStringAsync().Result;
        //                JObject obj = (JObject)JsonConvert.DeserializeObject(responseBody);
        //                return obj;
        //            }

        //        }




        //        /// <summary>
        //        /// 获取token
        //        /// </summary>
        //        /// <returns></returns>
        //        public static string getToken()
        //        {
        //            var url = "http://121.196.245.215/admintor/api/login"; // 替换为你的API接口URL
        //            var data = "{\"username\":\"admin\",\"password\":\"admin\"}"; // 替换为你要发送的数据
        //            string result = PostResponse(url, data, null);
        //            JObject jo = (JObject)JsonConvert.DeserializeObject(result);
        //            string token = jo["data"]["token"].ToString();
        //            return token;

        //        }
        //        /// <summary>
        //        /// post请求
        //        /// </summary>
        //        /// <param name="url"></param>
        //        /// <param name="postData">post数据</param>
        //        /// <returns></returns>
        //        public static string PostResponse(string url, string postData, string token)
        //        {

        //            string result = "";
        //            if (url.StartsWith("https"))
        //                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

        //            HttpContent httpContent = new StringContent(postData);
        //            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        //            ////if(token != null)
        //            ////{
        //            ////    httpContent.DefaultRequestHeaders.Authorization =
        //            ////  new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        //            ////    httpContent.Headers.Add("Authorization", "Bearer "+token);
        //            ////}
        //            httpContent.Headers.ContentType.CharSet = "utf-8";

        //            HttpClient httpClient = new HttpClient();
        //            //httpClient..setParameter(HttpMethodParams.HTTP_CONTENT_CHARSET, "utf-8");

        //            HttpResponseMessage response = httpClient.PostAsync(url, httpContent).Result;

        //            //  statusCode = response.StatusCode.ToString();
        //            if (response.IsSuccessStatusCode)
        //            {
        //                result = response.Content.ReadAsStringAsync().Result;
        //                return result;
        //            }

        //            return result;
        //        }
        //        /// <summary>
        //        /// 参数转换
        //        /// </summary>
        //        /// <param name="query"></param>
        //        /// <returns></returns>
        //        public static NameValueCollection ParseQueryString(string query)
        //        {
        //            var result = new NameValueCollection();
        //            if (string.IsNullOrEmpty(query))
        //            {
        //                return result;
        //            }

        //            var bytes = Encoding.UTF8.GetBytes(query);
        //            for (int i = 0; i < bytes.Length; i++)
        //            {
        //                var j = i;
        //                while (j < bytes.Length && bytes[j] != '=')
        //                {
        //                    j++;
        //                }

        //                if (j < bytes.Length)
        //                {
        //                    var key = Encoding.UTF8.GetString(bytes, i, j - i);
        //                    i = j + 1;
        //                    j = i;
        //                    while (j < bytes.Length && bytes[j] != '&')
        //                    {
        //                        j++;
        //                    }

        //                    var value = Encoding.UTF8.GetString(bytes, i, j - i);
        //                    result.Add(key, value);
        //                    i = j + 1;
        //                }
        //            }

        //            return result;
        //        }
    }
}
