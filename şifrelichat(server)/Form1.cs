using System;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using System.Threading;

namespace THT_Şifreli_Chat_Programı__Server_
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Thread t;
        TcpListener dinle;
        Socket soket;
        NetworkStream ag;
        StreamReader oku;
        StreamWriter yaz;

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox7.Text=="")
            {
                textBox7.Text = "AYT";
            }
            numericUpDown2.ReadOnly = true;
            textBox7.ReadOnly = true;
            dinle = new TcpListener(System.Net.IPAddress.Any, Convert.ToInt32(numericUpDown2.Value));
            dinle.Start();
            t = new Thread(new ThreadStart(okumayabasla));
            t.Start();
        }

        private void okumayabasla()
        {
            soket = dinle.AcceptSocket();
            ag = new NetworkStream(soket);
            oku = new StreamReader(ag);
            while (true)
            {
                try
                {
                    string yazi = oku.ReadLine();
                    ekranabas(yazi);
                }
                catch
                { return; }
            }

        }
        public delegate void ricdegis(string text);
        private void ekranabas(string s)
        {
            if (s.StartsWith("/get%$"))
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    notifyIcon1.ShowBalloonTip(3);
                }
                s = Crypto.SifreyiCozAES(s.Substring(6),textBox7.Text);
            }
            if (this.InvokeRequired)
            {
                ricdegis degis = new ricdegis(ekranabas);
                this.Invoke(degis, s);
            }
            else
            {
                listBox2.Items.Add(s );
            }
        }

        private void textBox8_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void textBox8_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (textBox8.Text == "/clear")
                {
                    textBox8.Clear();
                    listBox2.Items.Clear();
                }
                else
                {

                    if (e.KeyCode == Keys.Enter)
                    {

                        listBox2.Items.Add("Ben: " + textBox8.Text);
                        yaz = new StreamWriter(ag);
                        yaz.WriteLine("/get%$" + Crypto.SifreleAES(textBox6.Text + ": " + textBox8.Text, textBox7.Text));
                        yaz.Flush();
                        textBox8.Text = "";
                    }
                }
            }
            catch
            {
                MessageBox.Show("Karşıdaki Kişinin Baplantısı Kopmuştur...");
                Application.Exit();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (IPAddress adres in Dns.GetHostAddresses(Dns.GetHostName()))
            {
               comboBox1.Items.Add (""+adres);
            }
        }
    }
    public class Crypto
    {
        private static byte[] _salt = Encoding.ASCII.GetBytes("o6806642kbM7c5");
        public static string SifreleAES(string plainText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");

            string outStr = null;
            RijndaelManaged aesAlg = null;

            try
            {

                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);
                aesAlg = new RijndaelManaged();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);


                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);


                using (MemoryStream msEncrypt = new MemoryStream())
                {

                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    outStr = Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
            finally
            {

                if (aesAlg != null)
                    aesAlg.Clear();
            }


            return outStr;
        }
        public static string SifreyiCozAES(string cipherText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException("cipherText");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");


            RijndaelManaged aesAlg = null;


            string plaintext = null;

            try
            {

                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);


                byte[] bytes = Convert.FromBase64String(cipherText);
                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {

                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    aesAlg.IV = ReadByteArray(msDecrypt);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                            //uzmanim.net
                            plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
            finally
            {

                if (aesAlg != null)
                    aesAlg.Clear();
            }

            return plaintext;
        }
        private static byte[] ReadByteArray(Stream s)
        {
            byte[] rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
            {
                throw new SystemException("Stream did not contain properly formatted byte array");
            }

            byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new SystemException("Did not read byte array properly");
            }

            return buffer;
        }
    }
}
