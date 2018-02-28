using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using Android.OS;
using Symbol.XamarinEMDK;
using EMDKXamarinSerialComm;
using Java.Lang;
using Java.Util;
using Android;
using Symbol.XamarinEMDK.SerialComm;
using System.Collections.Generic;


namespace EMDKXamarinSerialComm
{
    [Activity(Label = "EMDKXamarinSerialComm", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        
        private EMDKManager emdkManager = null;
        private static Symbol.XamarinEMDK.SerialComm.SerialCommMgrEX serialCommManager = null;
        private string TAG = typeof(MainActivity).Name;
        private EditText editText = null;
        private TextView statusView = null;
        private Button readButton = null;
        private Button writeButton = null;
        private Spinner spinnerports = null;
        private static SerialCommMgr serialCommPort = null;
        public Dictionary<string, SerialPortInfo> supportedPorts = new Dictionary<string, SerialPortInfo>();
        static MainActivity Activity;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource

            SetContentView(Symbol.XamarinEMDK.SerialCommSample1.Resource.Layout.Main);
            editText = FindViewById<EditText>(Symbol.XamarinEMDK.SerialCommSample1.Resource.Id.editText1);
            editText.SetText("Serial Communication Write Data Testing.", EditText.BufferType.Normal);

            statusView = FindViewById<TextView>(Symbol.XamarinEMDK.SerialCommSample1.Resource.Id.statusView);
            statusView.SetText("", TextView.BufferType.Normal);
            statusView.RequestFocus();

            EMDKResults results = EMDKManager.GetEMDKManager(ApplicationContext, this);
            if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
            {
                statusView.SetText("Failed to open EMDK", TextView.BufferType.Normal);
            }
            else
            {
                statusView.SetText("Opening EMDK...", TextView.BufferType.Normal);
            }

            addReadButtonEvents();
            writeButtonEvents();
            setEnabled(false);
            Activity = this;
        }

        public static MainActivity getinstance()
        {
            return Activity;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            deinitSerialComm();

            if (emdkManager != null)
            {
                // Clean up the objects created by EMDK manager
                emdkManager.Release();
                emdkManager = null;
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            deinitSerialComm();
            serialCommManager = null;
            if (emdkManager != null)
            {
                // Clean up the objects created by EMDK manager
                emdkManager.Release(EMDKManager.FEATURE_TYPE.SerialcommEx);
                emdkManager = null;
            }

        }

        protected override void OnResume()
        {
            base.OnResume();
            // Acquire the serialComm manager resources
            if (emdkManager != null)
            {
                serialCommManager = (SerialCommMgrEX)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.SerialcommEx);

                if (serialCommManager != null)
                {
                    populatePorts();
                    initSerialComm();
                }
            }
        }

        void populatePorts()
        {
        try {

            if(serialCommManager != null) {
                List<SerialPortInfo> serialPorts = (List<SerialPortInfo>)serialCommManager.SupportedPorts;
                if(serialPorts.Count>0) {  
                    supportedPorts = new Dictionary<string, SerialPortInfo> ();
                    string[] ports = new string[serialPorts.Count];
                    int count = 0;
                    foreach (SerialPortInfo info in serialPorts) {
                        supportedPorts.Add(info.FriendlyName, info);
                        ports[count] = info.FriendlyName;
                        count++;
                    }

                    spinnerports.Adapter= (new ArrayAdapter<string>(this,
                            Android.Resource.Layout.SimpleDropDownItem1Line, ports));

                    spinnerports.ItemSelected +=spinnerports_ItemSelected;
                    
                    

                }
                else
                {
                    RunOnUiThread(() => statusView.Text =  "Failed to get available ports");
   
                }
            }
            else
            {
                RunOnUiThread(() => statusView.Text =  "SerialCommManager is null");
            }

        }
        catch (SerialCommException ex)
        {
            Log.Debug(TAG, ex.Message);
            RunOnUiThread(() => statusView.Text =  ex.Message);
        }

            
        }
        void spinnerports_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            deinitSerialComm();
            initSerialComm();
        }
        


        public void OnClosed()
        {
            deinitSerialComm();
            if (emdkManager != null)
            {
                emdkManager.Release();
            }
            displayMessage("EMDK closed abruptly.");
        }

        public void deinitSerialComm()
        {
            if (serialCommPort != null)
            {
                try
                {
                    serialCommPort.Disable();
                    serialCommPort = null;

                }
                catch (System.Exception ex)
                {
                    Android.Util.Log.Error("EMDKXamarinSerialCom","deinitSerialComm disable Exception: " + ex.Message);
                }
            }
        }

        public void initSerialComm() 
        {
            new AsyncEnableSerialComm().Execute(supportedPorts[spinnerports.SelectedItem.ToString()]);
        }


        private class AsyncEnableSerialComm : Android.OS.AsyncTask<SerialPortInfo, Java.Lang.Void, SerialCommResults>
        {
        
            
        protected override SerialCommResults RunInBackground(params SerialPortInfo[] params1) {

            SerialCommResults returnvar = SerialCommResults.Failure;
            try {
                serialCommPort = serialCommManager.GetPort(params1[0]);

            } catch (SerialCommException ex) {
                ex.PrintStackTrace();
            }

            if (serialCommPort != null) {
                try {
                    serialCommPort.Enable();
                    returnvar = SerialCommResults.Success;

                } catch (SerialCommException e) {
                    Log.Debug("SerialComm", e.Message);
                    e.PrintStackTrace();
                    returnvar = e.Result;
                }
            }

            return returnvar;
        }

        protected void onPostExecute(SerialCommResults result) {
            base.OnPostExecute(result);
            MainActivity mainactvity = MainActivity.getinstance();
            if (result == SerialCommResults.Success) {

                mainactvity.statusView.Text = "Failed to get available ports" + mainactvity.spinnerports.SelectedItem.ToString() + ")";
                mainactvity.editText.SetText("Serial Communication Write Data Testing " + mainactvity.spinnerports.SelectedItem.ToString() + ".", EditText.BufferType.Normal);
                mainactvity.setEnabled(true);
            } else {                
                mainactvity.statusView.Text = "read:" + result.Description; 
                mainactvity.setEnabled(false);
                }
            }        
       
        }
       
       

        public void OnOpened(EMDKManager emdkManager)
        {
            this.emdkManager = emdkManager;

            Log.Debug(TAG, "EMDK opened");

            try
            {
                serialCommManager = (SerialCommMgrEX)this.emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.SerialcommEx);
                if (serialCommManager != null)
                {
                    populatePorts();
                }
                else
                {
                    RunOnUiThread(() => statusView.Text = EMDKManager.FEATURE_TYPE.SerialcommEx.ToString() + " Feature not supported.");
                }
            }
            catch (SerialCommException e)
            {
                Log.Debug(TAG, e.Message);
                RunOnUiThread(() => statusView.Text = e.Message);
            }
        }

        private void addReadButtonEvents()
        {
            readButton = FindViewById<Button>(Symbol.XamarinEMDK.SerialCommSample1.Resource.Id.ReadButton);
            readButton.Click += ReadButton_Click;
        }

        private void ReadButton_Click(object sender, EventArgs e)
        {
            
            RunOnUiThread(() =>
            {
                setEnabled(false);
                string statusText = "";
                try
                {

                    byte[] readBuffer = serialCommPort.Read(10000); //Timeout after 10 seconds

                    if (readBuffer != null)
                    {
                        string tempString = new string(System.Text.Encoding.UTF8.GetChars(readBuffer));
                        statusText = "Data Read:\n" + tempString;
                    }
                    else
                    {
                        statusText = "No Data Available";
                    }

                }
                catch (SerialCommException ex)
                {
                    statusText = "read:" + ex.Result.Description;
                }
                catch (Java.Lang.Exception exp)
                {
                    statusText = "read:" + exp.Message;
                }
                setEnabled(true);
                displayMessage(statusText);

            });

        }

        private void displayMessage(string message)
        {
            string tempMessage = message;
            RunOnUiThread(() =>
            {
                statusView.SetText(tempMessage + "\n", TextView.BufferType.Normal);
            });
        }

        private void writeButtonEvents()
        {
            writeButton = FindViewById<Button>(Symbol.XamarinEMDK.SerialCommSample1.Resource.Id.WriteButton);
            writeButton.Click += WriteButton_Click;
        }

        private void WriteButton_Click(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                setEnabled(false);
                try
                {
                    string writeData = editText.Text.ToString();
                    char[] data=writeData.ToCharArray();

                    int bytesWritten = serialCommPort.Write(System.Text.Encoding.ASCII.GetBytes(writeData), System.Text.Encoding.ASCII.GetBytes(writeData).Length);
                    statusView.SetText("Bytes written: " + bytesWritten , TextView.BufferType.Normal);
                }
                catch (SerialCommException ex)
                {
                    statusView.SetText("write: " + ex.Result.Description, TextView.BufferType.Normal);
                }
                catch (Java.Lang.Exception exp)
                {
                    statusView.SetText("write: " + exp.Message + "\n", TextView.BufferType.Normal);
                }
                setEnabled(true);

            });
        }

        private void setEnabled(bool enableState)
        {
            bool tempState = enableState;
            RunOnUiThread(() =>
            {
                readButton.Enabled = tempState;
                writeButton.Enabled = tempState;
                editText.Enabled = tempState;
                if(spinnerports!=null)
                spinnerports.Enabled = tempState;
            });

        }


    }
}


