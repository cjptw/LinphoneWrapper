using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinPhoneSDK
{
   public class UpdateInfo
    {
        public int id { get; set; }
        public string number { get; set; }
        public string name { get; set; }
        public string flags { get; set; }
        public int call_timeout { get; set; }
        public string record_format { get; set; }
        public string rtmp_server { get; set; }
        public Boolean enable_file_write_buffering { get; set; }
        public Boolean record_stereo { get; set; }
        public Boolean media_bug_answer_req { get; set; }
        public int record_min_sec { get; set; }
        public int[] member_ids { get; set; }
    }
}
