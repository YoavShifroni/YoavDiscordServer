using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    [Serializable]
    public class AesKeys
    {
        /// <summary>
        /// Byte's array that represent the AES key
        /// </summary>
        public byte[] Key { get; set; }

        /// <summary>
        /// Byte's array that represent the AES iv
        /// </summary>
        public byte[] Iv { get; set; }


    }
}
