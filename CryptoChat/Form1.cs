using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoChat
{
    public partial class Form1 : Form
    {
        bool processChat = false;
        login lgWin;
        string userName;
        string anotherUser;
        TcpListener listener = null;
        TcpClient client = null;
        int timerTicks = 0;
        bool inConnect;
        bool rdyToSend = false;

        string toUpdate = "";

        public Form1(login lgn)
        {
            lgWin = lgn;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(Data.Value))
                userName = Data.Value;
            else userName = "user";
            timer1.Enabled = true;
            richTextBox1.Text = "Waiting interlocutor.";
            Thread clientThread = new Thread(new ThreadStart(StartChat));
            clientThread.Start();
        }
        //check port or start server 
        void StartChat() {
            const int port = 8888;
            const string address = "127.0.0.1";

            /* someone start chat server
             * Client PART
             */
            try {               
                client = new TcpClient(address, port);
                try {
                    NetworkStream stream = client.GetStream();

                    string usrHello = SendMsg(stream, "firstConn_" + userName);
                    if (usrHello.Contains("hello_im_")) {
                        Thread.Sleep(105);
                        inConnect = true;
                        anotherUser = usrHello.Split('_')[2];
                        processChat = true;
                        toUpdate = "rchn|Connected with <" + anotherUser + ">!\n";
                        Thread.Sleep(300);
                        bool dontCheck = false;
                        while (inConnect)
                        {
                            if (rdyToSend) {
                                string msg = SendMsg(stream, "msg|" + textBox1.Text);
                                toUpdate = "rchu|" + userName + ": " + textBox1.Text + "\n";
                                Thread.Sleep(120);
                                toUpdate = "tbc";
                                rdyToSend = false;
                                if (msg == "nth") continue;
                                else toUpdate = "rchu|" + anotherUser + ": " + msg.Split('|')[1] + "\n";
                                Thread.Sleep(120);
                            }
                            else
                            {
                                string msg = SendMsg(stream, "nth");
                                if (msg == "nth") continue;
                                else if (msg == "usr_endConn") {
                                    inConnect = false;
                                    dontCheck = true;
                                    toUpdate = "rchu|" + anotherUser + " disconnected\n";
                                }
                                else toUpdate = "rchu|" + anotherUser + ": " + msg.Split('|')[1] + "\n";
                                Thread.Sleep(120);
                            }
                            Thread.Sleep(50);
                        }
                        if (dontCheck)
                        {
                            string usrEnd = SendMsg(stream, "usr_endConn");
                        }
                        if (stream != null) stream.Close();
                    }
                }
                catch { }
            }
            //no one start server
            catch {                
                try {
                    listener = new TcpListener(IPAddress.Parse(address), port);
                    listener.Start();
                    client = listener.AcceptTcpClient();

                    Thread clientThread = new Thread(new ThreadStart(ProcessingChat));
                    clientThread.Start();
                    listener.Stop();
                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
                finally { if (listener != null) listener.Stop(); }
            }
        }

        //Server PART
        void ProcessingChat() {
            NetworkStream stream = null;
            try {
                stream = client.GetStream();
                //Buffer for inc data
                string data = "";
                bool work = true;
                while (work) {
                    data = GetMsg(stream);

                    //exchange messages
                    if (data.Contains("msg|")) {
                        toUpdate = "rchu|" + anotherUser + ": " + data.Split('|')[1] + "\n";
                        if (rdyToSend) { 
                            byte[] msg = Encoding.UTF8.GetBytes("msg|" + textBox1.Text);
                            stream.Write(msg, 0, msg.Length);
                            toUpdate = "rchu|" + userName + ": " + textBox1.Text + "\n";
                            Thread.Sleep(105);
                            toUpdate = "tbc";
                            rdyToSend = false;
                        }
                        else {
                            byte[] msg = Encoding.UTF8.GetBytes("nth");
                            stream.Write(msg, 0, msg.Length);
                        }
                    }
                    else if (data == "nth") {
                        if (rdyToSend) {
                            byte[] msg;
                            if (!inConnect) msg = Encoding.UTF8.GetBytes("usr_endConn");
                            else msg = Encoding.UTF8.GetBytes("msg|" + textBox1.Text);
                            stream.Write(msg, 0, msg.Length);
                            if (inConnect) toUpdate = "rchu|" + userName + ": " + textBox1.Text + "\n";
                            Thread.Sleep(105);
                            toUpdate = "tbc";
                            rdyToSend = false;
                        }
                        else {
                            byte[] msg;
                            if (!inConnect) msg = Encoding.UTF8.GetBytes("usr_endConn");
                            else msg = Encoding.UTF8.GetBytes("nth");
                            stream.Write(msg, 0, msg.Length);
                            if (!inConnect) work = false;
                        }
                    }
                    //first connection
                    else if(data.Contains("firstConn_")) {
                        inConnect = true;
                        anotherUser = data.Split('_')[1];
                        processChat = true;
                        toUpdate = "rchn|User <" + anotherUser + "> connected!\n";
                        Thread.Sleep(120);
                        byte[] msg = Encoding.UTF8.GetBytes("hello_im_"+ userName);
                        stream.Write(msg, 0, msg.Length);
                    }
                    //end connection
                    else if (data == "usr_endConn") {
                        byte[] msg = Encoding.UTF8.GetBytes("bye_"+ anotherUser);
                        stream.Write(msg, 0, msg.Length);
                        toUpdate = "rchu|"+anotherUser+" are disconnected\n";
                        if (stream != null) stream.Close();
                        if (client != null) client.Close();
                        if (listener != null) listener.Stop();
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                if (stream != null) stream.Close();
                if (client != null) client.Close();
            }
            finally {
                if (stream != null) stream.Close();
                if (client != null) client.Close();
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {
            if (!processChat) {
                if (timerTicks > 1000) timerTicks = 0;
                if (timerTicks % 7 == 0) richTextBox1.Text += ".";
                if (timerTicks % 56 == 0) richTextBox1.Text = "Waiting interlocutor.";
                timerTicks++;
            }
            else {
                if (toUpdate != "")
                {
                    if (toUpdate.IndexOf("rchn") == 0)
                    {
                        richTextBox1.Text = toUpdate.Split('|')[1];
                        toUpdate = "";
                    }
                    else if (toUpdate.IndexOf("rchu") == 0) 
                    {
                        richTextBox1.Text += toUpdate.Split('|')[1];
                        toUpdate = "";
                    }
                    else if (toUpdate.IndexOf("tbc") == 0)
                    {
                        textBox1.Text = "";
                        toUpdate = "";
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            rdyToSend = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (lgWin != null) lgWin.Close();
            if (listener != null) listener.Stop();
            if (client != null) client.Close();
        }

        static string SendMsg(NetworkStream stream, string msg)
        {
            // преобразуем сообщение в массив байтов
            byte[] data = Encoding.UTF8.GetBytes(msg);
            // отправка сообщения
            stream.Write(data, 0, data.Length);

            // получаем ответ
            data = new byte[5000]; // буфер для получаемых данных
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            do
            {
                bytes = stream.Read(data, 0, data.Length);
                builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                Thread.Sleep(300);
            }
            while (stream.DataAvailable);

            msg = builder.ToString();
            return msg;
        }

        string GetMsg(NetworkStream stream)
        {
            byte[] data = new byte[2048]; // буфер для получаемых данных
            // получаем сообщение
            string msg = "";
            int bytes = 0;
            bytes = stream.Read(data, 0, data.Length);
            msg = Encoding.UTF8.GetString(data, 0, bytes);
            return msg;
        }

        private void button2_Click(object sender, EventArgs e) {
            inConnect = false;
        }
    }
}
