using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Youtube
{
    class AuthVO
    {
        public string ip { set; get; }        
        public string username { set; get; }
        public string password { set; get; }
        public string memo { set; get; }
        internal string getMophnNo()
        {
            return username.Substring(2, username.Length - 2);
        }
    }
}
