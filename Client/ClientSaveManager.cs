using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SharedProject.FastDebug;

namespace ClientSpace
{
    internal class ClientSaveManager
    {
        const string MessagesFolder = "Messages";
        public static ClientSaveManager Instance = new();
        public ClientSaveManager() { }
        private bool startCalled = false;

        public void Start()
        {
            if (!startCalled)
            {
                start();
                startCalled = true;
            }
        }

        private void start()
        {
            p("not implemented metod. client save manage start function is not finished.");
            if (!Directory.Exists(MessagesFolder))
                Directory.CreateDirectory(MessagesFolder);
            foreach (var item in Directory.EnumerateFiles(MessagesFolder))
            {
                p(Path.GetFileNameWithoutExtension(item));
            }
        }
    }
}
