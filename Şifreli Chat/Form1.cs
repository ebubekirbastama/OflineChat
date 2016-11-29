using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace THT_Şifreli_Chat_Programı_Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Thread t;
        NetworkStream ag;
        StreamReader oku;
        StreamWriter yaz;
        TcpClient iste;

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox7.Text == "")
            {
                textBox7.Text = "AYT";
            }
            textBox5.ReadOnly = true;
            textBox7.ReadOnly = true;
            numericUpDown2.ReadOnly = true;
            iste = new TcpClient(textBox5.Text, Convert.ToInt32(numericUpDown2.Value));
            ag = iste.GetStream();
            yaz = new StreamWriter(ag);
            t = new Thread(new ThreadStart(okumayabasla));
            t.Start();
        }
        public void okumayabasla()
        {
            oku = new StreamReader(ag);
            while (true)
            {
                try
                {
                    string yazi = oku.ReadLine();
                    ekranabas(yazi);
                }
                catch { return; }
            }

        }
        public delegate void ricdegis(string text);

        public void ekranabas(string s)
        {
            if (s.StartsWith("/get%$"))
            {
                if (this.WindowState==FormWindowState.Minimized)
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
                listBox2.Items.Add(s);
            }
        }

        private void textBox8_KeyDown(object sender, KeyEventArgs e)
        {
            
            try
            {
                if (textBox8.Text == "/clear")
                {
                    listBox2.Items.Clear();
                    textBox8.Clear();
                }
                else
                {
if (e.KeyValue == 13) // enter e basılınca
            {
                listBox2.Items.Add("Ben: " + textBox8.Text);
                yaz = new StreamWriter(ag);
                yaz.WriteLine("/get%$"+Crypto.SifreleAES(textBox6.Text + ": " + textBox8.Text,textBox7.Text));
                yaz.Flush();
                textBox8.Clear();
            }
                }

            }
            catch {
                MessageBox.Show("Karşıdaki Kişinin Bağlantısı Kopmuştur");
                Application.Exit();
            }
        }
    }
    public class Crypto
    {
        private static byte[] _salt = Encoding.ASCII.GetBytes("o6806642kbM7c5");

        /// <summary>
        /// uzmanim.net Verilen string ifadeyi AES kullanarak şifreler. Şifrelenen metin        
        /// uzmanim.net DecryptStringAES() kullanılarak şifresi çözülebilir. 
        /// 
        /// </summary>
        /// <param name="plainText">şifrelenecek metin</param>
        /// <param name="sharedSecret">şifreleme için kullanılacak parola</param>
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
        /// <summary>
        /// EncryptStringAES() ile şifrelenen metnin şifresini çözer.        
        /// <param name="cipherText">Deşifre edilecek metin uzmanim.net</param>
        /// <param name="sharedSecret">Paylaşılan gizli anahtar</param>
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
