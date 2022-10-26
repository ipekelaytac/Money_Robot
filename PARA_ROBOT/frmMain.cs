using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Collections;
using System.Security.Policy;
using System.IO;

namespace PARA_ROBOT
{
    public partial class frmMain : Form
    {
        public tools myTools = new tools();
        public DAL myDal = new DAL();
        public string constr = "";
        ArrayList ParaListesi = new ArrayList();
        ArrayList UrlListesi = new ArrayList();
        ArrayList indirilenler = new ArrayList();
        ArrayList idler = new ArrayList();
        string sontarih = "";
        string sonpara = "";
        string simdikitarih = "";
        public frmMain()
        {
            InitializeComponent();
        }
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        private static double ConvertDateTimeToTimestamp(DateTime value)
        {
            TimeSpan epoch = (value - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
            //return the total seconds (which is a UNIX timestamp)
            return (double)epoch.TotalSeconds;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            myDal.frmMain = this;
            myTools.frmMain = this;
            myTools.logWriter("Robot Başladı");
            constr = System.Configuration.ConfigurationSettings.AppSettings["con"].ToString();
            myDal.OpenSQLConnection(constr, myDal.myConnection);
            timer1.Interval = 3000;
            timer1.Start();
        }
        private void SonDataGetir()
        {
            string sql = "select top 1 tarih, kisaltma from DATA_GUNLUK_PARA t1 inner join para t2 on t2.id=t1.para_id order by t1.id desc";
            SqlDataReader oku3 = myDal.CommandExecuteSQLReader(sql, myDal.myConnection);
            while (oku3.Read())
            {
                sontarih = oku3[0].ToString();
                var parcalar = sontarih.Split('.');
                DateTime dt = new DateTime(Convert.ToInt32(parcalar[2].Substring(0, 4)), Convert.ToInt32(parcalar[1]), Convert.ToInt32(parcalar[0]), 0, 0, 0);
                sontarih = ConvertDateTimeToTimestamp(dt).ToString();
                sonpara = oku3[1].ToString();
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            var parca = ConvertDateTimeToTimestamp(DateTime.UtcNow).ToString().Split(',');
            simdikitarih = parca[0];
            timer1.Stop();
            SonDataGetir();
            ParaCek();
            UrlListe();
            BorsaIndir();
            indirilenOku();
            idDoldur();
            CsvOku();
        }
        public void UrlListe()
        {
            int sayac = 0;
            for (int i = 0; i < ParaListesi.Count; i++)
            {
                if (sayac == 0)
                {
                    if (sonpara != "")
                    {
                        i = ParaListesi.IndexOf("sdfdsfds") - 1;
                        if (i < 0)
                        {
                            i = -1;
                        }
                    }
                    i = -1;
                    sayac++;
                }
                else
                {
                    if (sontarih == "")
                    {
                        sontarih = "1104710400";
                    }
                    if (ParaListesi[i].ToString() == sonpara)
                    {
                        string url = "https://query1.finance.yahoo.com/v7/finance/download/" + ParaListesi[i] + "USD=X?period1=" + sontarih + "&period2=" + simdikitarih + "&interval=1d&events=history";
                        UrlListesi.Add(url);
                        myTools.logWriter("Url eklendi: " + url);
                        Application.DoEvents();
                    }
                    else
                    {
                        string url = "https://query1.finance.yahoo.com/v7/finance/download/" + ParaListesi[i] + "USD=X?period1=1104710400&period2=" + simdikitarih + "&interval=1d&events=history";
                        UrlListesi.Add(url);
                        myTools.logWriter("Url eklendi: " + url);
                        Application.DoEvents();
                    }
                }
            }
        }
        public void ParaCek()
        {
            string sql = "select id, kisaltma from para where ad != 'USD' and cekildi = 0";
            SqlDataReader oku3 = myDal.CommandExecuteSQLReader(sql, myDal.myConnection);
            while (oku3.Read())
            {
                myTools.logWriter("Para çekiliyor: " + oku3[0].ToString());
                ParaListesi.Add(oku3[1]);
            }
        }
        public void BorsaIndir()
        {
            for (int i = 0; i < UrlListesi.Count; i++)
            {
                myTools.DownloadFile(UrlListesi[i].ToString(), "Datalar\\", ParaListesi[i].ToString() + ".csv");
                myTools.logWriter("Data indilirildi: " + i);
                Application.DoEvents();
            }
        }
        public void indirilenOku()
        {
            for (int i = 0; i < ParaListesi.Count; i++)
            {
                string dosya_dizini = AppDomain.CurrentDomain.BaseDirectory.ToString() + "Datalar\\" + ParaListesi[i] + ".csv";
                var dosyaAdi = dosya_dizini.Split('\\');
                if (File.Exists(dosya_dizini) == true) // dizindeki dosya var mı ?
                {
                    indirilenler.Add(dosyaAdi.Last());
                    myTools.logWriter("İndirildi: " + dosyaAdi.Last());
                }
                Application.DoEvents();
            }
        }
        public void idDoldur()
        {
            string sql = "select id from para where ad != 'USD'";
            for (int i = 0; i < indirilenler.Count; i++)
            {
                var ad = indirilenler[i].ToString().Split('.');
                if (i == 0)
                {
                    sql += $" and kisaltma= '{ad[0]}'";
                }
                else
                {
                    sql += $" or kisaltma= '{ad[0]}'";
                }
                Application.DoEvents();
            }
            SqlDataReader oku3 = myDal.CommandExecuteSQLReader(sql, myDal.myConnection);
            while (oku3.Read())
            {
                idler.Add(oku3[0]);
            }
        }
        public void CsvOku()
        {
            for (int i = 0; i < indirilenler.Count; i++)
            {
                string[] satirlar = System.IO.File.ReadAllLines("Datalar\\" + indirilenler[i]);
                for (int j = 1; j < satirlar.Length; j++)
                {
                    if (!satirlar[j].Contains("null"))
                    {
                        var satirParcala = satirlar[j].Split(',');
                        myDal.CommandExecuteNonQuery($"insert DATA_GUNLUK_PARA values('{satirParcala[0]}', '{idler[i]}', '{satirParcala[4]}')", myDal.myConnection);
                        myTools.logWriter("Veri eklendi: " + indirilenler[i] + " " + satirParcala[0]);
                        Application.DoEvents();
                    }
                }
                var parca = indirilenler[i].ToString().Split('.');
                myDal.CommandExecuteNonQuery($"update para set cekildi=1 where kisaltma = '{parca[0]}'", myDal.myConnection);
            }
        }
    }
}
