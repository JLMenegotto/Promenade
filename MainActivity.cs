using System;
using System.Linq;
using System.Net;
using System.IO;
using static System.Net.WebRequestMethods;
using System.Collections.Generic;
using System.Xml;
using Stream = System.IO.Stream;

using Android.Accounts;
using Android.App;
using Android.Locations;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Media;
using Android.Graphics.Drawables;
using Android.Media.Midi;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Android.Hardware;
using System.Threading.Tasks;
using Java.Util;
using Xamarin.Essentials;

using TextToSpeech = Xamarin.Essentials.TextToSpeech;
using Geolocation  = Xamarin.Essentials.Geolocation;

using ScanMode = Android.Bluetooth.LE.ScanMode;
using Context  = Android.Content.Context;
using System.Runtime;
using System.Net.Mail;
using System.Runtime.Remoting.Contexts;

using Environment = Android.OS.Environment;
using Uri         = Android.Net.Uri;
using File        = Java.IO.File;

namespace Promenade_2019
{
    public class BES         
    {
        public int                                   pulso = 125;
        private readonly Dictionary<string, beacon> _btd   = new Dictionary<string, beacon>();
        private readonly BluetoothAdapter           _btadapter;
        private readonly ScanCallback               _btcallback;
        public List<ScanFilter>                     _filt;
        public ScanSettings                         _sets;
        public string                               _error;
        public struct beacon                                   
        {
                  public BluetoothDevice btd;
                  public string nome;
                  public string mac;
                  public int    rssi;
                  public string uuid;
                  public string taxa;
                  public string servicios;
                  public double distancia;
                  public string classe;
                  public string bond;
                  public string pac;
        }
        public struct Localiza                                 
        {
                  public string Mac;
                  public string Bloco;
                  public string Andar;
                  public string X;
                  public string Y;
                  public string Noco;
                  public string Nuco;
                  public string Pd;
                  public string Area;
                  public string Midi;
                  public string Foto;
        }
        public BES ( )                                         
        {
                   var btManager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);
                   _btadapter    = btManager.Adapter;
                   _filt         = Pega_filtros();
                   _sets         = new ScanSettings.Builder().SetScanMode(ScanMode.LowLatency).Build();
                   _btcallback   = new BLECallback(_btd, _filt, _sets);
                   _error        = "-";
        }
        public       List<ScanFilter>          Pega_filtros( ) 
        {
                        List<ScanFilter> filtros = new List<ScanFilter> { };
                        string[] nomes = new string[] { "OnyxBeacon", "RqBleLight", "PlaybulbX", "Playbulbx" };
                        for (int i = 0; i < nomes.Length; i++)
                        {
                                          ScanFilter filtro = new ScanFilter.Builder().SetDeviceName(nomes[i]).Build();
                                          filtros.Add(filtro);
                        }
                 return filtros;
        }
        public async Task<IEnumerable<beacon>> Scan_Beacons( ) 
        {
                         if (!_btadapter.IsEnabled)
                         {  return null; }
                         else
                         {
                                 List<ScanFilter> sf = null;              // pega todos os sinais
                                 //List<ScanFilter> sf = Pega_filtros();  // pega apenas sinais filtrados
                                 _btadapter.BluetoothLeScanner.StartScan(sf, _sets, _btcallback);
                                 await Task.Delay(pulso);
                                 return _btd.Values;
                         }
        }
        public       Task<IEnumerable<beacon>> Stop_Beacons( ) 
        {
                            _btadapter.BluetoothLeScanner.StopScan(_btcallback);
                            return null;
        }
    }
    public class BLECallback : ScanCallback
    {
        public Dictionary<string, BES.beacon> bec;
        public List<ScanFilter>               filt;
        public ScanSettings                   sets;
        public string                         fail;
        public               BLECallback  ( Dictionary<string, BES.beacon> b, List<ScanFilter> f, ScanSettings s) 
        {
                             bec = b;
                             filt = f;
                             sets = s;
                             fail = "-";
        }
        public override void OnScanResult ( ScanCallbackType callbackType, ScanResult res)                        
        {
                             base.OnScanResult(callbackType, res);
                             BES.beacon      dad = new BES.beacon();
                             BluetoothDevice btd = res.Device;
                             ScanRecord      rec = res.ScanRecord;

                             string mac = btd.Address;
                             string nom = btd.Name;
                             string cla = btd.BluetoothClass.ToString();
                             string bon = btd.BondState.ToString();
                             int    rss = res.Rssi;
                             string uui = "";
                             int    tax = 3;

                             IDictionary<ParcelUuid, byte[]> sda;
                             if (nom == "OnyxBeacon")
                             {
                                   sda = rec.ServiceData;
                                   uui = Retorna_UUID(sda).ToString();
                             }
                             tax           = rec.TxPowerLevel;
                             dad.distancia = Calcula_Dist(rss, tax);
                             dad.btd       = btd;
                             dad.nome      = nom;
                             dad.mac       = mac;
                             dad.rssi      = rss;
                             dad.taxa      = tax.ToString();
                             dad.uuid      = uui;
                             dad.classe    = cla;
                             dad.bond      = bon;
                             bec[mac]      = dad;
        }
       
        public override void OnScanFailed ( ScanFailure falla)                                                    
        {
                             fail = falla.ToString();
        }
        public UUID          Retorna_UUID ( IDictionary<ParcelUuid, byte[]> dic)                                  
        {
                             UUID u = null;
                             foreach (var pair in dic)
                             {
                                    u = pair.Key.Uuid;
                             }
                             return u;
        }
        public double        Calcula_Dist ( int rssi, int txPw)                                                   
        {
                             int taxapw = -20;
                             switch (txPw)
                             { 
                                case 0:  taxapw = -30; break;
                                case 1:  taxapw = -20; break;
                                case 2:  taxapw = -16; break;
                                case 3:  taxapw = -12; break;
                                case 4:  taxapw =  -8; break;
                                case 5:  taxapw =  -4; break;
                                case 6:  taxapw =   0; break;
                                case 7:  taxapw =   4; break;
                                default: taxapw = -20; break;
                             }
                             return Math.Round(Math.Pow(10d, ((double)taxapw - rssi) / (10 * 2)) / 100 , 2);
        }
    }

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ISensorEventListener
    {
        static string Raiz_Gal  = Android.OS.Environment.ExternalStorageDirectory.Path;   //Pasta raiz do cartão interno do telefone
        static string Raiz_Loc  = System.IO.Path.Combine( Raiz_Gal , "Documents/Promenade2019/");
        static string Raiz_Web  = "https://sites.google.com/a/poli.ufrj.br/jose-luis-menegotto/mapas/";

        ImageView vwima;
        ImageView vwfot;
        Button    btini;
        Button    btfin;
        Button    btxml;
        TextView  txcro;
        TextView  txbea;
        TextView  txfot;
        BES       btes;

        Color corazul = Color.Argb ( 255,  33 , 150, 243);
        Color corroja = Color.Argb ( 255, 211 ,  47,  47);
        Color corverd = Color.Argb ( 255,  76 , 175,  80);

        Bitmap        imagen;
        Canvas        canvas;
        MediaPlayer   player;
        SpeechOptions voz;

        string Latitude  = "-";
        string Longitud  = "-";
        string Altitude  = "-";
        double cobertura = 40; // raio de cobertura da procura

        string[][] MapasPNG = new string[][]  {   new string[] {  "planta_ufrj.png" , "F_00.png"  },
                                                  new string[] {  "planta_ct.png"   , "F_00.png"  }};
        string[]   MapasXML = new string[]    { "Mapa_Promenade_2019.xml" , "Rio_310.XML" , "Rio_500.XML", "Rio_782.XML", "Rio_792.XML" };

        IEnumerable<BES.beacon> bec_Lbruta;
        List<BES.beacon>        bec_LOrden;
        string bec_inform;
        string captura;
        bool   escanea;
        int    scan;

        protected override void OnCreate(Bundle estado)
        {
              base.OnCreate(estado);
              Xamarin.Essentials.Platform.Init(this, estado);
              SetContentView    (Resource.Layout.activity_main);

              vwima = FindViewById<ImageView>(Resource.Id.Mapa0);
              btini = FindViewById<Button>   (Resource.Id.Iniciar);
              btfin = FindViewById<Button>   (Resource.Id.Parar);
              btxml = FindViewById<Button>   (Resource.Id.Xml);
              txcro = FindViewById<TextView> (Resource.Id.Crono);
              txbea = FindViewById<TextView> (Resource.Id.Beacons);
              vwfot = FindViewById<ImageView>(Resource.Id.Info_Foto);
              txfot = FindViewById<TextView> (Resource.Id.Txt_Foto);

              try
              {
                    BluetoothAdapter BTAD      = BTLE_Verificar();
                    SensorManager    Sensores  = (SensorManager)GetSystemService(Context.SensorService);
                    IList<Sensor>    Lsensores = Sensores.GetSensorList(SensorType.All);
                    Sensor           S_Bus     = Sensores.GetDefaultSensor(SensorType.Orientation);

                    //Sensor S_Mag = Sensores.GetDefaultSensor(SensorType.MagneticField);
                    //Sensor S_Pro = Sensores.GetDefaultSensor(SensorType.Proximity);
                    //Sensor S_Lux = Sensores.GetDefaultSensor(SensorType.Light);

                    Sensores.RegisterListener ( this , S_Bus , SensorDelay.Normal);    // Sensor de Bussola
                    //Sensores.RegisterListener ( this , S_Pro , SensorDelay.Normal);  // Sensor de Proximity
                    //Sensores.RegisterListener ( this , S_Lux , SensorDelay.Normal);  // Sensor de Iluminamento
                    //Sensores.RegisterListener ( this , S_Mag , SensorDelay.Normal);  // Sensor de Campo Magnético

                    btini.SetBackgroundColor(corazul);
                    btfin.SetBackgroundColor(corazul);
                    btxml.SetBackgroundColor(corazul);
                    btini.Text = "ESCANEAR";
                    btfin.Text = "PARAR";
                    btxml.Text = "XML";

                    imagen = PNG_Fotos(MapasPNG[1][0] , MapasPNG[1][1] ,  vwima,  vwfot);

                    TextToSpeech.SpeakAsync("Sistema pronto.", Tts_Voz(0.25));                
                    btes = new BES();
                    txfot.Text = "\n Lat: " + Latitude +
                                 "\n Lon: " + Longitud +
                                 "\n Alt: " + Altitude;

                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(Raiz_Loc + "Mapa_Promenade_2019.xml");
                    XmlNodeList Lnodos = xmldoc.SelectNodes("/Mapa/Registro");

                    btini.Click += async delegate { await Processar_ON  ( Lnodos , escanea = true);  };
                    btfin.Click +=       delegate {       Processar_OF  (          escanea = false); };
                    btxml.Click +=       delegate {       Processar_XML ( Lnodos , escanea = false); };
              }
              catch   {                                 }
              finally { bec_inform = "Aguardando....";  }
        }

        void ISensorEventListener.OnAccuracyChanged ( Sensor s, SensorStatus accuracy )  
        {

        }
        void ISensorEventListener.OnSensorChanged   ( SensorEvent e)                     
        {
                      string s = "\n" + e.Sensor.Name;
                      if (e.Sensor.Type == SensorType.Orientation) s = s + "\nAzim: " + e.Values[0].ToString() + "\nIncl: " + e.Values[1].ToString();
                      //if (e.Sensor.Type == SensorType.MagneticField) s = "\n" + e.Sensor.Name + "\nMagX: "    + e.Values[0].ToString() + "\nMagY: "    + e.Values[1].ToString();
                      //if (e.Sensor.Type == SensorType.Proximity)     s = s + "\n" + e.Sensor.Name + "\nProximidade: " + e.Values[0].ToString();
                      //if (e.Sensor.Type == SensorType.Light)         s = s + "\n" + e.Sensor.Name + "\nLux: "         + e.Values[0].ToString();

                      txcro.Text = Crono(scan) + s;
        }

        private void Escreve_Arquivo ( string nomearquivo)
        {
            var camino  = Android.OS.Environment.ExternalStorageDirectory.Path;
            var arquivo = System.IO.Path.Combine ( camino ,  nomearquivo);

            if (!System.IO.File.Exists(arquivo))
            {
                using (System.IO.StreamWriter arqwr = new System.IO.StreamWriter(arquivo, true))
                {
                    arqwr.Write("Texto no arquivo");
                    Toast.MakeText(this, "Arquivo " + arquivo.ToString() + " OK.", ToastLength.Short).Show();
                }
            }
            else
            {
                Toast.MakeText(this, "Arquivo " + arquivo.ToString() + " já existe.", ToastLength.Short).Show();
            }
        }

        public async Task Geolocalizar ( )          
        {
                    try
                    {
                         var location = await Geolocation.GetLastKnownLocationAsync();
                         if (location != null)
                         { 
                              Latitude = String.Format("{0}",      location.Latitude);
                              Longitud = String.Format("{0}",      location.Longitude);
                              Altitude = String.Format("{0:0.00}", location.Altitude);
                         }
                    }
                    catch   {
                              Latitude = ".";
                              Longitud = ".";
                              Altitude = ".";
                            }
                    finally {
                              Latitude = "-";
                              Longitud = "-";
                              Altitude = "-";
                            }
        }

        public void Analisar_Beacon_String( ) 
        {
                                             string estrutura = "m:2-3=0215,i:4-19,i:20-21,i:22-23,p:24-24";
        }

        // Função de setagem da fala --------------------------------------------------
        public SpeechOptions Tts_Voz(double tom)
        {
                voz = new SpeechOptions()
                {
                      Volume = (float)1.00,
                      Pitch = (float)tom
                };
                return voz;
        }

        //Funções de Processamento ----------------------------------------------------
        public async Task  Processar_ON  ( XmlNodeList Lnodos , bool faz )  
        {
                     bec_LOrden = new List<BES.beacon> { };
                     btini.SetBackgroundColor(corverd); btini.Text = "ESCANEANDO...";
                     btfin.SetBackgroundColor(corazul); btfin.Text = "PAUSAR";
                     btxml.SetBackgroundColor(corazul); btxml.Text = "XML";
                     scan = 1;
                     
                     BES.beacon beacon1;
                     
                     while (escanea)
                     {
                            await Geolocalizar();
                            try
                            {
                                bec_inform = "";
                                captura    = "";
                                txcro.Text = Crono(scan);
                                bec_LOrden = new List<BES.beacon> { };
                                bec_Lbruta = await btes.Scan_Beacons();
                                bec_LOrden = bec_Lbruta.OrderByDescending(s => s.rssi).Select(x => x).ToList();
                                beacon1    = bec_LOrden[0];                             //<-- Pega 1° beacon da lista ordenada
                                BES.Localiza Posic = Verifica_Mapa( Lnodos , beacon1 ); //<-- Verifica o local do beacon no mapa XML
 
                                foreach (BES.beacon b in bec_LOrden)
                                {
                                    if (b.distancia < cobertura)
                                    {
                                          captura += b.mac + " || " + b.nome + " || " + b.distancia.ToString() + "\n"; // mostra apenas os beacons mais próximos
                                    }
                                }

                                imagen = PNG_Fotos( MapasPNG[1][0] , Posic.Foto , vwima , vwfot);

                                if (Posic.Noco != "--") { PNG_Marca(imagen, "c",         Posic, scan);  }
                                else                    { PNG_Marca(imagen, "No marcar", Posic, scan);  }

                                txbea.Text = captura;
                                scan += 1;
                                GC.Collect();                  // limpa memoria
                                GC.WaitForPendingFinalizers(); // limpa memoria
                            }
                            catch   {   bec_inform = "Não achou beacon cadastrado"; }
                            finally {
                                        txbea.Text = bec_inform;
                                        scan += 1;
                                        GC.Collect();                  // limpa memoria
                                        GC.WaitForPendingFinalizers(); // limpa memoria
                                    }
                     }
        }
        public void        Processar_OF  (                      bool faz )  
        {
                           escanea = faz;
                           btes.Stop_Beacons();
                           btini.SetBackgroundColor(corazul); btini.Text = "ESCANEAR";
                           btfin.SetBackgroundColor(corroja); btfin.Text = "PAUSADO...";
                           btxml.SetBackgroundColor(corazul); btxml.Text = "XML";
        }
        public void        Processar_XML ( XmlNodeList Lnodos,  bool faz )  
        {
                           Processar_OF( escanea = faz);
                           btini.SetBackgroundColor(corazul); btini.Text = "ESCANEAR";
                           btfin.SetBackgroundColor(corazul); btfin.Text = "PAUSAR";
                           btxml.SetBackgroundColor(corverd); btxml.Text = "XML...";
                           foreach (XmlNode nod in Lnodos)
                           {
                                    XmlAttributeCollection atts = nod.Attributes;
                                    txbea.Text +=  atts["Noco"].Value;
                           }
        }
        public BluetoothAdapter BTLE_Verificar(             )   
        {
                     Free_Permiso( );  // Libera os permisos necessários
                     BluetoothAdapter BTA = BluetoothAdapter.DefaultAdapter;

                     if (BTA == null)
                     {               Mensaje_Curto("Bluetooth não suportado.");
                                     Finish();
                                     return null;
                     }
                     else { Mensaje_Curto("Bluetooth OK."); }
                     if (!BTA.IsEnabled)
                     {
                          int REQUEST_ENABLE_BT = 1;
                          var enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                          StartActivityForResult(enableBtIntent, REQUEST_ENABLE_BT);
                          Mensaje_Curto ("Bluetooth ativado." );
                     }
                     return BTA;
        }
        

        public void             Mensaje_Curto ( string txt  )   
        { Toast.MakeText(this, txt, ToastLength.Short).Show(); }
        public void             Mensaje_Longo ( string txt  )   
        { Toast.MakeText(this, txt, ToastLength.Long).Show(); }
        public string           Crono         ( int    esc  )   
        {
                       string data = "Data: " + DateTime.Now.ToString("dd:MM:yyyy");
                       string hora = "Hora: " + DateTime.Now.ToString("HH:mm:ss");
                       string paso = "Scan: " + esc.ToString();
                       if (esc >= 0)
                              return data + " " + hora + " " + paso;
                       else   return data + " " + hora;
        }
        public int              Modulo        ( int a, int m)   
        { return a % m; }

        public BES.Localiza Verifica_Mapa( XmlNodeList Lnodos , BES.beacon bicon )                  
        {
                      BES.Localiza Posicion = new BES.Localiza();
                      Posicion.Mac   = "--";
                      Posicion.Bloco = "--";
                      Posicion.Andar = "--";
                      Posicion.X     = "--";
                      Posicion.Y     = "--";
                      Posicion.Noco  = "--";
                      Posicion.Nuco  = "--";
                      Posicion.Pd    = "--";
                      Posicion.Area  = "--";
                      Posicion.Midi  = "M_00.mid";
                      Posicion.Foto  = "F_00.png";

                      foreach (XmlNode nod in Lnodos)
                      {
                               XmlAttributeCollection atts = nod.Attributes;
                               if (atts["MAC"].Value == bicon.mac)
                               {
                                     Posicion.Mac   = atts["MAC"].Value;
                                     Posicion.Bloco = atts["Bloco"].Value;
                                     Posicion.Andar = atts["Andar"].Value;
                                     Posicion.X     = atts["X"].Value.Substring(0, 3);
                                     Posicion.Y     = atts["Y"].Value.Substring(0, 3);
                                     Posicion.Noco  = atts["Noco"].Value;
                                     Posicion.Nuco  = atts["Nuco"].Value;
                                     Posicion.Pd    = atts["Pd"].Value.Substring(0, 4);  
                                     Posicion.Area  = atts["Area"].Value.Substring(0, 6);
                                     Posicion.Midi  = atts["Midi"].Value;
                                     Posicion.Foto  = atts["Foto"].Value;
                                     break;
                               }
                      }
                      return Posicion;
        }

        private void  Saca_la_Foto ( object sender, EventArgs eventArgs )                           
        {
                      try
                      {
                           Intent disparo = new Intent(MediaStore.ActionImageCapture);
                           File arquivo = new File(Raiz_Loc, String.Format("Foto_sacada_{0}.jpg", Guid.NewGuid()));
                           disparo.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(arquivo));
                           StartActivityForResult(disparo, 0);
                           Bitmap bm = BitmapFactory.DecodeFile(Raiz_Loc + "Menegotto.png", new BitmapFactory.Options());
                           vwfot.SetImageBitmap(bm);
                      }
                      catch { }
                      finally { }
        }
        public Bitmap PNG_Fotos    ( string arq1  , string arq2 , ImageView    i1  , ImageView i2 ) 
        {
                      string arqf1 = Raiz_Loc + arq1;
                      string arqf2 = Raiz_Loc + arq2;
                      BitmapFactory.Options opcion = new BitmapFactory.Options();
                      opcion.InSampleSize = 1;
                      opcion.InMutable = true;
                      Bitmap bitmap1 = BitmapFactory.DecodeFile(arqf1, opcion);
                      Bitmap bitmap2 = BitmapFactory.DecodeFile(arqf2, opcion);
                      i1.SetImageBitmap(bitmap1);
                      i2.SetImageBitmap(bitmap2);
                      BitmapDrawable bd1 = (BitmapDrawable)i1.Drawable;
                      BitmapDrawable bd2 = (BitmapDrawable)i2.Drawable;
                      return bitmap1;
        }
        public int[]  PNG_Pixel    ( Bitmap imag                                                  ) 
        {
                      int   w  = imag.Width;
                      int   h  = imag.Height;
                      int[] pt = new int[2] { w/2 , h/2 };  // centro da imagem
                      int   A  = 0;
                      int   R  = 0;
                      int   G  = 0;
                      int   B  = 0;

                      for (int i = 0; i < w; i++)
                      {
                             pt[0] = i;
                             for (int j = 0; j < h; j++)
                             {
                                  pt[1] = j;
                                  Color p = new Color(imag.GetPixel(i, j));
                                  A = Color.GetAlphaComponent (p);
                                  R = Color.GetRedComponent   (p);
                                  G = Color.GetGreenComponent (p);
                                  B = Color.GetBlueComponent  (p);
                                  if (R > 250 && G < 3) break;  
                             }
                             if (R > 250 && G < 3) break;
                      }
                      return pt;
        }
        public Canvas PNG_Marca    ( Bitmap imag  , string form , BES.Localiza pos , int sc  )      
        {
                      if (Modulo(sc, 30) == 0 && pos.Noco != "--")
                      {
                         TextToSpeech.SpeakAsync("Você está na sala " + pos.Nuco + ". No andar " + pos.Andar, Tts_Voz(0.15));
                      }
                      string x  = pos.X;
                      string y  = pos.Y;
                      int diam  = 40;
                      float di  = Modulo (sc * 10, diam);

                      double x1 = 718 - Convert.ToDouble(x) / 1.5;
                      double y1 = 407 - Convert.ToDouble(y) / 1.5;
                      double x2 = x1 + di;
                      double y2 = y1 + di;

                      Paint pin = PNG_Pincel(new int[] { 255, 50, 50 }, sc);
                      canvas = new Canvas(imag);

                      switch (form.ToLower())
                      {
                               case "c": canvas.DrawCircle ((float)x1, (float)y1, di,                   pin); break;
                               case "r": canvas.DrawRect   ((float)x1, (float)y1, (float)x2, (float)y2, pin); break;
                               default:  break;
                           //  default: canvas.DrawCircle((float)x1, (float)y1, di, pin); break;
                      }

                      string texto = "\n MAC:   " + pos.Mac   +
                                     "\n Bloco: " + pos.Bloco +
                                     "\n Andar: " + pos.Andar +
                                     "\n Num:   " + pos.Nuco  + " Nome: " + pos.Noco +
                                     "\n Area:  " + pos.Area  + " Pdir: " + pos.Pd   +
                                     "\n Lat:   " + Latitude  +
                                     "\n Lon:   " + Longitud  +
                                     "\n Alt:   " + Altitude; // Escreve no campo da interface
                      txfot.Text = texto;
                      vwima.Invalidate();  // forçar a atualização da imagem1
                      vwfot.Invalidate();  // forçar a atualização da imagem2
               return canvas;
        }
        public Paint  PNG_Pincel   ( int[] cores  , int sc )                                        
        {
                      float dim = Modulo(sc * 10, 200);
                      int a = 210 - (int)dim;
                      int r = cores[0];
                      int g = cores[1];
                      int b = cores[2];
                      Color cor = Color.Argb(a, r, g, b);
                      Paint pin = new Paint { AntiAlias = true, Color = cor };
                      pin.SetStyle(Paint.Style.Fill);
                      return pin;
        }

        public string        Info_Usuario ( string contaem   )                                      
        {
               string usuario = "-";
               try
               {
                   AccountManager acmana = AccountManager.Get(this);
                   Account[]      contas = acmana.GetAccounts();
                   List<string>   Emails = new List<string>();
                   foreach (Account conta in contas)
                   {
                        if (conta.Type.ToLower().Contains(contaem.ToLower()) == true)
                        {
                              Emails.Add(conta.Name);
                        }
                   }
                   if (Emails.Count != 0 && Emails[0] != null)
                        usuario = Emails[0].ToString();
                   else usuario = "Usuário";
               }
               catch  { }
               return usuario;
        }
        public void          Free_Permiso (                  )                                      
        {
            RequestPermissions(new string[]
                                        {
                                          Android.Manifest.Permission.AccessCoarseLocation,
                                          Android.Manifest.Permission.AccessFineLocation,
                                          Android.Manifest.Permission.AccessMockLocation,
                                          Android.Manifest.Permission.AccessLocationExtraCommands,
                                          Android.Manifest.Permission.LocationHardware,
                                          Android.Manifest.Permission.Internet,
                                          Android.Manifest.Permission.ReadExternalStorage,
                                          Android.Manifest.Permission.WriteExternalStorage,
                                          Android.Manifest.Permission.Bluetooth,
                                          Android.Manifest.Permission.BluetoothAdmin,
                                          Android.Manifest.Permission.BluetoothPrivileged,
                                          Android.Manifest.Permission.AccountManager,
                                          Android.Manifest.Permission.BindMidiDeviceService,
                                          Android.Manifest.Permission.Camera,
                                          Android.Manifest.Permission.WriteExternalStorage,
                                          Android.Manifest.Permission.BindVrListenerService
                                        }, (int)Permission.Granted);
        }
        public Canvas        XML_Marca    ( List<float[]> LP )                                      
        {
                       Canvas xcanvas = new Canvas(imagen);
                       float dim = 4;
                       int   alf = 255;
                       int   red = 255;
                       int   gre =   0;
                       int   blu =   0;
                       Color cor =   Color.Argb(alf, red, gre, blu);
                       Paint pin =   new Paint { AntiAlias = true, Color = cor };
                       pin.SetStyle(Paint.Style.Fill);
                       foreach (float[] p in LP)
                       {
                                 xcanvas.DrawCircle(p[0], p[1], dim, pin);
                                 vwima.Invalidate();
                       }
                       GC.Collect();                   //<-- limpa memoria
                       GC.WaitForPendingFinalizers();  //<-- limpa memoria
                return xcanvas;
        }
        public Canvas        XML_Matriz   (                  )                                      
        {
                                 Canvas xcanvas = new Canvas(imagen);
                                 int dim = 2;
                                 Paint pincel = PNG_Pincel(new int[] { 255, 0, 0 }, dim);
                                 List<float[]>  PM = Pts_Matriz (720, 465, 20, 10);
                                 foreach (float[] p in PM)
                                 {
                                          xcanvas.DrawCircle(p[0], p[1], dim, pincel);
                                          vwima.Invalidate();
                                 }
                                 return xcanvas;
        }
        public List<float[]> Pts_Matriz   ( int    resx  , int    resy  , int      denx, int deny ) 
        {
                                List<float[]> mp = new List<float[]> { };
                                for (int i = 0; i <= resx; i = i + denx)
                                {
                                    float x = i;
                                    for (int j = 0; j <= resy; j = j + deny)
                                    {
                                          float y = j;
                                          mp.Add(new float[] { x, y });
                                    }
                                }
                                return mp;
        }
        public void          XML_Carga    ( string raizW , string raizL , string[] arqs  )          
        {
                                    Mensaje_Curto("Carregando arquivos da Web...");
                                    foreach (string arq in arqs)
                                    {
                                           HttpWebRequest  llamadas = (HttpWebRequest)WebRequest.Create(raizW + arq);
                                           HttpWebResponse resposta = (HttpWebResponse)llamadas.GetResponse();
                                           System.IO.Stream txt = resposta.GetResponseStream();
                                           StreamWriter     doc = new StreamWriter(raizL + arq);
                                           byte[] buf = new byte[8192];
                                           string lin = null;
                                           int    con = 1;
                                           while (con > 0)
                                           {
                                                  con = txt.Read(buf, 0, buf.Length);
                                                  if (con != 0)
                                                  {
                                                      lin = System.Text.Encoding.UTF8.GetString(buf, 0, con);
                                                      doc.WriteLine(lin);
                                                  }
                                           }
                                           doc.Close();
                                           doc.Dispose();
                                    }
                                    Mensaje_Curto("Arquivos XML carregados no telefone.");
        }
                
        //converte um bitmap em array de bytes
        public byte[] BMPtoARR         ( Bitmap imag         )     
        {
                                       byte[] imaArr;
                                       using (var stream = new MemoryStream())
                                       {
                                             imag.Compress(Bitmap.CompressFormat.Png, 0, stream);
                                             imaArr = stream.ToArray();
                                       }
                                       return imaArr;
        }
        public Canvas PNG_FundoColorido( Bitmap imag , int s )     
        {
            canvas = new Canvas(imag);
            float di = Modulo(s * 10, 80);
            System.Random r = new System.Random();

            for (int i = 0; i < 10; i++)
            {
                    float x = r.Next(0, imag.Width);
                    float y = r.Next(0, imag.Height);
                    Paint pin = PNG_Pincel(new int[] { i+33 %255, i+150 %255, i+243 % 255 }, i+s % 60);
                    canvas.DrawCircle(x, y, di, pin);
                    vwima.Invalidate();
            }
            return canvas;
        }

        //Funções MIDI--------------------------------------------------------------------------
        public void Midi_Tocar_Local ( string midi )               
        {
            if (midi != "")
            {
                           Midi_Parar_Music(    );
                           Midi_Tocar_Music(midi);
            }
        }
        public void Midi_Tocar_Music ( string midi )               
        {
                    string locnube        = "https://sites.google.com/";
                    string arqmidi        = "a/poli.ufrj.br/jose-luis-menegotto/mapas/" + midi + ".mid";
                    Android.Net.Uri tocar = Pega_Uri(locnube, arqmidi);
                    MediaPlayer player    = MediaPlayer.Create(this, tocar);
                    player.Start();
        }
        public void Midi_Parar_Music (            )                
        {
                   AudioManager AudioManager = (AudioManager)GetSystemService(Context.AudioService);
                   if (AudioManager.IsMusicActive)
                   {
                         player.Stop();
                   }
        }
        public Uri  Pega_Uri         ( string dir, string arqui )  
        {
                   Uri Url          = Uri.Parse(dir);
                   Uri.Builder buil = new Uri.Builder();
                   Uri.Builder path = new Uri.Builder();
                   buil.Path(arqui);
                   path.EncodedPath(Url.EncodedPath);
                   path.AppendEncodedPath(buil.Build().Path);
                   buil.Scheme(Url.Scheme);
                   buil.EncodedAuthority(Url.EncodedAuthority);
                   buil.EncodedPath(path.Build().EncodedPath);
            return buil.Build(); 
        }


    }

}


